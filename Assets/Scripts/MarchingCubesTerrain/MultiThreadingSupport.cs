using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static UnityEngine.Debug;

public class MultiThreadingSupport : MonoBehaviour
{
    public static MultiThreadingSupport Instance;
	public ChunkSettings chunkSettings;

	public ChunkGenerator chunkGenerator;
	
	private void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

	
	[HideInInspector]
	public int currentThreads = 0;
	public readonly List<Chunk> QueueToThread = new();

	private void RequestThreading()
	{
		if (QueueToThread.Count <= 0) return;
		if(currentThreads >= Mathf.Clamp(chunkSettings.maxNumberOfThreadsPerFrame * 10,25, 45) && chunkSettings.limitThreads) return;
		for (var i = chunkSettings.maxNumberOfThreadsPerFrame - 1; i >= 0; i--)
		{
			if(QueueToThread.Count <= i) continue;
			var tempChunk = ClosestChunk();
			if (!tempChunk.ReRequestChunkData(false)) continue;
			currentThreads++;
			QueueToThread.Remove(tempChunk);
		}
	}

	private Chunk ClosestChunk()
	{
		var closest = QueueToThread[0];
		var dis = Mathf.Infinity;
		var viewerPos = chunkGenerator.viewer.position;
		foreach (var chunk in QueueToThread)
		{
			var distance = Vector2.Distance(new Vector2(viewerPos.x, viewerPos.z),
				new Vector2(chunk.ChunkPos.x, chunk.ChunkPos.z));
			if (!(distance < dis)) continue;
			closest = chunk;
			dis = distance;
		}
		return closest;
	}


	private readonly Queue<ChunkThreadInfo<MeshData>> _chunkThreadInfoQueue = new();

	private readonly struct ChunkThreadInfo<T>
	{
		public readonly Action<T> Callback;
		public readonly T Parameter;

		public ChunkThreadInfo(Action<T> callback, T parameter)
		{
			Callback = callback;
			Parameter = parameter;
		}
	}
	
	private readonly Queue<ChunkListThreadInfo<List<Chunk>>> _chunkListThreadInfo = new();

	private readonly struct ChunkListThreadInfo<T>
	{
		public readonly Action<T> Callback;
		public readonly T Parameter;

		public ChunkListThreadInfo(Action<T> callback, T parameter)
		{
			Callback = callback;
			Parameter = parameter;
		}
	}

	public struct ChunkDataRequested
	{
		public int Chunkwidth;
		public int ChunkHeight;
		public Vector3Int ChunkPos;
		public int ChunkScale;
		public float WorldHeight;
		public NoiseFilter[] NoiseFilters;
		public IReadOnlyList<ChunkSettings.NoiseLayer> NoiseLayers;
		public float Freq;
		public float Amp;
		public float TerrainSurface;
		public bool smooth;
		public bool flatShading;
		public float caveThreshold;
		public float caveAmp;
		public float caveFreq;
		public bool caveHeightLimited;
		public float caveMaxHeight;
		public int chunkBelowZero;
		public int Lod;
		public float heightMultiplier;
	}

	public void RequestChunkData(Action<MeshData> callback, ChunkDataRequested newChunkData)
	{

		void ThreadStart()
		{
			ChunkDataThread(callback, newChunkData);
		}

		new Thread(ThreadStart).Start();
	}

	private void ChunkDataThread(Action<MeshData> callback, ChunkDataRequested chunkDataReq)
	{
		var meshData = BuildChunk(chunkDataReq);
		lock (_chunkThreadInfoQueue)
		{
			_chunkThreadInfoQueue.Enqueue(new ChunkThreadInfo<MeshData>(callback, meshData));
		}
	}

	public void RequestMeshData(Action<MeshData> callback, MeshData meshData, ChunkDataRequested newChunkData)
	{
		void ThreadStart()
		{
			MeshDataThread(callback, newChunkData, meshData);
		}

		new Thread(ThreadStart).Start();
	}

	private void MeshDataThread(Action<MeshData> callback, ChunkDataRequested chunkDataReq, MeshData _meshData)
	{
		var meshData = CreateMeshData(chunkDataReq, _meshData);
		lock (_chunkThreadInfoQueue)
		{
			_chunkThreadInfoQueue.Enqueue(new ChunkThreadInfo<MeshData>(callback, meshData));
		}
	}

	private int frame = 0;
	private void LateUpdate()
	{
		if (frame >= 60)
			frame = 0;
		frame++;
		if(frame % 2 == 0)
			RequestThreading();

		//if (frame % 2 != 0) return;
		for (var j = chunkSettings.maxNumberOfThreadsPerFrame - 1; j >= 0; j--)
		{
			lock (_chunkListThreadInfo)
			{
				if (_chunkListThreadInfo.Count > 0)
				{
					for (var i = 0; i < _chunkListThreadInfo.Count; i++)
					{
						var threadInfo = _chunkListThreadInfo.Dequeue();
						threadInfo.Callback(threadInfo.Parameter);
					}
				}
			}

			lock (_chunkThreadInfoQueue)
			{
				if (_chunkThreadInfoQueue.Count <= 0) return;
			}

			lock (_chunkThreadInfoQueue)
			{
				for (var i = 0; i < _chunkThreadInfoQueue.Count; i++)
				{
					var threadInfo = _chunkThreadInfoQueue.Dequeue();
					threadInfo.Callback(threadInfo.Parameter);
				}
			}
		}
	}


    public class MeshData
	{
		public readonly List<Vector3> Vertices = new();
		public readonly List<int> Triangles = new();
		public List<Vector3> Normals = new();
		public float[] TerrainMap;
	}

    private MeshData BuildChunk(ChunkDataRequested chunkDataReq)
    {
	    var width = chunkDataReq.Chunkwidth + 1;
	    var height = chunkDataReq.ChunkHeight + 1  + chunkDataReq.chunkBelowZero;

		var terrainMap = new float[width * height * width + height * width + width];
		
		return CreateMeshData(chunkDataReq, PopulateTerrainMap(chunkDataReq, terrainMap));
	}

	private static float CalculateNoiseVal(Vector3 pos, IReadOnlyList<NoiseFilter> noiseFilters, IReadOnlyList<ChunkSettings.NoiseLayer> noiseLayers, float freq, float amp, float worldHeight)
	{
		float firstLayerValue = 0;
		float elevation = 0;

		var temp = new Vector3(0, 0, 0);

		if (noiseFilters.Count > 0)
		{
			temp.x = pos.x / 16f * freq + 0.001f;
			temp.y = pos.y / 16f * freq + 0.001f;
			temp.z = pos.z / 16f * freq + 0.001f;
			firstLayerValue = noiseFilters[0].Evaluate(temp);
			if (noiseLayers[0].enabled)
			{
				elevation = firstLayerValue;
			}
		}

		for (var i = 1; i < noiseFilters.Count; i++)
		{
			if (!noiseLayers[i].enabled) continue;
			temp.x = pos.x / 16f * freq + 0.001f;
			temp.y = pos.y / 16f * freq + 0.001f;
			temp.z = pos.z / 16f * freq + 0.001f;
			var mask = (noiseLayers[i].useFirstLayerAsMask) ? firstLayerValue : 1;
			elevation += noiseFilters[i].Evaluate(temp) * mask;
		}

		return worldHeight / amp * elevation;
	}

	private static MeshData PopulateTerrainMap(ChunkDataRequested chunkDataReq, float[] terrainMap)
	{
		var width = chunkDataReq.Chunkwidth + 1;
		var height = chunkDataReq.ChunkHeight + 1;

		// The data points for terrain are stored at the corners of our "cubes", so the terrainMap needs to be 1 larger
		// than the width/height of our mesh.
		for (var x = 0; x < width; x++)
		for (var y = -chunkDataReq.chunkBelowZero; y < height; y++)
		for (var z = 0; z < width; z++)
		{
			var noisePos = new Vector3((float) x + chunkDataReq.ChunkPos.x / chunkDataReq.ChunkScale,
				(float) y + chunkDataReq.ChunkPos.y / chunkDataReq.ChunkScale,
				(float) z + chunkDataReq.ChunkPos.z / chunkDataReq.ChunkScale);
			var point = CalculateNoiseVal(noisePos, chunkDataReq.NoiseFilters, chunkDataReq.NoiseLayers, chunkDataReq.Freq, chunkDataReq.Amp, chunkDataReq.WorldHeight);

			//var point = 0f;
			
			if (y > chunkDataReq.ChunkHeight - 1)
			{
				point = 1f;
			}
			else if (y < -chunkDataReq.chunkBelowZero + 1)
			{
				point = 0f;
			}
			else
			{
				var point2 = chunkDataReq.heightMultiplier * Mathf.PerlinNoise(
					((float) x + chunkDataReq.ChunkPos.x / chunkDataReq.ChunkScale) / 96f * chunkDataReq.Freq + 81f,
					((float) z + chunkDataReq.ChunkPos.z / chunkDataReq.ChunkScale) / 64f * chunkDataReq.Freq - 81f);

				point2 = y - point2;
				point = y - point;

				point = (point2 + point) / 2;
				
				
				
				var pointCave = CalculateNoiseVal(noisePos, chunkDataReq.NoiseFilters, chunkDataReq.NoiseLayers,
					chunkDataReq.Freq * chunkDataReq.caveFreq, chunkDataReq.Amp * chunkDataReq.caveAmp, chunkDataReq.WorldHeight);
				if(!chunkDataReq.caveHeightLimited)
					point = pointCave < chunkDataReq.caveThreshold ? 1 : point;
				else
				{
					if(y < chunkDataReq.caveMaxHeight)
						point = pointCave < chunkDataReq.caveThreshold ? 1 : point;
				}

			}

			var tempHeight = height + chunkDataReq.chunkBelowZero;
			// Set the value of this point in the terrainMap.
			terrainMap[x * tempHeight * width + (y + chunkDataReq.chunkBelowZero) * width + z] = point;
			
		}

		var tempMeshData = new MeshData
		{
			TerrainMap = terrainMap
		};
		return tempMeshData;
	}

	private MeshData CreateMeshData(ChunkDataRequested chunkDataReq, MeshData meshData)
	{
		var colourMap = new List<Color>();
		var simpleIncrement = chunkDataReq.Lod == 0
				? 1
				: chunkDataReq.Lod * 2;

		// Loop through each "cube" in our terrain.
		for (var x = 0; x < chunkDataReq.Chunkwidth - simpleIncrement + 1; x+=simpleIncrement)
		for (var y = -chunkDataReq.chunkBelowZero; y < chunkDataReq.ChunkHeight  - simpleIncrement + 1; y+=simpleIncrement)
		for (var z = 0; z < chunkDataReq.Chunkwidth  - simpleIncrement + 1; z+=simpleIncrement)
		{
			// Create an array of floats representing each corner of a cube and get the value from our terrainMap.
			var cube = new float[8];
			var configurationIndex = 0;
			for (var i = 0; i < 8; i++)
			{
				var temp = new Vector3Int(x, y, z);
				
				var point = temp + ChunkData.CornerTable[i] * simpleIncrement;

				var sampleTerrain = SampleTerrain(point, meshData.TerrainMap, chunkDataReq);

				cube[i] = sampleTerrain;
				// Get the configuration index of this cube.
				if (sampleTerrain > chunkDataReq.TerrainSurface)
					configurationIndex |= 1 << i;
			}
			
			var configIndex = configurationIndex;

			// If the configuration of this cube is 0 or 255 (completely inside the terrain or completely outside of it) we don't need to do anything.
			if (configIndex is 0 or 255) continue;

			// Loop through the triangles. There are never more than 5 triangles to a cube and only three vertices to a triangle.
			var edgeIndex = 0;
			for (var i = 0; i < 5; i++)
			{
				for (var p = 0; p < 3; p++)
				{
					// Get the current indice. We increment triangleIndex through each loop.
					var indice = ChunkData.TriangleTable[configIndex, edgeIndex];

					// If the current edgeIndex is -1, there are no more indices and we can exit the function.
					if (indice == -1)
					{
						i = 6;
						break;
					}

					// Get the vertices for the start and end of this edge.
					var tempVert1 = new Vector3Int(x , y, z);
					Vector3 vert1 = tempVert1 + ChunkData.CornerTable[ChunkData.EdgeIndexes[indice, 0]] * simpleIncrement;
					Vector3 vert2 = tempVert1 + ChunkData.CornerTable[ChunkData.EdgeIndexes[indice, 1]] * simpleIncrement;
					Vector3 vertPosition;
					if (chunkDataReq.smooth)
					{
						var vert1Sample = cube[ChunkData.EdgeIndexes[indice, 0]];
						var vert2Sample = cube[ChunkData.EdgeIndexes[indice, 1]];
						var dif = vert2Sample - vert1Sample;
						if (dif == 0) dif = chunkDataReq.TerrainSurface;
						else dif = (chunkDataReq.TerrainSurface - vert1Sample) / dif;
						vertPosition = vert1 + (vert2 - vert1) * dif;
					}
					else
					{
						vertPosition = (vert1 + vert2) / 2f;
					}

					// Add to our vertices and triangles list and increment the edgeIndex.
					if (chunkDataReq.flatShading)
					{
							meshData.Vertices.Add(vertPosition);
							meshData.Triangles.Add(meshData.Vertices.Count - 1);
					}
					else
					{
							meshData.Triangles.Add(VertForIndice(vertPosition, meshData.Vertices));
					}

					edgeIndex++;
				}
			}
		}

		meshData.Normals = CalculateNormals(meshData.Vertices, meshData.Triangles);
		return meshData;
	}

	private static List<Vector3> CalculateNormals(IList<Vector3> vertices, IList<int> triangles)
	{
		var vertexNormals = new Vector3[vertices.Count];
		
		var triangleCount = triangles.Count / 3;
		for (var i = 0; i < triangleCount; i++)
		{
			var normalTriangleIndex = i * 3;
			var vertexIndexA = triangles[normalTriangleIndex];
			var vertexIndexB = triangles[normalTriangleIndex + 1];
			var vertexIndexC = triangles[normalTriangleIndex + 2];

			var triangleNormal = SurfaceNormals(vertices, vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		for (var i = 0; i < vertexNormals.Length; i++)
		{
			vertexNormals[i].Normalize();
		}

		return vertexNormals.ToList();
	}

	private static Vector3 SurfaceNormals(IList<Vector3> vertices, int A, int B, int C)
	{
		var pointA = vertices[A];
		var pointB = vertices[B];
		var pointC = vertices[C];

		var sideAB = pointB - pointA;
		var sideAC = pointC - pointA;
		
		return Vector3.Cross(sideAB, sideAC).normalized;
	}

	private static float SampleTerrain(Vector3Int point, IReadOnlyList<float> terrainMap, ChunkDataRequested chunkDataReq)
	{
		var width = chunkDataReq.Chunkwidth + 1;
		var height = chunkDataReq.ChunkHeight + 1 + chunkDataReq.chunkBelowZero;
		return terrainMap[point.x * height * width + (point.y + chunkDataReq.chunkBelowZero) * width + point.z];
	}

	private int VertForIndice(Vector3 vert, IList<Vector3> vertices)
    {
		for(var i = 0; i < vertices.Count; i++)
        {
			if (vertices[i] == vert)
				return i;
        }
		vertices.Add(vert);
		return vertices.Count - 1;
    }
	
	public void RequestGetNeighbourChunksFromPos(Action<List<Chunk>> callback, ChunkDataRequested chunkDataReq, float radious, Vector3 pos, Vector3 hitPoint, Dictionary<Vector3Int, Chunk> _chunks, float val)
	{
		void ThreadStart()
		{
			GetNeighbourChunksFromPosThread(callback, chunkDataReq, radious, pos, hitPoint, _chunks, val);
		}
		new Thread(ThreadStart).Start();
	}

	private void GetNeighbourChunksFromPosThread(Action<List<Chunk>> callback, ChunkDataRequested chunkDataReq, float radious, Vector3 pos, Vector3 hitPoint, Dictionary<Vector3Int, Chunk> _chunks, float val)
	{
		var chunks = GetNeighbourChunksFromPos(chunkDataReq, radious, pos, hitPoint, _chunks, val);
		lock (_chunkListThreadInfo)
		{
			_chunkListThreadInfo.Enqueue(new ChunkListThreadInfo<List<Chunk>>(callback, chunks));
		}
	}
	
	private List<Chunk> GetNeighbourChunksFromPos(ChunkDataRequested chunkDataReq, float radious, Vector3 pos, Vector3 hitPoint, Dictionary<Vector3Int, Chunk> _chunks, float val)
	{
		var posInt = new Vector3Int((int) hitPoint.x, (int) hitPoint.y, (int) hitPoint.z);
		var gridPoints = new List<Vector3>();
		var radiusCeil = Mathf.CeilToInt(radious);
		for (var i = -radiusCeil; i <= radiusCeil; i++)
		{
			for (var j = -radiusCeil; j <= radiusCeil; j++)
			{
				for (var k = -radiusCeil; k <= radiusCeil; k++)
				{
					var gridPoint = new Vector3(posInt.x + i,posInt.y + j,posInt.z + k);
					if (Vector3.Distance(posInt, gridPoint) <= radious)
					{
						gridPoints.Add(gridPoint + new Vector3(0, chunkSettings.chunkBelowZero));
					}
				}
			}
		}
		
		var x = Mathf.CeilToInt(pos.x);
		var y = Mathf.CeilToInt(pos.y);
		var z = Mathf.CeilToInt(pos.z);
		var tempVector3 = new Vector3Int(x, y, z);
		
		var chunkList = new List<Chunk>();
		var numOfChunks = (int) ((radious * 4) / 10);
		if (numOfChunks < 1) numOfChunks = 1;
		for (var i = -numOfChunks; i <= numOfChunks; i++)
		{
			for (var j = -numOfChunks; j <= numOfChunks; j++)
			{
				var tempChunkPos = new Vector3Int(tempVector3.x - chunkDataReq.Chunkwidth * j, 0,
					tempVector3.z - chunkDataReq.Chunkwidth * i);

				if (!_chunks.ContainsKey(tempChunkPos)) continue;
				var _chunk = _chunks[tempChunkPos];
					
				var _width = chunkDataReq.Chunkwidth + 1;
				var height = chunkDataReq.ChunkHeight + 1 + chunkDataReq.chunkBelowZero;
				var chunkPos = _chunk.ChunkPos;
		
				foreach (var v3Int in from p in gridPoints
					where !(p.z < chunkPos.z) && !(p.z > chunkPos.z + chunkDataReq.Chunkwidth)
					where !(p.x < chunkPos.x) && !(p.x > chunkPos.x + chunkDataReq.Chunkwidth)
					where !(p.y < chunkPos.y + radious) &&
					      !(p.y > chunkPos.y + chunkDataReq.ChunkHeight * 2)
					select val > 0f
						? new Vector3Int(Mathf.FloorToInt(p.x) - chunkPos.x, Mathf.FloorToInt(p.y) - chunkPos.y,
							Mathf.FloorToInt(p.z) - chunkPos.z)
						: new Vector3Int(Mathf.CeilToInt(p.x) - chunkPos.x, Mathf.CeilToInt(p.y) - chunkPos.y,
							Mathf.CeilToInt(p.z) - chunkPos.z))
				{
					try
					{
						_chunk._meshData.TerrainMap[v3Int.x * height * _width + v3Int.y * _width + v3Int.z] = val;
						chunkList.Add(_chunk);
					}
					catch
					{
						// ignored
					}
				}
			}
		}
		return chunkList;
	}
	
}

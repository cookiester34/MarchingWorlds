using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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

	private int maxThreads = 50;
	public int currentThreads = 0;
	public readonly List<Chunk> QueueToThread = new();

	private void RequestThreading()
	{
		if (QueueToThread.Count <= 0) return;
		if(currentThreads >= maxThreads && chunkSettings.limitThreads) return;
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


	private readonly Queue<ChunkThreadInfo<MeshData>> _chunkThreadInfoQueue = new Queue<ChunkThreadInfo<MeshData>>();

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
		
		public struct NoiseLayerStruct
		{
			public bool enabled;
			public bool useFirstLayerAsMask;
			public NoiseSettings NoiseSettings;
		}
	}

	public void RequestChunkData(Action<MeshData> callback, Vector3Int chunkPos, int chunkScale, NoiseFilter[] noiseFilters, ChunkDataRequested newChunkData)
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
	    
	    lock (_chunkThreadInfoQueue)
	    {
		    if (_chunkThreadInfoQueue.Count <= 0) return;
	    }

	    lock (_chunkThreadInfoQueue)
	    {
		    for(var i = 0; i < _chunkThreadInfoQueue.Count; i++)
		    {
			    var threadInfo = _chunkThreadInfoQueue.Dequeue();
			    threadInfo.Callback(threadInfo.Parameter);
		    }
	    }
	}


    public class MeshData
	{
		public readonly List<Vector3> Vertices = new();
		public readonly List<int> Triangles = new();
		public float[] TerrainMap = new float[36443];
	}

    private MeshData BuildChunk(ChunkDataRequested chunkDataReq)
	{
		var meshData = new MeshData();

		var terrainMap = new float[36443];

		terrainMap = PopulateTerrainMap(chunkDataReq, terrainMap);
		meshData.TerrainMap = terrainMap;
		
		return CreateMeshData(chunkDataReq, meshData);
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

	private static float[] PopulateTerrainMap(ChunkDataRequested chunkDataReq, float[] terrainMap)
	{
		// The data points for terrain are stored at the corners of our "cubes", so the terrainMap needs to be 1 larger
		// than the width/height of our mesh.
		for (var x = 0; x < chunkDataReq.Chunkwidth + 1; x++)
		for (var y = 0; y < chunkDataReq.ChunkHeight + 1; y++)
		for (var z = 0; z < chunkDataReq.Chunkwidth + 1; z++)
		{
			var noisePos = new Vector3((float) x + chunkDataReq.ChunkPos.x / chunkDataReq.ChunkScale,
				(float) y + chunkDataReq.ChunkPos.y / chunkDataReq.ChunkScale,
				(float) z + chunkDataReq.ChunkPos.z / chunkDataReq.ChunkScale);
			var point = CalculateNoiseVal(noisePos, chunkDataReq.NoiseFilters, chunkDataReq.NoiseLayers, chunkDataReq.Freq, chunkDataReq.Amp, chunkDataReq.WorldHeight);
			var point2 = chunkDataReq.ChunkHeight * Mathf.PerlinNoise(((float) x + chunkDataReq.ChunkPos.x / chunkDataReq.ChunkScale) / 96f * chunkDataReq.Freq + 81f,((float) z + chunkDataReq.ChunkPos.z / chunkDataReq.ChunkScale) / 64f * chunkDataReq.Freq - 81f);
			if (y > chunkDataReq.WorldHeight)
			{
				point2 = 1f;
				point = 1f;
			}
			else
			{
				point2 = y - point2;
				point = y - point;
			}
				
			if (y < 10)
			{
				point = 0f;
			}
			else if (y > 60)
			{
				point = (point2 + point) / 2;
			}

			// Set the value of this point in the terrainMap.
			terrainMap[x * 276 * 11 + y * 11 + z] = point;
		}

		return terrainMap;
	}


	private MeshData CreateMeshData(ChunkDataRequested chunkDataReq, MeshData meshData)
	{
		// Loop through each "cube" in our terrain.
		for (float x = 0; x < chunkDataReq.Chunkwidth; x++)
		for (float y = 0; y < chunkDataReq.ChunkHeight; y++)
		for (float z = 0; z < chunkDataReq.Chunkwidth; z++)
		{
			// Create an array of floats representing each corner of a cube and get the value from our terrainMap.
			var cube = new float[8];
			for (var i = 0; i < 8; i++)
			{
				var temp = new Vector3Int((int) x, (int) y, (int) z);
				cube[i] = SampleTerrain(temp + ChunkData.CornerTable[i], meshData.TerrainMap);
			}

			// Get the configuration index of this cube.
			var configurationIndex = 0;
			for (var i = 0; i < 8; i++)
			{
				// If it is, use bit-magic to the set the corresponding bit to 1. So if only the 3rd point in the cube was below
				// the surface, the bit would look like 00100000, which represents the integer value 32.
				if (cube[i] > chunkDataReq.TerrainSurface)
					configurationIndex |= 1 << i;
			}
			var configIndex = configurationIndex;

			// If the configuration of this cube is 0 or 255 (completely inside the terrain or completely outside of it) we don't need to do anything.
			if (configIndex is 0 or 255) continue;

			// Loop through the triangles. There are never more than 5 triangles to a cube and only three vertices to a triangle.
			var edgeIndex = 0;
			for (var i = 0; i < 5; i++)
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
				var tempVert1 = new Vector3Int((int) x, (int) y, (int) z);
				Vector3 vert1 = tempVert1 + ChunkData.CornerTable[ChunkData.EdgeIndexes[indice, 0]];
				Vector3 vert2 = tempVert1 + ChunkData.CornerTable[ChunkData.EdgeIndexes[indice, 1]];
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

				// Add to our vertices and triangles list and incremement the edgeIndex.
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

		return meshData;
	}

	private static float SampleTerrain(Vector3Int point, IReadOnlyList<float> terrainMap)
	{
		return terrainMap[point.x * 276 * 11 + point.y * 11 + point.z];
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
	
}

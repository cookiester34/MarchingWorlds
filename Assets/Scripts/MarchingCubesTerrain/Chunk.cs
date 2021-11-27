using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Chunk
{
	public readonly GameObject ChunkObject;
	private readonly MeshFilter _meshFilter;
    private readonly MeshCollider _meshCollider;

    private readonly int _scale;
    private readonly ChunkSettings _chunkSettings;
    private Vector3Int _position;
	private readonly NoiseFilter[] _noiseFilters;

	public Vector3Int ChunkPos;

	private readonly MultiThreadingSupport _multiThreadingSupport;
	private MultiThreadingSupport.MeshData _meshData;

	private bool _meshUpdate = false;

	public Chunk(ChunkSettings chunkSettings, Vector3Int position, Material material, MultiThreadingSupport multiThreadingSupport, Transform parent)
	{
		_multiThreadingSupport = multiThreadingSupport;
		_position = position;
		_chunkSettings = chunkSettings;
		_scale = _chunkSettings.chunkScale;
		ChunkPos = _position;

		ChunkObject = new GameObject("Terrain Chunk: " + position)
		{
			layer = 3,
			transform =
			{
				position = _position
			}
		};
		ChunkObject.transform.SetParent(parent);

		_meshFilter = ChunkObject.AddComponent<MeshFilter>();
		_meshCollider = ChunkObject.AddComponent<MeshCollider>();
		var meshRenderer = ChunkObject.AddComponent<MeshRenderer>();

		meshRenderer.material = material;
		_noiseFilters = new NoiseFilter[_chunkSettings.NoiseLayers.Count];
		for (var i = 0; i < _noiseFilters.Length; i++)
		{
			_noiseFilters[i] = new NoiseFilter(_chunkSettings.NoiseLayers[i].noiseSettings);
		}
		ReRequestChunkData();
	}

	public Chunk(ChunkSettings chunkSettings, Vector3Int position, Material material, MultiThreadingSupport multiThreadingSupport, MultiThreadingSupport.MeshData meshData, Transform parent)
	{
		_multiThreadingSupport = multiThreadingSupport;
		_meshData = meshData;
		_position = position;
		_chunkSettings = chunkSettings;
		_scale = chunkSettings.chunkScale;
		ChunkPos = _position;

		ChunkObject = new GameObject("Terrain Chunk: " + position)
		{
			layer = 3,
			transform =
			{
				position = _position
			}
		};
		ChunkObject.transform.SetParent(parent);

		_meshFilter = ChunkObject.AddComponent<MeshFilter>();
		_meshCollider = ChunkObject.AddComponent<MeshCollider>();
		var meshRenderer = ChunkObject.AddComponent<MeshRenderer>();

		meshRenderer.material = material;
		
		BuildMesh(_meshData.Vertices, _meshData.Triangles);
	}

	public bool ReRequestChunkData(bool queue = true)
	{
		var newChunkData = new MultiThreadingSupport.ChunkDataRequested
		{
			Chunkwidth = _chunkSettings.chunkwidth,
			ChunkHeight = _chunkSettings.chunkHeight,
			ChunkPos = _position,
			ChunkScale = _scale,
			WorldHeight = _chunkSettings.worldHeight,
			NoiseFilters = _noiseFilters,
			NoiseLayers = _chunkSettings.NoiseLayers,
			Freq = _chunkSettings.freq,
			Amp = _chunkSettings.amp,
			TerrainSurface = _chunkSettings.terrainSurface,
			smooth = _chunkSettings.smoothTerrain,
			flatShading = _chunkSettings.flatShading,
			caveThreshold = _chunkSettings.caveThreshold,
			caveAmp = _chunkSettings.caveAmpMult,
			caveFreq = _chunkSettings.caveFreqMult,
			caveHeightLimited = _chunkSettings.caveHeightLimited,
			caveMaxHeight = _chunkSettings.caveMaxHeight,
			chunkBelowZero = _chunkSettings.chunkBelowZero
		};
		
		switch (queue)
		{
			case false:
				_multiThreadingSupport.RequestChunkData(OnMeshDataReceived, _position, _scale, _noiseFilters, newChunkData);
				return true;
			case true:
				_multiThreadingSupport.QueueToThread.Add(this);
				break;
		}

		return false;
	}

	private void OnMeshDataReceived(MultiThreadingSupport.MeshData meshData)
    {
	    //_multiThreadingSupport.currentThreads--;
		_meshUpdate = false;
		_meshData = meshData;
		BuildMesh(_meshData.Vertices, _meshData.Triangles);
		if (ChunkGenerator.Instance.updatingChunks.Contains(ChunkPos))
			ChunkGenerator.Instance.updatingChunks.Remove(ChunkPos);
	}

	private void RecreateMesh()
	{
		var newChunkData = new MultiThreadingSupport.ChunkDataRequested
		{
			Chunkwidth = _chunkSettings.chunkwidth,
			ChunkHeight = _chunkSettings.chunkHeight,
			ChunkPos = _position,
			ChunkScale = _scale,
			WorldHeight = _chunkSettings.worldHeight,
			NoiseFilters = _noiseFilters,
			NoiseLayers = _chunkSettings.NoiseLayers,
			Freq = _chunkSettings.freq,
			Amp = _chunkSettings.amp,
			TerrainSurface = _chunkSettings.terrainSurface,
			smooth = _chunkSettings.smoothTerrain,
			flatShading = _chunkSettings.flatShading,
			caveThreshold = _chunkSettings.caveThreshold,
			caveAmp = _chunkSettings.caveAmpMult,
			caveFreq = _chunkSettings.caveFreqMult,
			caveHeightLimited = _chunkSettings.caveHeightLimited,
			caveMaxHeight = _chunkSettings.caveMaxHeight,
			chunkBelowZero = _chunkSettings.chunkBelowZero
		};
		
		_meshData.Triangles.Clear();
		_meshData.Vertices.Clear();
		_multiThreadingSupport.RequestMeshData(OnMeshDataReceived, _meshData, newChunkData);
	}

	private void BuildMesh(List<Vector3> vertices, List<int> triangles)
	{
		var mesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = triangles.ToArray()
		};
		mesh.RecalculateNormals();
		if(_meshFilter != null)
		{
			_meshFilter.mesh = mesh;
		}
		if (_meshCollider != null)
		 	_meshCollider.sharedMesh = mesh;
		if (ChunkObject != null)
			ChunkObject.transform.localScale = new Vector3(_scale, _scale, _scale);
		if (!ChunkGenerator.Instance.CreatedMeshes.ContainsKey(_position))
			ChunkGenerator.Instance.CreatedMeshes.Add(_position, _meshData);
	}

	public void EditTerrain(List<Vector3> pos, float val)
	{
		if (!_meshUpdate)
		{
			var width = _chunkSettings.chunkwidth + 1;
			var height = _chunkSettings.chunkHeight + 1 + _chunkSettings.chunkBelowZero;
			
			_meshUpdate = true;
			var cancel = true;
			foreach (var v3Int in from i in pos where !(i.z < ChunkPos.z) && !(i.z > ChunkPos.z + _chunkSettings.chunkwidth) where !(i.x < ChunkPos.x) && !(i.x > ChunkPos.x + _chunkSettings.chunkwidth) where !(i.y < ChunkPos.y + _chunkSettings.editingRadious) && !(i.y > ChunkPos.y + _chunkSettings.chunkHeight * 2) select val > 0f
				? new Vector3Int(Mathf.FloorToInt(i.x) - _position.x, Mathf.FloorToInt(i.y) - _position.y,
					Mathf.FloorToInt(i.z) - _position.z)
				: new Vector3Int(Mathf.CeilToInt(i.x) - _position.x, Mathf.CeilToInt(i.y) - _position.y,
					Mathf.CeilToInt(i.z) - _position.z))
			{
				try
				{
					cancel = false;
					_meshData.TerrainMap[v3Int.x * height * width + v3Int.y * width + v3Int.z] = val;
				}
				catch
				{
					// ignored
				}
			}

			if(!cancel)
				RecreateMesh();
            else
            {
				_meshUpdate = false;
				if (ChunkGenerator.Instance.updatingChunks.Contains(ChunkPos))
					ChunkGenerator.Instance.updatingChunks.Remove(ChunkPos);
			}
		}
        else
        {
			var temp = new ChunkGenerator.ChunkUpdateQueue
			{
				Chunk = this,
				Pos = pos,
				Val = val
			};
			ChunkGenerator.Instance.QueuedChunkUpdates.Add(temp);
		}
	}
}

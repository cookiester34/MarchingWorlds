using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ChunkSettings : ScriptableObject
{
	public bool occlusion = false;
	public bool limitThreads = false;
	public bool threadChunkEditing = false;
	public int seed = 225;
	[Range(3, 64)] public int viewDistance = 7;
	[HideInInspector] public float RealViewDistance;
	[Range(1, 24)] public int maxNumberOfThreadsPerFrame = 8;
	[HideInInspector] public int chunkScale = 1;
	[SerializeField] public List<NoiseLayer> NoiseLayers;
	[SerializeField] public List<LodLayer> LodLayers;
	public float heightMultiplier = 13f;
	[Range(150, 420)] public int chunkHeight = 250;
	[Range(5, 18)] public int chunkwidth = 5;
	[Range(0, 150)] public int chunkBelowZero = 30;
	[HideInInspector] public float worldHeight = 275;
	[Range(0f, 6f)] public float amp = 1.6f;
	[Range(0.1f, 0.3f)] public float freq = 0.13f;
	[HideInInspector]public float terrainSurface = 0.5f;
	[Range(0, 100)] public float caveThreshold = 25f;
	[Range(0.1f, 8f)] public float caveAmpMult = 1.6f;
	[Range(0.1f, 8f)] public float caveFreqMult = 0.13f;
	public bool caveHeightLimited = false;
	[Range(0, 420)] public float caveMaxHeight = 75f;
	public bool smoothTerrain = false;
	public bool flatShading = false;
	public bool allowEditing = false;
	public float editingRadious = 2f;
	public TerrainShaderData terrainShaderData;

	[System.Serializable]
	public class NoiseLayer
	{
		public bool enabled;
		public bool useFirstLayerAsMask;
		public NoiseSettings noiseSettings;
	}

	[System.Serializable]
	public class LodLayer
	{
		public bool disabled = false;
		[Range(1, 6)] public int Lod = 0;
		[Range(3, 64)] public int distance;
	}
}
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
	[Range(1, 6)] public int maxNumberOfThreadsPerFrame = 2;
	[HideInInspector] public int chunkScale = 1;
	public AnimationCurve heightCurve;
	[SerializeField] public List<NoiseLayer> NoiseLayers;
	[Range(0,4)] public int LOD;
	[Range(150, 420)] public int chunkHeight = 250;
	[Range(5, 30)] public int chunkwidth = 5;
	[Range(0, 150)] public int chunkBelowZero = 30;
	[HideInInspector] public float worldHeight = 275;
	[Range(0f, 3f)] public float amp = 1.6f;
	[Range(0.1f, 0.3f)] public float freq = 0.13f;
	public float terrainSurface = 0.5f;
	[Range(0, 100)] public float caveThreshold = 25f;
	[Range(0.1f, 8f)] public float caveAmpMult = 1.6f;
	[Range(0.1f, 8f)] public float caveFreqMult = 0.13f;
	public bool caveHeightLimited = false;
	[Range(0, 420)] public float caveMaxHeight = 75f;
	public bool smoothTerrain = false;
	[HideInInspector] public bool flatShading = false;
	public bool allowEditing = false;
	public float editingRadious = 2f;

	[System.Serializable]
	public class NoiseLayer
	{
		public bool enabled;
		public bool useFirstLayerAsMask;
		public NoiseSettings noiseSettings;
	}
}
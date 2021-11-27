using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ChunkSettings : ScriptableObject
{
	public bool occlusion = false;
	public bool limitThreads = false;
	public int seed = 225;
	[Range(10, 300)]
	public int viewDistance = 10;
	[Range(1, 12)]
	public int maxNumberOfThreadsPerFrame = 5;
	[HideInInspector]
    public int chunkScale = 1;
    
    [SerializeField]
    public List<NoiseLayer> NoiseLayers;

	[HideInInspector]
	public int chunkHeight = 275;
	[HideInInspector]
	public int chunkwidth = 10;

	[HideInInspector]
	public float worldHeight = 275;
	[Range(0f,3f)]
	public float amp = 1.6f;
	[Range(0.1f,0.3f)]
	public float freq = 0.13f;

	public float terrainSurface = 0.5f;

	public bool smoothTerrain = false;
	
	[HideInInspector]
	public bool flatShading = false;

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
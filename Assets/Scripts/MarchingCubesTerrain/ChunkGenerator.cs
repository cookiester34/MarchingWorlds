using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MultiThreadingSupport))]
public class ChunkGenerator : MonoBehaviour
{
    public static ChunkGenerator Instance;

    public List<Vector3Int> updatingChunks = new List<Vector3Int>();
    public readonly List<ChunkUpdateQueue> QueuedChunkUpdates = new List<ChunkUpdateQueue>();

    public Camera cam;
    
    private readonly Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
    public readonly Dictionary<Vector3Int, MultiThreadingSupport.MeshData> CreatedMeshes = new Dictionary<Vector3Int, MultiThreadingSupport.MeshData>();

    public Transform viewer;
    [HideInInspector]
    public Vector2 viewerPosition;

    public Transform playerCamera;

    public Rigidbody viewerRb;

    public Material material;

    public ChunkSettings chunkSettings;

    private float OldviewDistance;

    public bool threadingChunks = false;
    public int waitingOnChunks = 0;

    private int _frames = 0;

    
    public class ChunkUpdateQueue
    {
        public Chunk Chunk;
        public List<Vector3> Pos;
        public float Val;
    }


    private void Start()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        chunkSettings.flatShading = !chunkSettings.smoothTerrain;

        if (chunkSettings.chunkScale != 1)
            chunkSettings.allowEditing = false;

        chunkSettings.worldHeight = chunkSettings.chunkHeight + 35;

        chunkSettings.RealViewDistance = chunkSettings.viewDistance * chunkSettings.chunkwidth;
        OldviewDistance = chunkSettings.viewDistance;

        CheckLOD();
    }

    private void CheckLOD()
    {
        if(chunkSettings.LOD == 0) return;
        var chunkSettingsLOD = chunkSettings.LOD * 2;
        if (chunkSettings.chunkwidth % chunkSettingsLOD == 0) return;
        if(chunkSettings.chunkwidth > chunkSettingsLOD && chunkSettingsLOD >= 6)
            chunkSettings.chunkwidth--;
        else
            chunkSettings.chunkwidth++;
        CheckLOD();
    }

    private void FixedUpdate()
    {
        var position = viewer.position;
        viewerPosition = new Vector2(position.x, position.z);
        UpdateVisibleChunks();

        if (chunkSettings.viewDistance != OldviewDistance)
        {
            chunkSettings.RealViewDistance = chunkSettings.viewDistance * chunkSettings.chunkwidth;
            OldviewDistance = chunkSettings.viewDistance;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
        if (Input.GetKeyDown(KeyCode.L))
            Cursor.lockState = CursorLockMode.Locked;
        
        if (!chunkSettings.allowEditing) return;
        _frames++;
        if (_frames % 4 != 0) return;
        _frames = 0;
        EditTerrainHold();
        // if (QueuedChunkUpdates.Count <= 0) return;
        // QueuedChunkUpdates[0].Chunk.EditTerrain(QueuedChunkUpdates[0].Pos, QueuedChunkUpdates[0].Val);
        // QueuedChunkUpdates.RemoveAt(0);
    }

    private int RoundToNearest(float num, int factor)
    {
        var temp = Mathf.Round(num / factor);
        return (int)temp * factor;
    }

    private void UpdateVisibleChunks()
    {
        var posX = Mathf.RoundToInt(viewerPosition.x);
        var posZ = Mathf.RoundToInt(viewerPosition.y);
        var playerPosition = new Vector3(posX, 0, posZ);
        var width = chunkSettings.chunkwidth * chunkSettings.chunkScale;

        for (var i = posX - (chunkSettings.RealViewDistance * 2 * chunkSettings.chunkScale); i < posX + (chunkSettings.RealViewDistance * 2 * chunkSettings.chunkScale); i+= width)
        {
            for (var j = posZ - (chunkSettings.RealViewDistance * 2 * chunkSettings.chunkScale); j < posZ + (chunkSettings.RealViewDistance * 2 * chunkSettings.chunkScale); j += width)
            {
                var chunkX = RoundToNearest(i, width);
                var chunkZ = RoundToNearest(j, width);
                var pos = new Vector3Int(chunkX, 0, chunkZ);
                
                
                var distance = Vector3.Distance(playerPosition, pos);
                if (distance < chunkSettings.RealViewDistance && OnScreen(pos) || distance < chunkSettings.chunkwidth * 4)
                {
                    if (_chunks.ContainsKey(pos)) continue;
                    Chunk tempChunk;
                    
                    var chunkLod = chunkSettings.LOD;
                    
                    if (CreatedMeshes.ContainsKey(pos))
                    {
                        tempChunk = new Chunk(chunkSettings, pos, material, MultiThreadingSupport.Instance,
                            CreatedMeshes[pos], transform);
                    }
                    else
                    {
                        tempChunk = new Chunk(chunkSettings, pos, material, MultiThreadingSupport.Instance,
                            transform, chunkLod);
                    }

                    _chunks.Add(pos, tempChunk);
                }
                else if (_chunks.ContainsKey(pos))
                {
                    Destroy(_chunks[pos].ChunkObject);
                    _chunks.Remove(pos);
                }
            }
        }
	}

    private bool OnScreen(Vector3 pos)
    {
        if (!chunkSettings.occlusion)
            return true;
        float width = chunkSettings.chunkwidth * 2;
        var position = viewer.position;
        pos += new Vector3(0, position.y, 0);
        var screenPoint1 = cam.WorldToViewportPoint(pos);
        var screenPoint2 = cam.WorldToViewportPoint(pos + new Vector3(width, 0, width));
        var screenPoint3 = cam.WorldToViewportPoint(pos  + new Vector3(width, 0, 0));
        var screenPoint4 = cam.WorldToViewportPoint(pos + new Vector3(0, 0, width));
        var screenPoint5 = cam.WorldToViewportPoint(pos + new Vector3(-width, 0, -width));
        
        return screenPoint1.z > 0 && screenPoint1.x is > 0 and < 1|| 
               screenPoint2.z > 0 && screenPoint2.x is > 0 and < 1||
               screenPoint3.z > 0 && screenPoint3.x is > 0 and < 1||
               screenPoint4.z > 0 && screenPoint4.x is > 0 and < 1||
               screenPoint5.z > 0 && screenPoint5.x is > 0 and < 1;
    }

    private void EditTerrainHold()
    {
        if (waitingOnChunks <= 0)
        {
            threadingChunks = true;
        }
        
        if (updatingChunks.Count > 0 || !threadingChunks) return;
        
        if (!Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1)) return;
        
        RaycastHit hit;
        if (!Physics.Raycast(viewer.position, viewer.transform.TransformDirection(Vector3.forward), out hit, 10)) return;
        
        if (!(hit.point.y > -chunkSettings.chunkBelowZero + 1)) return;
        
        var val = Input.GetKey(KeyCode.Mouse0) ? 0f : 1f;
        
        var newChunkData = new MultiThreadingSupport.ChunkDataRequested
        {
            Chunkwidth = chunkSettings.chunkwidth,
            ChunkHeight = chunkSettings.chunkHeight,
            ChunkScale = chunkSettings.chunkScale,
            WorldHeight = chunkSettings.worldHeight,
            NoiseLayers = chunkSettings.NoiseLayers,
            Freq = chunkSettings.freq,
            Amp = chunkSettings.amp,
            TerrainSurface = chunkSettings.terrainSurface,
            smooth = chunkSettings.smoothTerrain,
            flatShading = chunkSettings.flatShading,
            caveThreshold = chunkSettings.caveThreshold,
            caveAmp = chunkSettings.caveAmpMult,
            caveFreq = chunkSettings.caveFreqMult,
            caveHeightLimited = chunkSettings.caveHeightLimited,
            caveMaxHeight = chunkSettings.caveMaxHeight,
            chunkBelowZero = chunkSettings.chunkBelowZero,
            heightCurve = chunkSettings.heightCurve,
            Lod = chunkSettings.LOD
        };

        if(chunkSettings.threadChunkEditing)
        {
            threadingChunks = true;
            waitingOnChunks++;
            MultiThreadingSupport.Instance.RequestGetNeighbourChunksFromPos(EditEffectedChunks, newChunkData,
                chunkSettings.chunkwidth, chunkSettings.editingRadious, hit.transform.position, hit.point, _chunks,
                val);
        }
        else
        {
            var points = GetCirclePoints(hit.point, chunkSettings.editingRadious);
            var js = GetNeighbourChunksFromPos(hit.transform.position);
            foreach (var j in js)
            {
                updatingChunks.Add(j.ChunkPos);
                j.EditTerrain(points, val, chunkSettings.LOD);
            }
        }
    }

    private void EditEffectedChunks(List<Chunk> _chunkList)
    {
        waitingOnChunks += _chunks.Count;
        foreach (var j in _chunkList.Where(j => _chunks.ContainsValue(j)))
        {
            var chunk = _chunks[j.ChunkPos];
            chunk._meshData = j._meshData;
            chunk.OnEditReturn();
        }
        waitingOnChunks--;
    }

    public Chunk GetChunkFromPos(Vector3 pos)
    {
        var x = Mathf.CeilToInt(pos.x);
        var y = Mathf.CeilToInt(pos.y);
        var z = Mathf.CeilToInt(pos.z);

        return _chunks[new Vector3Int(x, y, z)];
    }

    private List<Chunk> GetNeighbourChunksFromPos(Vector3 pos)
    {
        var x = Mathf.CeilToInt(pos.x);
        var y = Mathf.CeilToInt(pos.y);
        var z = Mathf.CeilToInt(pos.z);
        var tempVector3 = new Vector3Int(x, y, z);

        var chunkList = new List<Chunk>();
        var numOfChunks = (int) (chunkSettings.editingRadious * 4 / 10);
        if (numOfChunks < 1) numOfChunks = 1;
        for (var i = -numOfChunks; i <= numOfChunks; i++)
        {
            for (var j = -numOfChunks; j <= numOfChunks; j++)
            {
                var tempChunkPos = new Vector3Int(tempVector3.x - chunkSettings.chunkwidth * j, 0,
                    tempVector3.z - chunkSettings.chunkwidth * i);
                if (_chunks.ContainsKey(tempChunkPos)) chunkList.Add(_chunks[tempChunkPos]);
            }
        }

        return chunkList;
    }

    private List<Vector3> GetCirclePoints(Vector3 pos, float radius = 0.87f)
    {
        Vector3Int posInt = new Vector3Int((int) pos.x, (int) pos.y, (int) pos.z);
        var gridPoints = new List<Vector3>();
        var radiusCeil = Mathf.CeilToInt(radius);
        for (var i = -radiusCeil; i <= radiusCeil; i++)
        {
            for (var j = -radiusCeil; j <= radiusCeil; j++)
            {
                for (var k = -radiusCeil; k <= radiusCeil; k++)
                {
                    var gridPoint = new Vector3(posInt.x + i,posInt.y + j,posInt.z + k);
                    if (Vector3.Distance(posInt, gridPoint) <= radius)
                    {
                        gridPoints.Add(gridPoint + new Vector3(0, chunkSettings.chunkBelowZero));
                    }
                }
            }
        }
        return gridPoints;
    }

    public List<Vector3> GetGridPoints(Vector3 pos, float _width = 2f, float _length = 2f, float _hieght = 2f)
    {
        var gridPoints = new List<Vector3>();
        var width = Mathf.CeilToInt(_width);
        var length = Mathf.CeilToInt(_length);
        var height = Mathf.CeilToInt(_hieght);

        for (var i = -width; i <= width; i++)
        {
            for(var j = -height; j <= height; j++)
            {
                for(var k = -length; k <= length; k++)
                {
                    var gridPoint = new Vector3(Mathf.FloorToInt(pos.x + i),
                                                    Mathf.FloorToInt(pos.y + j),
                                                    Mathf.FloorToInt(pos.z + k));

                    gridPoints.Add(gridPoint);
                }
            }
        }

        return gridPoints;
    }
}

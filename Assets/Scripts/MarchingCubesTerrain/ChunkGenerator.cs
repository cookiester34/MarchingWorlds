using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MultiThreadingSupport))]
public class ChunkGenerator : MonoBehaviour
{
    public static ChunkGenerator Instance;
    public SensorToolkit.RaySensor sensorPoint;
    public SensorToolkit.RaySensor sensorTerrain;
    public Transform placementPoint;

    public List<Vector3Int> updatingChunks = new List<Vector3Int>();
    public readonly List<ChunkUpdateQueue> QueuedChunkUpdates = new List<ChunkUpdateQueue>();

    public Camera cam;

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
    }

    //pool chunks

    private readonly Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
    public readonly Dictionary<Vector3Int, MultiThreadingSupport.MeshData> CreatedMeshes = new Dictionary<Vector3Int, MultiThreadingSupport.MeshData>();

    public Transform viewer;
	[HideInInspector]
	public Vector2 viewerPosition;

    public Transform playerCamera;

    public Rigidbody viewerRb;

    public Material material;

    public ChunkSettings chunkSettings;

    private void LateUpdate()
    {
        var position = viewer.position;
        viewerPosition = new Vector2(position.x, position.z);
        UpdateVisibleChunks();

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
        if (Input.GetKeyDown(KeyCode.L))
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!chunkSettings.allowEditing) return;
        // frames++;
        // if (frames % 2 == 0)
        // {
        //frames = 0;
        EditTerrainHold();
        // }
        if (QueuedChunkUpdates.Count <= 0) return;
        QueuedChunkUpdates[0].Chunk.EditTerrain(QueuedChunkUpdates[0].Pos, QueuedChunkUpdates[0].Val);
        QueuedChunkUpdates.RemoveAt(0);
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

        for (var i = posX - (chunkSettings.viewDistance * 2 * chunkSettings.chunkScale); i < posX + (chunkSettings.viewDistance * 2 * chunkSettings.chunkScale); i+= width)
        {
            for (var j = posZ - (chunkSettings.viewDistance * 2 * chunkSettings.chunkScale); j < posZ + (chunkSettings.viewDistance * 2 * chunkSettings.chunkScale); j += width)
            {
                var chunkX = RoundToNearest(i, width);
                var chunkZ = RoundToNearest(j, width);
                var pos = new Vector3Int(chunkX, 0, chunkZ);
                
                
                var distance = Vector3.Distance(playerPosition, pos);
                if (distance < chunkSettings.viewDistance && OnScreen(pos) || distance < chunkSettings.chunkwidth * 4)
                {
                    if (_chunks.ContainsKey(pos)) continue;
                    Chunk tempChunk;
                    if (CreatedMeshes.ContainsKey(pos))
                    {
                        tempChunk = new Chunk(chunkSettings, pos, material, MultiThreadingSupport.Instance,
                            CreatedMeshes[pos], transform);
                    }
                    else
                    {
                        tempChunk = new Chunk(chunkSettings, pos, material, MultiThreadingSupport.Instance,
                            transform);
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
        
        return screenPoint1.z > 0 && screenPoint1.x > 0 && screenPoint1.x < 1|| 
               screenPoint2.z > 0 && screenPoint2.x > 0 && screenPoint2.x < 1||
               screenPoint3.z > 0 && screenPoint3.x > 0 && screenPoint3.x < 1||
               screenPoint4.z > 0 && screenPoint4.x > 0 && screenPoint4.x < 1||
               screenPoint5.z > 0 && screenPoint5.x > 0 && screenPoint5.x < 1;
    }

    private void EditTerrainHold()
    {
        if (updatingChunks.Count <= 0)
        {
            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
            {
                if (sensorPoint.DetectedObjects.Count > 0)
                {
                    if (sensorPoint.DetectedObjectRayHits[0].point.y > 15)
                    {
                        var val = Input.GetKey(KeyCode.Mouse0) ? 0f : 1f;
                        foreach (var j in GetNeighbourChunksFromPos(sensorPoint.DetectedObjectRayHits[0].transform.position))
                        {
                            updatingChunks.Add(j.ChunkPos);
                            var points = GetCirclePoints(sensorPoint.DetectedObjectRayHits[0].point, chunkSettings.editingRadious);
                            j.EditTerrain(points, val);
                            foreach (var dir in from i in points where Vector3.Distance(i, viewer.position) < 1.5f && val == 0f select new Vector3(i.x, i.y -2, i.z) - new Vector3(viewer.position.x, viewer.position.y + 2, viewer.position.z) into dir select -dir.normalized)
                            {
                                // And finally we add force in the direction of dir and multiply it by force. 
                                // This will push back the player
                                viewerRb.AddForce(dir * 700);
                                break;
                            }
                        }
                    }
                }
            }
        }
        if (sensorPoint.DetectedObjects.Count > 0)
        {
            placementPoint.position = sensorPoint.DetectedObjectRayHits[0].point;
        }
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
        for (var i = -1; i < 2; i++)
        {
            for (var j = -1; j < 2; j++)
            {
                var tempChunkPos = new Vector3Int(tempVector3.x - (chunkSettings.chunkwidth * j), 0, tempVector3.z - (chunkSettings.chunkwidth * i));
                chunkList.Add(_chunks[tempChunkPos]);
            }
        }

        return chunkList;
    }

    private static List<Vector3> GetCirclePoints(Vector3 pos, float radius = 0.87f)
    {
        var gridPoints = new List<Vector3>();
        var radiusCeil = Mathf.CeilToInt(radius);
        for (var i = -radiusCeil; i <= radiusCeil; i++)
        {
            for (var j = -radiusCeil; j <= radiusCeil; j++)
            {
                for (var k = -radiusCeil; k <= radiusCeil; k++)
                {
                    var gridPoint = new Vector3(Mathf.FloorToInt(pos.x + i),
                                                    Mathf.FloorToInt(pos.y + j),
                                                    Mathf.FloorToInt(pos.z + k));
                    if (Vector3.Distance(pos, gridPoint) <= radius)
                    {
                        gridPoints.Add(gridPoint);
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

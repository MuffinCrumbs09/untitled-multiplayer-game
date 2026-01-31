
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
//using Unity.AI.Navigation;

public class HexMaze_Manager : MonoBehaviour
{

    public GameObject tilePrefab;
    public bool genWalls;
    public GameObject wallPrefab;
    public GameObject wallPrefabColliderOnly;
    [ContextMenuItem("RecalcMaze", "InitMaze")]
    public uint mazeRadius = 3;
    //public NavMeshSurface surface;
    public float magicNumber;
    public uint RandomSeed = 666;
    public bool supressGenOnStart = false;
    private Unity.Mathematics.Random random;

    private const float offset = 5.2f;
    private Mesh _meshTile;
    private Material _materialTile;
    private RenderParams _rpTile;

    private Mesh _meshWall;
    private Material _materialWall;
    private RenderParams _rpWall;

    private int hexGridSize;
    private int solidWalls;
    private class HexLink
    {
        // In C#, references are essentially managed pointers
        public GameObject wall;
        
        // Fixed-size array for the two tiles this link connects
        public HexTile[] tiles = new HexTile[2] { null, null};
        public bool solid = true;
        public HexTile Next(HexTile current) {
            if (current == tiles[0])
                return tiles[1];
            else
                return tiles[0];
        }
    }

    private class HexTile
    {
        //public GameObject TileObject;
        public Hex pos;
        public int pointsAt = -1; //-2 = origin, -1 = not set
        // Each tile is surrounded by 6 links
        public HexLink[] links = new HexLink[6] { null , null , null , null , null , null };

        internal HexLink[] Friends()
        {
            List<HexLink> fList = new(5);
            ;
            foreach (var friend in links)
            {
                if (friend.Next(this) != null && friend.Next(this).pointsAt == -1) {
                    fList.Add(friend);
                }
            }
            return fList.ToArray();
        }
    }
    private HexTile CoreTile = new();

    
    private GraphicsBuffer tilePositionBuffer;
    private GraphicsBuffer wallPositionBuffer;
    private GraphicsBuffer PlayerPositionBuffer;
    private List<HexTile> hexGrid;
    //private LinkedList<HexTile> hexLinks;

    void Awake()
    {
        if (tilePrefab != null)
        {
            _meshTile = tilePrefab.GetComponent<MeshFilter>().sharedMesh;
            _materialTile = tilePrefab.GetComponent<MeshRenderer>().sharedMaterial;
        }
        _rpTile = new RenderParams(_materialTile);

        if (wallPrefab != null)
        {
            _meshWall = wallPrefab.GetComponent<MeshFilter>().sharedMesh;
            _materialWall = wallPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        }

        _rpWall = new RenderParams(_materialWall);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //CoreTile.TileObject = gameObject;
        
        if (!supressGenOnStart)
            InitMaze();
    }

    int HexVec3ToIndex(Vector2Int hexCoord) {
        return 0;
    }


    //implementation based on https://www.redblobgames.com/grids/hexagons/
    struct Hex {
        public int q, r;
        public int s => -q - r;
        public Hex(int q, int r) : this()
        {
            this.q = q;
            this.r = r;
        }
        public static Hex operator +(Hex h, Hex k)
        {
            return new Hex(h.q + k.q, h.r + k.r);
        }
        public static Hex operator -(Hex h, Hex k)
        {
            return new Hex(h.q - k.q, h.r - k.r);
        }
        public static Hex operator *(Hex h, int k)
        {
            return new Hex(h.q * k, h.r * k);
        }
        public static bool operator ==(Hex h, Hex k)
        {
            return (h.q == k.q) && (h.r == k.r);
        }
        public static bool operator !=(Hex h, Hex k)
        {
            return (h.q != k.q) || (h.r != k.r);
        }
    }

    static readonly Hex[] hex_direction_vectors = new Hex[]
    {
        new Hex(+1, 0), new Hex(+1, -1), new Hex(0, -1),
        new Hex(-1, 0), new Hex(-1, +1), new Hex(0, +1)
    };

    int HexRing(Hex a) { 
        return (math.abs(a.q) + math.abs(a.r) + math.abs(a.s))/2;
    }

    int HexIndexRingStart(int radius) {
        return 1 + 3 * radius * (radius - 1);
    }

    int HexIndex2Ring(int index) {
        return (int)math.floor((math.sqrt(12 * index - 3) + 3) / 6);
    }

    Hex RingIndex2hexInRing(int ringIndex, int ring) {
        int turns = (int)math.floor(ringIndex / ring);
        int lastSteps = ringIndex - (turns * ring);
        Hex res = new Hex( -ring, ring);
        for (int i = 0; i < turns; i++){
            res += hex_direction_vectors[i] * ring;
        }
        if (lastSteps > 0)
        {
            res += hex_direction_vectors[turns] * lastSteps;
        }
        return res;
    }

    Hex Index2Hex(int index) {
        int ring = HexIndex2Ring(index);
        int ringStart = HexIndexRingStart(ring);

        Hex res = RingIndex2hexInRing(index- ringStart, ring);
        return res;
    }

    UnityEngine.Vector2 HexToWorld(Hex pos) {
        // can be made into matrix mul
        float x = (3.0f/ 2.0f * pos.q);
        float y = (math.sqrt(3) / 2.0f * pos.q + math.sqrt(3.0f) * pos.r);
        return new UnityEngine.Vector2( x, y);
    }

    int Hex2Index(Hex pos) {
        int radius = HexRing(pos);
        //slow but not the worse
        for (int i = HexIndexRingStart(radius);i < HexIndexRingStart(radius+1);i++ ) {
            if (pos == Index2Hex(i))
                return i;
        }
        return -1;
    }

    public void InitMaze() {

        if (!Application.isPlaying)
        {
            return;
        }
        
        random = new Unity.Mathematics.Random(RandomSeed);

            //cleanUp
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        hexGridSize = 1 + (3 * (int)mazeRadius * ((int)mazeRadius + 1));
        hexGrid = new List<HexTile>(hexGridSize); //this one is zero wasted space.
        CoreTile = new HexTile();
        CoreTile.pointsAt = -2;


        UnityEngine.Matrix4x4[] tileMats;
        tileMats = new UnityEngine.Matrix4x4[hexGridSize - 1]; 
        hexGrid.Add(CoreTile);
        //hexGrid[0] = CoreTile;
        for (int i = 0; i < hexGridSize-1; i++)
        {
            Hex tileHex = Index2Hex(i+1);
            //hexGrid[i].pos = tileHex;
            HexTile t = new HexTile();
            t.pos = tileHex;
            hexGrid.Add(t);
            UnityEngine.Vector2 tempPos = HexToWorld(tileHex);
            //UnityEngine.Vector3 wPos = new UnityEngine.Vector3(tempPos.x,0,tempPos.y) * offset;
            UnityEngine.Vector3 wPos = (new UnityEngine.Vector3(tempPos.x, 0, tempPos.y) * 300.0f);
            //UnityEngine.Matrix4x4 managerMatrix = transform.localToWorldMatrix;

            // Remove translation (Column 3)
            //managerMatrix.SetColumn(3, new UnityEngine.Vector4(0, 0, 0, 1));
            tileMats[i] = transform.localToWorldMatrix * UnityEngine.Matrix4x4.TRS(wPos,UnityEngine.Quaternion.identity, UnityEngine.Vector3.one);
        }
        tilePositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, hexGridSize - 1, Marshal.SizeOf<UnityEngine.Matrix4x4>());
        tilePositionBuffer.SetData(tileMats);
        Material instanceMat = new Material(tilePrefab.GetComponent<MeshRenderer>().sharedMaterial);
        //instanceMat.EnableKeyword("_PROCEDURAL_ON");
        //instanceMat.SetKeyword("_PROCEDURAL_ON",true);
        instanceMat.SetFloat("_IsProcedural", 1f);
        instanceMat.SetBuffer("_PositionBuffer", tilePositionBuffer);
        _rpTile = new RenderParams(instanceMat);
        {
            Bounds b = new Bounds(transform.position, UnityEngine.Vector3.one);

            UnityEngine.Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 1; i < tileMats.Length; i++)
            {
                b.Encapsulate((worldToLocal.MultiplyPoint( tileMats[i].GetPosition())));
                //b.Encapsulate((worldToLocal.MultiplyPoint( tileMats[i].GetPosition())+ ((transform.localPosition / 10000.0f) + (Vector3.one * 0.005f))));
            }
            //b.Expand(2.0f);

            _rpTile.worldBounds = b;
        }

        _rpTile.material.SetBuffer("_PositionBuffer", tilePositionBuffer);
        //_rp = new RenderParams(_material);

        if (genWalls)
            GenWalls();
    }
    private void GenWalls()
    {
        LinkedList<HexLink> hexLinks = new();
        //link the tiles
        for (int i = 0; i < hexGrid.Count; i++)
        {
            Hex currentTile = Index2Hex(i);
            for (int j = 0; j < 6; j++)
            {
                if (hexGrid[i].links[j] == null)
                {
                    HexLink l = new HexLink();
                    l.tiles[0] = hexGrid[i];
                    hexLinks.AddLast(l);
                    hexGrid[i].links[j] = l;
                    Hex nearb = hexGrid[i].pos + hex_direction_vectors[j];
                    if (HexRing(nearb) <= mazeRadius)
                    {
                        l.tiles[1] = hexGrid[Hex2Index(nearb)];
                        hexGrid[Hex2Index(nearb)].links[(j + 3) % 6] = l;
                    }
                }
            }
        }

        //gen maze (hunt and kill)
        //hunt
        for (int i = 0; i < hexGrid.Count; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                HexTile check = hexGrid[i].links[j].Next(hexGrid[i]);
                if ((check != null) && check.pointsAt == -1)
                {
                    //kill
                    hexGrid[i].links[j].solid = false; //opening up the walls
                    HexTile victim = check;
                    bool killing = true;
                    while (killing)
                    {
                        HexLink[] nextVictims = victim.Friends();
                        victim.pointsAt = 0; //no need to backtrack or keep them alive.
                        killing = false;
                        if (nextVictims.Length > 0)
                        {
                            HexLink victimWall = nextVictims[random.NextInt(nextVictims.Length)];
                            victimWall.solid = false;
                            victim = victimWall.Next(victim);
                            killing = true;
                        }
                    }
                }
            }
        }
        //all of them are now very dead

        //making entrance
        List<HexLink> hexLinksEdge = new(hexLinks.Count / 2);
        int edgeWallCount = 0;
        foreach (var edgeLink in hexLinks)
        {
            if (edgeLink.tiles[1] == null)
            {
                hexLinksEdge.Add(edgeLink);
                edgeWallCount++;
            }
        }
        hexLinksEdge[random.NextInt(edgeWallCount)].solid = false;
        
        //collsion
        solidWalls = 0;
        foreach (var link in hexLinks)
        {
            if (link.solid)
            {
                solidWalls++;
                UnityEngine.Vector2 t1 = HexToWorld(link.tiles[0].pos), t2, mid;
                if (link.tiles[1] != null)
                {
                    t2 = HexToWorld(link.tiles[1].pos);

                }
                else
                {
                    t2 = UnityEngine.Vector2.zero;
                    for (int i = 0; i < 6; i++)
                    {
                        if (link.tiles[0].links[i] == link)
                        {
                            t2 = HexToWorld(link.tiles[0].pos + hex_direction_vectors[i]);
                            break;
                        }

                    }
                }
                mid = (t1 + t2) / 2.0f;
                float3 p1 = new float3(t1.x, 0, t1.y);
                float3 p2 = new float3(t2.x, 0, t2.y);
                float3 localMid = (p1 + p2) * 0.5f * 3.0f; // Multiplied by your scale factor 3

                // 1. Rotate the position offset to match the Manager
                float3 worldPos = (float3)transform.position + math.rotate(transform.rotation, localMid);

                // 2. Calculate the local look direction
                float3 localDir = p1 - p2;

                // 3. Combine Manager rotation with the LookRotation
                quaternion worldRot = math.mul(transform.rotation, quaternion.LookRotation(localDir, math.up()));

                // 4. Spawn
                link.wall = GameObject.Instantiate(wallPrefabColliderOnly, worldPos, worldRot, transform);
                link.wall.transform.localScale = Vector3.one;
            }
        }



        UnityEngine.Matrix4x4[] wallMats;
        wallMats = new UnityEngine.Matrix4x4[solidWalls];
        int matIndex = 0;
        foreach (var link in hexLinks)
            if (link.solid)
                wallMats[matIndex++] = link.wall.transform.localToWorldMatrix;

        PlayerPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5, Marshal.SizeOf<UnityEngine.Vector3>());
        

        wallPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, solidWalls, Marshal.SizeOf<UnityEngine.Matrix4x4>());
        wallPositionBuffer.SetData(wallMats);

        Material instanceMat = new Material(wallPrefab.GetComponent<MeshRenderer>().sharedMaterial);
        instanceMat.SetFloat("_IsProcedural", 1f);
        instanceMat.SetBuffer("_PositionBuffer", wallPositionBuffer);
        _rpWall = new RenderParams(instanceMat);
        {
            Bounds b = new Bounds(transform.position, UnityEngine.Vector3.one);

            UnityEngine.Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 1; i < wallMats.Length; i++)
            {
                b.Encapsulate((worldToLocal.MultiplyPoint(wallMats[i].GetPosition())));
                //b.Encapsulate(wallMats[i].GetPosition());
            }
            b.Expand(2.0f);

            _rpWall.worldBounds = b;
        }

        _rpWall.material.SetBuffer("_PositionBuffer", wallPositionBuffer);
        //_rp = new RenderParams(_material);
        
        //TODO updateNavMesh, all objects are added
    }
    private void LateUpdate()
    {
        
        List<GameObject> players = new List<GameObject>();
        GameObject.FindGameObjectsWithTag("Player", players);
        Vector3[] pos = new Vector3[players.Count];
        for (int i = 0; i < players.Count; i++)
        {
            pos[i] = players[i].transform.position;
        }
        PlayerPositionBuffer.SetData(pos);
        _rpWall.material.SetBuffer("_players", PlayerPositionBuffer);
        _rpWall.material.SetFloat("_Drop", magicNumber);
        _rpWall.material.SetFloat("_PlayerCount", players.Count);
        //Graphics.DrawMeshInstancedProcedural(_mesh,0,);
        Graphics.RenderMeshPrimitives(_rpTile, _meshTile, 0, hexGridSize - 1);
        Graphics.RenderMeshPrimitives(_rpWall, _meshWall, 0, solidWalls);
        //Graphics.RenderMeshInstanced(_rp,_mesh, 0, tileMats, hexGridSize-1, 0);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Vector3[] h = new Vector3[6];
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 6; i++)
        {
            h[i] = new Vector3(HexToWorld(hex_direction_vectors[i] * ((int)mazeRadius + 1)).x*3,0, HexToWorld(hex_direction_vectors[i]*((int)mazeRadius+1) * 3).y) + transform.position;
        }
        Gizmos.DrawLineList(h);
        Gizmos.color = Color.red;
        for (int i = 1; i < 7; i++)
        {
            h[i-1] = new Vector3(HexToWorld(hex_direction_vectors[i%6] * ((int)mazeRadius + 1)).x * 3, 0, HexToWorld(hex_direction_vectors[i%6] * ((int)mazeRadius + 1) * 3).y) + transform.position;
        }
        Gizmos.DrawLineList(h);
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimplexNoise;

public enum BrickType {
    None,

    Dirt,
    Sand,
    Ice,
    Snow,
    Grass
}

[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(MeshFilter))]

public class Chunk:MonoBehaviour {
    public static List<Chunk> chunksWaiting = new List<Chunk>();
    public static List<Chunk> chunks = new List<Chunk>();

    public static int width {
        get { return World.currentWorld.chunkWidth; }
    }
    public static int height {
        get { return World.currentWorld.chunkHeight; }
    }
    public static float brickHeight {
        get { return World.currentWorld.brickHeight; }
    }

    public byte[,,] map;

    public Mesh mesh;
    protected MeshRenderer meshRenderer;
    protected MeshCollider meshCollider;
    protected MeshFilter meshFilter;

    protected bool initialized = false;
    protected bool colliderUpdated = false;

    void Start(){
        chunks.Add(this);

        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        chunksWaiting.Add(this);

        if(chunksWaiting[0] == this){
            StartCoroutine(CalculateMap());
        }
    }

    void OnDestroy(){
        if(chunksWaiting.Contains(this)) chunksWaiting.Remove(this);
        chunks.Remove(this);
    }

    public virtual IEnumerator CalculateMap(){
        yield return 0;
        
        map = new byte[width, height, width];

        Random.seed = World.currentWorld.seed;

        int x = 0;
        int y = 0;
        int z = 0;

        Vector3 offset1 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);
        Vector3 offset2 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);
        Vector3 offset3 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);

        for(x = 0; x < World.currentWorld.chunkWidth; x++){
            for(y = 0; y < height; y++){
                for(z = 0; z < width; z++){
                    map[x,y,z] = GetTheoreticalByte(new Vector3(x,y,z) + transform.position, offset1, offset2, offset3);
                }
            }
        }
        StartCoroutine(CreateMesh());

        initialized = true;

        chunksWaiting.Remove(this);

        while(chunksWaiting.Count > 0 && chunksWaiting[0] == null){
            chunksWaiting.RemoveAt(0);
        }

        if(chunksWaiting.Count > 0){
            StartCoroutine(chunksWaiting[0].CalculateMap());
        }
    }

    public static byte GetTheoreticalByte(Vector3 pos){
        Random.seed = World.currentWorld.seed;

        Vector3 offset1 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);
        Vector3 offset2 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);
        Vector3 offset3 = new Vector3(10000 * Random.value, 10000 * Random.value, 10000 * Random.value);

        return GetTheoreticalByte(pos, offset1, offset2, offset3);
    }

    public static byte GetTheoreticalByte(Vector3 pos, Vector3 offset1, Vector3 offset2, Vector3 offset3){
        float wetness = CalculateNoise(pos, offset3, 0.001f);
        float rockiness = CalculateNoise(pos, offset3, 0.003f);

        Biome biome = World.GetIdealBiome(wetness, rockiness);

        float blobValue = CalculateNoise(pos, offset2, 0.05f);
        float mountainValue = CalculateNoise(pos, offset1, 0.009f);

        byte brick = biome.GetBrick(Mathf.FloorToInt(pos.y), mountainValue, blobValue, wetness, rockiness);
        
        return brick;

        /*float clusterValue = CalculateNoise(pos, offset3, 0.012f);
        int biomeIndex = Mathf.FloorToInt(clusterValue * World.currentWorld.biomes.Length);
        Biome biome = World.currentWorld.biomes[biomeIndex];

        float heightBase = biome.minHeight;
        float maxHeight = biome.maxHeight;
        float heightSwing = maxHeight - heightBase;

        float blobValue = CalculateNoise(pos, offset2, 0.05f);
        float mountainValue = CalculateNoise(pos, offset1, 0.009f);

        mountainValue += biome.mountainPowerBonus;
        if(mountainValue < 0){
            mountainValue = 0;
        }

        mountainValue = Mathf.Pow(mountainValue, biome.mountainPower);

        byte brick = 1;//biome.GetBrick(Mathf.FloorToInt(pos.y), mountainValue, blobValue);

        mountainValue *= heightSwing;
        mountainValue += heightBase;

        mountainValue += (blobValue * heightSwing / 5f) - heightSwing / 10f;

        if(mountainValue >= pos.y){
            return brick;
        }

        return 0;*/
    }

    public static float CalculateNoise(Vector3 pos, Vector3 offset, float scale){
        float noiseX = Mathf.Abs((pos.x + offset.x) * scale);
        float noiseY = Mathf.Abs((pos.y + offset.y) * scale);
        float noiseZ = Mathf.Abs((pos.z + offset.z) * scale);

        return (Noise.Generate(noiseX, noiseY, noiseZ) + 1) / 2;

        //return Mathf.Max(0, Noise.Generate(noiseX, noiseY, noiseZ));
    }

    public virtual IEnumerator CreateMesh(){
        if(this == null){
            while(chunksWaiting[0] == null){
                chunksWaiting.RemoveAt(0);

                if(chunksWaiting.Count > 0){
                    StartCoroutine(chunksWaiting[0].CalculateMap());
                }

                return false;
            }
        }
        int x = 0;
        int y = 0;
        int z = 0;

        mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for(x=0; x < width; x++){
            for(y=0; y < height; y++){
                for(z=0; z < width; z++){
                    if(map[x,y,z] == 0){
                        continue;
                    }

                    byte brick = map[x,y,z];

                    if(IsTransparent(x-1, y, z)){
                        BuildFace(brick, new Vector3(x,y,z), Vector3.up, Vector3.forward, false, verts, uvs, tris);
                    }

                    if(IsTransparent(x+1, y, z)){
                        BuildFace(brick, new Vector3(x+1,y,z), Vector3.up, Vector3.forward, true, verts, uvs, tris);
                    }

                    if(IsTransparent(x, y-1, z)){
                        BuildFace(brick, new Vector3(x,y,z), Vector3.forward, Vector3.right, false, verts, uvs, tris);
                    }

                    if(IsTransparent(x, y+1, z)){
                        BuildFace(brick, new Vector3(x,y+1,z), Vector3.forward, Vector3.right, true, verts, uvs, tris);
                    }

                    if(IsTransparent(x, y, z-1)){
                        BuildFace(brick, new Vector3(x,y,z), Vector3.up, Vector3.right, true, verts, uvs, tris);
                    }

                    if(IsTransparent(x, y, z+1)){
                        BuildFace(brick, new Vector3(x,y,z+1), Vector3.up, Vector3.right, false, verts, uvs, tris);
                    }
                }
            }

            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;

            yield return 0;
        }
    }

    public virtual void BuildFace(byte brick, Vector3 corner, Vector3 up, Vector3 right, bool reversed, List<Vector3> verts, List<Vector2> uvs, List<int> tris){
        int index = verts.Count;

        float uvRow = ((corner.y + up.y) % 7);
        if(uvRow >= 4) uvRow = 7 - uvRow;
        uvRow /= 4f;

        Vector2 uvCorner = new Vector2(0.00f, uvRow);

        if(brick < 8){
            uvCorner.y += 0.125f;
        }

        corner.y *= brickHeight;
        up.y *= brickHeight;
        right.y *= brickHeight;

        verts.Add(corner);
        verts.Add(corner + up);
        verts.Add(corner + up + right);
        verts.Add(corner + right);

        Vector2 uvWidth = new Vector2(0.125f, 0.125f);
        uvCorner.x += (float)((brick) % 8 - 1) / 8f;

        uvs.Add(uvCorner);
        uvs.Add(new Vector2(uvCorner.x, uvCorner.y + uvWidth.y));
        uvs.Add(new Vector2(uvCorner.x + uvWidth.x, uvCorner.y + uvWidth.y));
        uvs.Add(new Vector2(uvCorner.x + uvWidth.x, uvCorner.y));

        if(reversed){
            tris.Add(index + 0);
            tris.Add(index + 1);
            tris.Add(index + 2);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 0);
        }else{
            tris.Add(index + 1);
            tris.Add(index + 0);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 2);
            tris.Add(index + 0);
        }

    }

    public virtual bool IsTransparent(int x, int y, int z){
        if(y < 0) return false;

        byte brick = GetByte(x,y,z);

        switch(brick){
            case 0: return true;
            default: return false;
        }
    }

    public virtual byte GetByte(int x, int y, int z){
        if(y < 0 || y >= height){
            return 0;
        }

        Vector3 worldPos = new Vector3(x,y,z) + transform.position;

        if(!initialized){
            return GetTheoreticalByte(worldPos);
        }

        if(x < 0 || z < 0 || x >= width || z >= width){
            Chunk chunk = Chunk.FindChunk(worldPos);
            if(chunk == this) return 0;
            if(chunk == null){
                return GetTheoreticalByte(worldPos);
            }
            return chunk.GetByte(worldPos);
        }

        return map[x,y,z];
    }

    public virtual byte GetByte(Vector3 worldPos){
        worldPos -= transform.position;

        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);
        int z = Mathf.FloorToInt(worldPos.z);

        return GetByte(x,y,z);
    }

    public static Chunk FindChunk(Vector3 pos){
        for(int a = 0; a < chunks.Count; a++){
            Vector3 cPos = chunks[a].transform.position;

            if(pos.x < cPos.x || pos.z < cPos.z || pos.x >= cPos.x + width || pos.z >= cPos.z + width){
                continue;
            }
            return chunks[a];
        }

        return null;
    }

    public bool SetBrick(byte brick, Vector3 worldPos){
        worldPos -= transform.position;
        return SetBrick(brick, Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), Mathf.FloorToInt(worldPos.z));
    }

    public bool SetBrick(byte brick, int x, int y, int z){
        if(x < 0 || y < 0 || z < 0 || x >= width || y >= height || z >= width){
            return false;
        }

        if(map[x,y,z] == brick) return false;

        map[x,y,z] = brick;

        StartCoroutine(CreateMesh());

        if(x == 0){
            Chunk chunk = FindChunk(new Vector3(x - 2, y, z) + transform.position);
            if(chunk != null) StartCoroutine(chunk.CreateMesh());
        }

        if(x == width - 1){
            Chunk chunk = FindChunk(new Vector3(x + 2, y, z) + transform.position);
            if(chunk != null) StartCoroutine(chunk.CreateMesh());
        }

        if(z == 0){
            Chunk chunk = FindChunk(new Vector3(x, y, z - 2) + transform.position);
            if(chunk != null) StartCoroutine(chunk.CreateMesh());
        }

        if(z == width - 1){
            Chunk chunk = FindChunk(new Vector3(x, y, z + 2) + transform.position);
            if(chunk != null) StartCoroutine(chunk.CreateMesh());
        }

        return true;
    }
}
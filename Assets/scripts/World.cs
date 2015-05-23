using UnityEngine;
using System.Collections;

public class World:MonoBehaviour {
    
    public Texture2D defaultBrickTexture;

    public Biome[] biomes;

    public static World currentWorld;

    public int chunkWidth = 20;
    public int chunkHeight = 20;

    public float viewRange = 30;

    public int seed = 0;

    public float brickHeight = 1;

    public Chunk chunkFab;

    void Awake(){
        currentWorld = this;
        if(seed == 0){
            seed = Random.Range(0, int.MaxValue);
        }
    }

    void Update(){
        int a = 0;

        float x = 0;
        float z = 0;

        for(a = 0; a < Chunk.chunks.Count; a++){
            Vector3 pos = Chunk.chunks[a].transform.position;
            Vector3 delta = pos - transform.position;

            delta.y = 0;

            if(delta.magnitude < viewRange + chunkWidth * 3) continue;

            Destroy(Chunk.chunks[a].gameObject);
        }

        for(x = transform.position.x - viewRange; x < transform.position.x + viewRange; x+= chunkWidth){
            for(z = transform.position.z - viewRange; z < transform.position.z + viewRange; z+= chunkWidth){
                Vector3 pos = new Vector3(x, 0, z);
                pos.x = Mathf.Floor(pos.x / (float)chunkWidth) * chunkWidth;
                pos.z = Mathf.Floor(pos.z / (float)chunkWidth) * chunkWidth;

               
                Vector3 delta = pos - transform.position;
                delta.y = 0;

                if(delta.magnitude > viewRange) continue;

                Chunk chunk = Chunk.FindChunk(pos);

                if(chunk == null){
                    chunk = (Chunk)Instantiate(chunkFab, pos, Quaternion.identity); 
                }
            }
        }
    }

    public static Biome GetIdealBiome(float wetness, float rockiness){
        int a = 0;
        float bestBid = 0;
        Biome biome = currentWorld.biomes[0];

        for(a = 0; a < currentWorld.biomes.Length; a++){
            float bid = currentWorld.biomes[a].Bid(wetness, rockiness);
            if(bid > bestBid){
                bestBid = bid;
                biome = currentWorld.biomes[a];
            }
        }

        return biome;
    }
}
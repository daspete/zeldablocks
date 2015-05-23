using UnityEngine;
using System.Collections;

[System.Serializable]
public class Biome {
    public string name = "Unknown";
    [Multiline]
    public string description = "description";

    public float idealWetness = 0;
    public float idealRockiness = 0;

    public BrickLayer[] brickLayers;

    public byte GetBrick(int y, float mountainValue, float blobValue, float wetness, float rockiness){
        BrickLayer bestBidder = null;
        float bestBid = 0;

        for(int a = 0; a < brickLayers.Length; a++){
            float bid = brickLayers[a].Bid(y, mountainValue, blobValue, wetness, rockiness);
            if(bid > bestBid){
                bestBid = bid;
                bestBidder = brickLayers[a];
            }
        }

        if(bestBidder == null){
            return 0;
        }else{
            return (byte)bestBidder.brick;
        }
    }

    public float Bid(float wetness, float rockiness){
        float delta = Mathf.Abs(wetness - idealWetness) + Mathf.Abs(rockiness - idealRockiness);
        return 100 / (delta + 1);
    }
}
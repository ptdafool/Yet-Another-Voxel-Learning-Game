using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScreen : MonoBehaviour
{
    World world;
    TextMeshProUGUI text;
    Player player;

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();


    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start()
    {
        
        world = GameObject.Find("World").GetComponent<World>();

        text = GetComponent<TextMeshProUGUI>();
        player = GetComponent<Player>();
        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    void Update()
    {
        if (world.debugScreen.activeSelf)
        {
            string debugText = "Minecraft-Like Debug Screen";
            debugText += "\n";
            debugText += frameRate + " fps";
            debugText += "\nCurrent World position, X: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + ", Y: " + Mathf.FloorToInt(world.player.transform.position.y) + ", Z: " + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels);
            debugText += "\n";
            debugText += "Chunk, X: " + (world.playerChunkCoord.x - halfWorldSizeInChunks) + ", Z: " + (world.playerChunkCoord.z - halfWorldSizeInChunks);
            // want some performance indexes showing the size of lists and queues of interest as FPS is SLOW.
            debugText += "\n\nChunk List Length: " + activeChunks.Count;
            text.text = debugText;
        }
        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
            timer += Time.deltaTime;
    }
}

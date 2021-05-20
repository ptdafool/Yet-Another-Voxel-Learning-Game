using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading;
using System.IO;

public class World : MonoBehaviour
{
    public Settings settings;

    [Header("World Generation Values")]
    public BiomeAttribute[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;
    
    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;

    public BlockType[] blocktypes;


    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerLastChunkCoord;
    public ChunkCoord playerChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    public GameObject debugScreen;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public Clouds clouds;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();



    private void Start()
    {

        Debug.Log("Generating new world using seed " + VoxelData.seed);
        Random.InitState(VoxelData.seed);
        //min/max global light levels derived from shader, passed back to shader depending on values set.
        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        if(settings.enableThreading) {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        SetGlobalLightValue();
        spawnPosition = new Vector3(VoxelData.WorldCentre, VoxelData.ChunkHeight - 75f, VoxelData.WorldCentre);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }
    public void SetGlobalLightValue() {

        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    private void Update() {


        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        // Only update the chunks if the player has moved from the chink they were previously on.
        if(!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if(chunksToCreate.Count > 0)
            CreateChunk();

        if(chunksToDraw.Count > 0) {
            if(chunksToDraw.Peek().isEditable)
                chunksToDraw.Dequeue().CreateMesh();
        }

        if(!settings.enableThreading) {

            if(!applyingModifications)
                ApplyModifications();

            if(chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);


    }

    void GenerateWorld() {

        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++) {
            for(int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++) {

                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
                chunksToCreate.Add(newChunk);
            }
        }
        player.position = spawnPosition;
        CheckViewDistance();
    }


    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks () {

        bool updated = false;
        int index = 0;

        lock (ChunkUpdateThreadLock) {

            while(!updated && index < chunksToUpdate.Count - 1) {

                if(chunksToUpdate[index].isEditable) {
                    chunksToUpdate[index].UpdateChunk();
                    if(!activeChunks.Contains(chunksToUpdate[index].coord))
                        activeChunks.Add(chunksToUpdate[index].coord);
                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                } else
                    index++;
            }
        }
    }


    void ThreadedUpdate() {

        while(true) {

            if(!applyingModifications)
                ApplyModifications();

            if(chunksToUpdate.Count > 0)
                UpdateChunks();

        }
    }
    private void OnDisable() {

        if(settings.enableThreading) {
            ChunkUpdateThread.Abort();
        }
    }

    void ApplyModifications() {

        applyingModifications = true;

        while(modifications.Count > 0) {

            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0) {

                VoxelMod v = queue.Dequeue();

                ChunkCoord c = GetChunkCoordFromVector3(v.position);

                if (chunks[c.x, c.z] == null) {
                    chunks[c.x, c.z] = new Chunk(c, this);
                    chunksToCreate.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);



            }
        }
        applyingModifications = false;

    }
    // Simple function to return a chunk coordinate from a vector3 (for use with the player position)
    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3 (Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];
    }

    // this uses the chunk coordinate to set a 'viewable' distance for
    // chunks 'ViewDistanceInChunks' away from the player in any horizontal direction
    void CheckViewDistance() {

        clouds.UpdateClouds();

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        // Loop through all chunks currently within view distance of the player.
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++) {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++) {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord)) {

                    // Check if it active, if not, activate it.
                    if (chunks[x, z] == null) {
                        chunks[x, z] = new Chunk(thisChunkCoord, this);
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else if (!chunks[x, z].IsActive) {
                        chunks[x, z].IsActive = true;
                    }
                    activeChunks.Add(thisChunkCoord);
                }

                // Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
                for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }
        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them.
        foreach (ChunkCoord c in previouslyActiveChunks) {
            chunks[c.x, c.z].IsActive = false;
        }
    }

    // function checks if voxels are in the world, if they are, returns the solid state (true/false).  Done by checking against height of terrain
    // and if within defined area, will flag as solid==true
    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);
        if(!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos).id].isSolid;
        
        return blocktypes[GetVoxel(pos)].isSolid;
    }

    public VoxelState GetVoxelState(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);
        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return null;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos);

        return new VoxelState(GetVoxel(pos));
    }

    public bool inUI
    {
        get { return _inUI;}
        set { _inUI = value;
            if (inUI) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }


    // this 'getvoxel' function is where the 'magic' happens.  It's where the different block types are generated in world.  Not really 'getting' anything, 
    // but rather it gets a voxel of a specific type to place at a specific location.  Perlin noise is - for example - used in generating entire 'areas' in the
    // structure.cs script.
    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);
        // IMMUTABLE TERRAIN PASS

        if(!IsVoxelInWorld(pos))
            return 0; //Air

        if(yPos == 0)
            return 1; //Bedrock

        // DO NOT MODIFY THE ABOVE

        // BIOME SELECTION PASS
        
        float sumOfHeights = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;


        for(int i = 0; i < biomes.Length; i++) {

            float weight =  Noise.Get2DPerlin(new Vector2(pos.x, pos.z),biomes[i].offset, biomes[i].scale);

            //keep track of which weight is strongest
            if(weight > strongestWeight) {

                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            //get height of terrain (for current biome) and multiply by its weight
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;
            
            // if height is greater than 0, add to sum of heights
            if(height > 0) {
                sumOfHeights += height;
                count++;
            }
        }
        // set biome to the one with the strongest weight.
        BiomeAttribute biome = biomes[strongestBiomeIndex];
        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + settings.solidGroundHeight);


        // Basic Terrain Pass (global for all terrains)
        byte voxelValue;

        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlock;

        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue =  biome.subSurfaceBlock;

        else if (yPos > terrainHeight)
            return 0;

        else
            voxelValue = 4;

        //Second Pass (where Lodes are set - basic logic is replace certain sections of stone with the lode of choice to create 'veins' of a particular ore)
        if (voxelValue == 4) // if value is stone, do the below
        {
            foreach(Lode lode in biome.lodes) {

                if(yPos > lode.minHeight && yPos < lode.maxHeight) {

                    if(Noise.Get3DPerlin(pos,lode.noiseOffset, lode.scale, lode.threshold)) {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }

        // TREE PASS (Trees which are part of our structure.cs for generating 'structures' on top of the terrain).

        if(yPos == terrainHeight && biome.placeMajorFlora)
        {
            if(Noise.Get2DPerlin(new Vector3(pos.x, pos.z), 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {
                if(Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight));

            }
        }

        return voxelValue;
    }



    // true/false returns true if the chunk is 'in world' (i.e. within the bounds
    // of our defined chunks), false if not
    bool IsChunkInWorld(ChunkCoord coord)
    {

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;

    }

    // as with the chunk in world, this determines whether a chunk is inside the
    // bounds of our world as defined by world size parameters

    bool IsVoxelInWorld(Vector3 pos)
    {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }

}


// this sets up the block type array used in some of the above functions.  Order is set according to the order of
// triangles laid out in Voxeldata.cs.  Order is important to be maintained throughout the code, and is noted as a reference
[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public Sprite icon;
    public bool renderNeighbourFaces;
    public float transparency;

    [Header("Gameplay Settings")]
    public int maxStackSize;
    

    // back, front, top, bottom, left, right (order of block faces)
    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    
    //  this array is used in the inspector to select which ID from our texture atlas (counted from top left to
    //  bottom right starting at 0) is shown on a particular face.
    //  Front face will be front from the perspective of the 'chunk' game object.
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID: invalid face index");
                return 0;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

        public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

        public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}

[System.Serializable]
public class Settings {

    [Header("Game Data")]
    public string version = "0.0.0.1";

    [Header("Performance")]
    public int viewDistance = 6;
    public bool enableThreading = true;

    [Header("Controls")]
    [Range(1f,5.0f)]
    public float mouseSensitivity = 2f;

    [Header("World Generation")]
    public int solidGroundHeight = 42;

};
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Chunk
{
    public ChunkCoord coord;

    //mesh renderer and filter to hold our mesh or sprite atlas
    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth]; //bool array for true/false is block next to another?

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    World world;

    private bool _IsActive;
    private bool isVoxelMapPopulated = false;
    //private bool threadLocked = false;
    

    public Chunk(ChunkCoord _coord, World _world)
    {
        coord = _coord;
        world = _world;
    }
    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;
       // lock(world.ChunkUpdateThreadLock) {
            PopulateVoxelMap();
        //}
    }

    // Populates Voxel map - a map of voxels for chunks updating true to the 'isVoxelMapPopulated' bool - and also calls updatechunk, which - as the
    // name suggests, updates the chunk referenced (indirectly - the direct function takes no parameters).
    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
                }
            }
        }

        isVoxelMapPopulated = true;
        world.chunksToUpdate.Add(this);

    }



    //Create the mesh data.  This will establish data from which the mesh can be drawn.
    public void UpdateChunk()
    {

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;
        }

        ClearMeshData();
        CalculateLight();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if(world.blocktypes[voxelMap[x, y, z].id].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }
        lock (world.chunksToDraw)
            world.chunksToDraw.Enqueue(this);
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    // as the name suggests - calculates light values
    void CalculateLight() {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkWidth; z++) {

                float lightRay = 1f;

                for(int y = VoxelData.ChunkHeight - 1; y >= 0; y--) {

                    VoxelState thisVoxel = voxelMap[x, y, z];

                    if(thisVoxel.id > 0 && world.blocktypes[thisVoxel.id].transparency < lightRay) 
                        lightRay = world.blocktypes[thisVoxel.id].transparency;
                    
                    thisVoxel.globalLightPercent = lightRay;

                    voxelMap[x, y, z] = thisVoxel;

                    if(lightRay > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));

                }
            }
        }
        while (litVoxels.Count > 0) {

            Vector3Int v = litVoxels.Dequeue();

            for(int p = 0; p < 6; p++) {

                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbour = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if(IsVoxelInChunk(neighbour.x, neighbour.y, neighbour.z)) {

                    if(voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if(voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent > VoxelData.lightFalloff)
                            litVoxels.Enqueue(neighbour);
                    }
                }
            }
        }
    }

    // 'IsActive' is used mainly in chunk active tests (true/false) but can be used elsewhere to check if something is active (true/false returned)
    public bool IsActive
    {
            get { return _IsActive; }
            set {
                _IsActive = value;
                if (chunkObject != null)
                    chunkObject.SetActive(value);
                 }
    }

    // 'iseditable' is used mainly to check if a voxel is editable (true/false returned) but can be used elsewhere to check for editable conditions (true/false)
    public bool isEditable
    {
        get
        {
            if (!isVoxelMapPopulated)
                return false;
            else
                return true;
        }
    }


    // This function checks if a voxel is within our boundaries (ChunkWidth/ChunkHeight) and returns a bool
    // Sanity check to prevent index out of range errors when called.
    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

    public void EditVoxel (Vector3 pos, byte newID)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);
        
        voxelMap[xCheck, yCheck, zCheck].id = newID;

        lock(world.ChunkUpdateThreadLock) {

            world.chunksToUpdate.Insert(0,this);
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        }
    }

    public void UpdateSurroundingVoxels (int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if(!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));
            }
        }
    }

    // If a voxel is solid, we want to return the voxel type as determined by our Block array.  Otherwise, we still need coordinates
    // for 'air'.  This still needs mapping.  If we don't return a value, for loops will fill with other voxel types leading to
    // voxels appearing in the wrong place.
    VoxelState CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!IsVoxelInChunk(x, y, z)) {
            return world.GetVoxelState(pos + position);

        }
        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3 (Vector3 pos) {

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return voxelMap[xCheck, yCheck, zCheck];
    }

    //public int textureSelect; //move to somewhere else
    // Adds Voxel Data to the chunk to be drawn.  This is called from the Create Mesh Data function.
    void UpdateMeshData(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockID = voxelMap[x, y, z].id;

        // bool isTransparent = world.blocktypes[blockID].renderNeighbourFaces;

        for (int p = 0; p < 6; p++) {

            VoxelState neighbour = CheckVoxel(pos + VoxelData.faceChecks[p]);

            if (neighbour != null && world.blocktypes[neighbour.id].renderNeighbourFaces) {

                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for(int i = 0; i < 4; i++)
                    normals.Add(VoxelData.faceChecks[p]);


                AddTexture(world.blocktypes[blockID].GetTextureID(p));

                float lightlevel = neighbour.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightlevel));
                colors.Add(new Color(0, 0, 0, lightlevel));
                colors.Add(new Color(0, 0, 0, lightlevel));
                colors.Add(new Color(0, 0, 0, lightlevel));

                if (!world.blocktypes[neighbour.id].renderNeighbourFaces) {

                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                
                }
                vertexIndex += 4;
            }
        }
    }

    //This will create mesh
    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        //mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;
    }

    //NB: face index is the index of texture in atlas, NOT the block type index - don't confuse the two.
    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y+ VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;
    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }
    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }

    // chunk coord comparison function.  If same, return true, otherwise if null or not same, returns false.
    public bool Equals (ChunkCoord other)
    {
        if (other == null)
            return false;
        else if (other.x == x && other.z == z)
            return true;
        else
            return false;
    }
}

public class VoxelState
{
    public byte id;
    public float globalLightPercent;

    public VoxelState()
    {
        id = 0;
        globalLightPercent = 0f;
    }

    public VoxelState (byte _id)
    {
        id = _id;
        globalLightPercent = 0f;
    }
}
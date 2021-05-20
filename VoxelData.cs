using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData {
    // This class is going to store the voxel data while the game is running.
    // to get this, we need to start from vertices, build up to triangles, then quads (2 triangles essentially)
    // then from quads, we can form the cube

    public static readonly int ChunkWidth = 16;
    public static readonly int ChunkHeight = 256;
    public static readonly int WorldSizeInChunks = 100;

    //lighting values
    public static float minLightLevel = 0.15f; //prev = 0.25f
    public static float maxLightLevel = 0.8f;
    public static float lightFalloff = 0.08f;

    public static int seed; // NOTE: Not Read-Only as this is a setting our player can set from the settings menu.

    public static int WorldCentre {

        get { return (WorldSizeInChunks * ChunkWidth) / 2; }

    }

    //lighting ranges allow for 'mood' settings

    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * ChunkWidth; }
    }

    public static readonly int TextureAtlasSizeInBlocks = 24; //size of the texture atlas file
    public static float NormalizedBlockTextureSize
    {
        get { return 1f / (TextureAtlasSizeInBlocks); }
    }

    public static readonly Vector3[] voxelVerts = new Vector3[8]
    {
        // setting up vertices of a cube.  The order this is done matters.
        // probably best not to mess with the order here, as that will need to be taken into account with order
        // being changed in several other places within the code.
        // back, front, top, bottom, left, right (order of block faces)
        new Vector3(0.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f),
        new Vector3(1.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(1.0f,0.0f,1.0f),
        new Vector3(1.0f,1.0f,1.0f),
        new Vector3(0.0f,1.0f,1.0f),
    };
    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        // This sets up the offsets so that world.cs can run its checks to determine whether or not to show a 
        // quad face or not.  Probably best not to mess with the order, or values here.
        // back, front, top, bottom, left, right (order of block faces)
        new Vector3(0.0f,0.0f,-1.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,-1.0f,0.0f),
        new Vector3(-1.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f)
    };

    public static readonly int[,] voxelTris = new int[6, 4] {
        // The order back, front, top, bottom, left, right (order of block faces)
        // is the triangle index for our UVs
        {0, 3, 1, 2},//{0, 3, 1, 2},
        {5, 6, 4, 7},
        {3, 7, 2, 6},
        {1, 5, 0, 4},
        {4, 7, 0, 3},
        {1, 2, 5, 6}
    };


    public static readonly Vector2[] voxelUvs = new Vector2[4]
    {
        //maps each point in the 6 digit face (remember, even though it's a 4-point UV,
        //we call point 1 and 3 twice in this case for the second triangle)
        //NOTE: ORDER MATTERS!!

        // given that we're calculating the UVs using Maths, I'm not sure if this method is strictly needed anymore.

        // DIRT BLOC
        new Vector2(0.125f,0.9375f),
        new Vector2(0.125f,1.0f),
        new Vector2(0.1875f,0.9375f),
        new Vector2(0.1875f,1.0f)
    };
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Minecraft-Like/Biome Attribute")]
public class BiomeAttribute : ScriptableObject
{
    [Header("Biome Name")]
    public string BiomeName;
    public int offset;
    public float scale;
    public int terrainHeight; // from solid ground to highest terrain point.
    public float terrainScale; // for the noise function.  Scale of terrain undulation.
    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Flora")]
    public int majorFloraIndex;
    public float majorFloraZoneScale = 1.3f;
    
    [Range(0.1f, 1f)]
    public float majorFloraZoneThreshold = 0.6f;
    public float majorFloraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float majorFloraPlacementThreshold = 0.8f;
    public bool placeMajorFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

}

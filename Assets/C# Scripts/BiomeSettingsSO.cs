using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Biome", menuName = "WorldSettings/Biome")]
public class BiomeSettingsSO : ScriptableObject
{
    public int maxChunkHeight;
    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;
}

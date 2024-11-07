using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Biome", menuName = "WorldSettings/Biome")]
public class BiomeSettingsSO : ScriptableObject
{
    public byte maxChunkHeight;
    public float scale;
    public byte octaves;
    public float persistence;
    public float lacunarity;
}

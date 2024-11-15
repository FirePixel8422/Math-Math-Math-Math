using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GenerationType : byte
{
    solid,
    sub
}
[CreateAssetMenu(fileName = "New Biome", menuName = "WorldSettings/Biome")]
public class BiomeSettingsSO : ScriptableObject
{
    public byte maxChunkHeight;
    public int subChunkHeight;
    public float scale;
    public byte octaves;
    public float persistence;
    public float lacunarity;

    public GenerationType typeOfChunkToGenerate;
}

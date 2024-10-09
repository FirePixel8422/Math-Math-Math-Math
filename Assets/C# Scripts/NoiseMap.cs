using Unity.Burst;
using UnityEngine;

[BurstCompile]
public static class NoiseMap
{
    public static float[,] heightMap;
    private static int chunkSize;
    private static float scale;
    private static int octaves;
    private static float persistence;
    private static float lacunarity;
    private static int maxChunkHeight;
    private static int seed;
    private static Vector2 offset;



    // Generates the height map based on the given parameters
    public static float[,] GenerateNoiseMap(int resolution, int seed, int chunkSize, float scale, int octaves, float lacunarity, float persistence, Vector2 offset)
    {
        NoiseMap.chunkSize = chunkSize;
        NoiseMap.scale = scale;
        NoiseMap.octaves = octaves;
        NoiseMap.persistence = persistence;
        NoiseMap.lacunarity = lacunarity;
        NoiseMap.seed = seed;
        NoiseMap.offset = offset;
        float[,] noiseMap = new float[resolution, resolution];

        // Generate Perlin noise values
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                // Calculate Perlin noise height for the current (x, z) position
                noiseMap[x, z] = CalculatePerlinNoiseHeight(x, z, resolution, chunkSize, scale, seed, maxChunkHeight);
            }
        }

        return noiseMap;
    }
    // Calculates the height for a specific (x, z) position using Perlin noise
    private static float CalculatePerlinNoiseHeight(int x, int z, int resolution, int chunkSize, float scale, int seed, int
maxChunkHeight)
    {
        float noiseHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;

        // Offset to ensure world coordinates are continuous across chunks
        float worldX = x + offset.x;
        float worldZ = z + offset.y;

        for (int i = 0; i < octaves; i++)
        {
            // Scale coordinates to ensure they are continuous across chunks
            float xCoord = (worldX / (float)resolution) * scale * frequency + seed;
            float zCoord = (worldZ / (float)resolution) * scale * frequency + seed;

            float sample = Mathf.PerlinNoise(xCoord, zCoord);

            noiseHeight += sample * amplitude;
            maxPossibleHeight += amplitude;

            amplitude = persistence; // Adjust amplitude based on persistence
            frequency = lacunarity;   // Adjust frequency based on lacunarity
        }
        noiseHeight = noiseHeight / maxPossibleHeight;
        return (noiseHeight);
    }
    // Get height value at specific coordinates
    public static float GetHeight(int x, int z)
    {
        if (x < 0 || x >= chunkSize || z < 0 || z >= chunkSize)
        {
            return 0; // Out of bounds, return 0
        }
        return heightMap[x, z];
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NoiseMap
{

    // Generates the height map based on the given parameters
    public static NativeArray<float> GenerateNoiseMap(int resolution, int seed, float scale, int octaves, float lacunarity, float persistence, int2 offset)
    {
        NativeArray<float> noiseMap = new NativeArray<float>(resolution * resolution, Allocator.TempJob);

        // Generate Perlin noise values
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                // Calculate Perlin noise height for the current (x, z) position
                noiseMap[x * resolution + z] = CalculatePerlinNoiseHeight(x + offset.x, z + offset.y, resolution, seed, scale, octaves, lacunarity, persistence);
            }
        }

        return noiseMap;
    }




    // Calculates the height for a specific (x, z) position using Perlin noise
    [BurstCompile]
    private static float CalculatePerlinNoiseHeight(float worldPosX, float worldPosZ, int resolution, int seed, float scale, int octaves, float lacunarity, float persistence)
    {
        float noiseHeight = 0;
        float frequency = 1;
        float maxPossibleHeight = 0;


        // Scale coordinates to ensure they are continuous across chunks
        float baseXCoord = worldPosX / resolution * scale;
        float baseZCoord = worldPosZ / resolution * scale;


        for (int i = 0; i < octaves; i++)
        {
            float xCoord = baseXCoord * frequency + seed;
            float zCoord = baseZCoord * frequency + seed;

            float sample = Mathf.PerlinNoise(xCoord, zCoord);

            noiseHeight += sample * persistence;
            maxPossibleHeight += persistence;


            frequency = lacunarity;   // Adjust frequency based on lacunarity
        }
        noiseHeight /= maxPossibleHeight;
        return noiseHeight;
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NoiseMapJob
{

    public static NativeArray<float> GenerateNoiseMap(int resolution, int seed, float scale, int octaves, float lacunarity, float persistence, int2 offset)
    {
        NativeArray<float> noiseMap = new NativeArray<float>(resolution * resolution, Allocator.TempJob);

        var job = new GenerateNoiseJob
        {
            resolution = resolution,
            seed = seed,
            scale = scale,
            octaves = octaves,
            lacunarity = lacunarity,
            persistence = persistence,
            offset = offset,
            noiseMap = noiseMap
        };

        // Schedule the job
        JobHandle jobHandle = job.Schedule(resolution * resolution, 2048);
        jobHandle.Complete();

        return noiseMap;
    }





    [BurstCompile]
    public struct GenerateNoiseJob : IJobParallelFor
    {
        [NoAlias][ReadOnly] public int resolution;
        [NoAlias][ReadOnly] public int seed;
        [NoAlias][ReadOnly] public float scale;
        [NoAlias][ReadOnly] public int octaves;
        [NoAlias][ReadOnly] public float lacunarity;
        [NoAlias][ReadOnly] public float persistence;
        [NoAlias][ReadOnly] public int2 offset;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float> noiseMap;


        [BurstCompile]
        public void Execute(int index)
        {
            int x = index / resolution;
            int z = index % resolution;

            // Calculate Perlin noise height for the current (x, z) position
            noiseMap[x * resolution + z] = CalculatePerlinNoiseHeight(x + offset.x, z + offset.y, resolution, seed, scale, octaves, lacunarity, persistence);
        }

        [BurstCompile]
        private float CalculatePerlinNoiseHeight(float worldPosX, float worldPosZ, int resolution, int seed, float scale, int octaves, float lacunarity, float persistence)
        {
            float noiseHeight = 0;
            float frequency = 1;
            float maxPossibleHeight = 0;


            float baseXCoord = worldPosX / resolution * scale;
            float baseZCoord = worldPosZ / resolution * scale;

            for (int i = 0; i < octaves; i++)
            {
                float xCoord = baseXCoord * frequency + seed;
                float zCoord = baseZCoord * frequency + seed;

                float sample = PerlinNoise.Perlin(xCoord, zCoord);

                noiseHeight += sample * persistence;
                maxPossibleHeight += persistence;

                frequency *= lacunarity;
            }
            noiseHeight /= maxPossibleHeight;
            return noiseHeight;
        }

        //// Custom 2D Perlin noise function
        //private static float PerlinNoise(float x, float y)
        //{
        //    // Find unit square that contains point
        //    int x0 = (int)math.floor(x);
        //    int x1 = x0 + 1;
        //    int y0 = (int)math.floor(y);
        //    int y1 = y0 + 1;

        //    // Find relative x and y in the square
        //    float sx = x - x0;
        //    float sy = y - y0;

        //    // Interpolate the contributions from the corners
        //    float n0 = SmoothNoise(x0, y0);
        //    float n1 = SmoothNoise(x1, y0);
        //    float ix0 = math.lerp(n0, n1, sx);

        //    n0 = SmoothNoise(x0, y1);
        //    n1 = SmoothNoise(x1, y1);
        //    float ix1 = math.lerp(n0, n1, sx);

        //    return math.lerp(ix0, ix1, sy);
        //}

        //private static float SmoothNoise(int x, int y)
        //{
        //    // Generate pseudo-random gradient
        //    int hash = x * 49632 + y * 325176 + 12345; // Simple hash function based on coordinates
        //    hash = (hash << 13) ^ hash;
        //    float random = (float)(1.0 - ((hash * (hash * hash * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);

        //    // Generate a smooth value based on the random gradient
        //    return random;
        //}
    }
}

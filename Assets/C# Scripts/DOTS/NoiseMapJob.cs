using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NoiseMapJob
{

    public static NativeArray<byte> GenerateNoiseMap(int resolution, byte maxHeight, int seed, float scale, byte octaves, float lacunarity, float persistence, int2 offset)
    {
        NativeArray<byte> noiseMap = new NativeArray<byte>(resolution * resolution, Allocator.TempJob);

        var job = new GenerateNoiseJob
        {
            resolution = resolution,
            seed = seed,
            scale = scale,
            maxHeight = maxHeight,
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
        [NoAlias][ReadOnly] public byte maxHeight;
        [NoAlias][ReadOnly] public byte octaves;
        [NoAlias][ReadOnly] public float lacunarity;
        [NoAlias][ReadOnly] public float persistence;
        [NoAlias][ReadOnly] public int2 offset;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<byte> noiseMap;


        [BurstCompile]
        public void Execute(int index)
        {
            int x = index / resolution;
            int z = index % resolution;

            // Calculate Perlin noise height for the current (x, z) position
            noiseMap[x * resolution + z] = CalculatePerlinNoiseHeight(x + offset.x, z + offset.y, resolution, seed, scale, maxHeight, octaves, lacunarity, persistence);
        }

        [BurstCompile]
        private byte CalculatePerlinNoiseHeight(float worldPosX, float worldPosZ, int resolution, int seed, float scale, byte maxHeight, byte octaves, float lacunarity, float persistence)
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
            return ClampUnderMax((byte)(noiseHeight * maxHeight), maxHeight);
        }


        [BurstCompile]
        private byte ClampUnderMax(byte value, byte max)
        {
            //clamp and data limit
            if (value > max || value > byte.MaxValue)
            {
                value = max;
            }

            return value;
        }
    }
}

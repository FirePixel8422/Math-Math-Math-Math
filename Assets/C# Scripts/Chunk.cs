
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.VisualScripting;
using Unity.Collections;
using Unity.Mathematics;



[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[BurstCompile]
public class Chunk : MonoBehaviour
{
    public float cubeSize;

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public bool autoUpdate;

    public int atlasSize;

    public int chunkSize, maxChunkHeight;
    public int seed;
    public float[,] noiseMap;

    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;

    public Vector2Int chunkGridPos;


    private int adjustedX;
    private int adjustedZ;
    private int perlinValue;
    private int maxY;


    private void Start()
    {
        noiseMap = NoiseMap.GenerateNoiseMap(chunkSize, chunkSize, seed, scale, octaves, persistence, lacunarity, new(transform.position.x, transform.position.z));

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        stopwatch = new Stopwatch();
        var chunkpos = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        GenerateBlockPos(chunkpos);
    }


    [BurstCompile]
    private void GenerateBlockPos(Vector3Int chunkPosition)
    {
        stopwatch.Start();

        NativeList<float3> blockpositions = new NativeList<float3>(chunkSize * chunkSize * maxChunkHeight, Allocator.TempJob);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Get height from the noise map (assuming noiseMap is already normalized between 0 and 1)
                int perlinValue = Mathf.FloorToInt(noiseMap[x, z] * maxChunkHeight);
                int maxY = Mathf.Clamp(perlinValue, 0, maxChunkHeight);

                // Add block positions up to the max height
                for (int y = 0; y < maxY; y++)
                {
                    blockpositions.Add(new float3(x, y, z));
                }
            }
        }

        MeshCalculatorJob.CallGenerateMeshJob(blockpositions, cubeSize, atlasSize, meshFilter.mesh);

        blockpositions.Dispose();

        stopwatch.Stop();
    }

    private int CalculatePerlinNoiseHeight(int x, int z, Vector2Int resolution, float scale, int seed)
    {
        float noiseHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = (x + seed) / (float)resolution.x * scale * frequency;
            float zCoord = (z + seed) / (float)resolution.y * scale * frequency;

            float sample = Mathf.PerlinNoise(xCoord, zCoord);

            noiseHeight += sample * amplitude;
            maxPossibleHeight += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        noiseHeight = noiseHeight / maxPossibleHeight;
        return Mathf.FloorToInt(noiseHeight * maxChunkHeight);
    }






    private Stopwatch stopwatch;






    public Vector3Int chunkPosition; // Position of the chunk in the chunk grid (chunk coordinates)

    public List<Vector3Int> GetConnectedEdge(Chunk neighbor)
    {
        List<Vector3Int> edgePositions = new List<Vector3Int>();

        // Determine which side is connected to the current chunk based on relative position
        if (neighbor.chunkPosition.x < this.chunkPosition.x)
        {
            // Neighbor is on the left (West), return right edge
            edgePositions = GetEdgePositionsRight();
        }
        else if (neighbor.chunkPosition.x > this.chunkPosition.x)
        {
            // Neighbor is on the right (East), return left edge
            edgePositions = GetEdgePositionsLeft();
        }
        else if (neighbor.chunkPosition.z > this.chunkPosition.z)
        {
            // Neighbor is in front (North), return back edge
            edgePositions = GetEdgePositionsBack();
        }
        else if (neighbor.chunkPosition.z < this.chunkPosition.z)
        {
            // Neighbor is behind (South), return front edge
            edgePositions = GetEdgePositionsFront();
        }

        return edgePositions;
    }



    // Get the right edge (x = 15) of this chunk
    private List<Vector3Int> GetEdgePositionsRight()
    {
        List<Vector3Int> edgePositions = new List<Vector3Int>();
        for (int y = 0; y < chunkSize; y++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                edgePositions.Add(new Vector3Int(chunkSize - 1, y, z));
            }
        }
        return edgePositions;
    }

    // Get the left edge (x = 0) of this chunk
    private List<Vector3Int> GetEdgePositionsLeft()
    {
        List<Vector3Int> edgePositions = new List<Vector3Int>();
        for (int y = 0; y < chunkSize; y++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                edgePositions.Add(new Vector3Int(0, y, z));
            }
        }
        return edgePositions;
    }

    // Get the back edge (z = 15) of this chunk
    private List<Vector3Int> GetEdgePositionsBack()
    {
        List<Vector3Int> edgePositions = new List<Vector3Int>();
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                edgePositions.Add(new Vector3Int(x, y, chunkSize - 1));
            }
        }
        return edgePositions;
    }

    // Get the front edge (z = 0) of this chunk
    private List<Vector3Int> GetEdgePositionsFront()
    {
        List<Vector3Int> edgePositions = new List<Vector3Int>();
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                edgePositions.Add(new Vector3Int(x, y, 0));
            }
        }
        return edgePositions;
    }






    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + new Vector3(chunkSize * 0.5f - 0.5f, chunkSize * 0.5f - 0.5f, chunkSize * 0.5f - 0.5f), Vector3.one * chunkSize);
    }
}
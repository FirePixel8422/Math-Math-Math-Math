
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
    public int cubeSize;

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public int atlasSize;

    public int chunkSize, maxChunkHeight;
    public int seed;
    public float[,] noiseMap;

    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;



    private void Start()
    {
        noiseMap = NoiseMap.GenerateNoiseMap(chunkSize, chunkSize, seed, scale, octaves, persistence, lacunarity, new(transform.position.x, transform.position.z));
    }


    public Vector3[] debugVerts;
    public int[] debugTris;




    public bool drawMeshVerticesGizmos;
    public bool drawMeshEdgesGizmos;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + new Vector3(chunkSize * 0.5f - 0.5f, chunkSize * 0.5f - 0.5f, chunkSize * 0.5f - 0.5f), Vector3.one * chunkSize);


        Gizmos.color = Color.black;
        if (drawMeshVerticesGizmos)
        {
            foreach (Vector3 vertex in debugVerts)
            {
                Gizmos.DrawCube(vertex, Vector3.one * cubeSize * .1f);
            }
        }

        if (drawMeshEdgesGizmos)
        {
            for (int i = 0; i < debugTris.Length; i += 3)
            {
                Gizmos.DrawLine(debugVerts[debugTris[i]], debugVerts[debugTris[i + 1]]);
                Gizmos.DrawLine(debugVerts[debugTris[i + 1]], debugVerts[debugTris[i + 2]]);
                Gizmos.DrawLine(debugVerts[debugTris[i + 2]], debugVerts[debugTris[i]]);
            }
        }
    }



    [BurstCompile]
    public void GenerateBlockPos()
    {
        NativeList<int3> blockPositions = new NativeList<int3>(chunkSize * chunkSize * maxChunkHeight, Allocator.TempJob);

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
                    blockPositions.Add(new int3(x, y, z));
                }
            }
        }

        MeshCalculatorJob.CallGenerateMeshJob(blockPositions.AsArray(), cubeSize, atlasSize, meshFilter.mesh, GetComponent<MeshCollider>());

        blockPositions.Dispose();
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
}
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;



[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[BurstCompile]
public class Chunk : MonoBehaviour
{
    public ChunkData chunkData;

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


    private int3 gridPos;


    [BurstCompile]
    public void Init()
    {
        gridPos = new int3((int)transform.position.x, 0, (int)transform.position.z);

        noiseMap = NoiseMap.GenerateNoiseMap(chunkSize, chunkSize, seed, scale, octaves, persistence, lacunarity, new int2(gridPos.x, gridPos.z));

        GenerateBlockPos();
    }



    [BurstCompile]
    private void GenerateBlockPos()
    {
        int chunkSize_X_MaxHeight = chunkSize * maxChunkHeight;

        NativeList<int3> blockPositions = new NativeList<int3>(chunkSize * chunkSize_X_MaxHeight, Allocator.TempJob);

        NativeList<int3> blockPositionsList_Left = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Right = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Forward = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Back = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Get height from the noise map (assuming noiseMap is already normalized between 0 and 1)
                int perlinValue = (int)(noiseMap[x, z] * maxChunkHeight);
                int maxY = ClampPerlinValueUnderMax(perlinValue, maxChunkHeight);

                // Add block positions up to the max height
                for (int y = 0; y < maxY; y++)
                {
                    if (x == 0)
                    {
                        blockPositionsList_Left.Add(new int3(x, y, z));
                    }
                    else if (x == chunkSize - 1)
                    {
                        blockPositionsList_Right.Add(new int3(x, y, z));
                    }

                    if (z == 0)
                    {
                        blockPositionsList_Forward.Add(new int3(x, y, z));
                    }
                    else if (z == chunkSize - 1)
                    {
                        blockPositionsList_Back.Add(new int3(x, y, z));
                    }


                    blockPositions.Add(new int3(x, y, z));
                }
            }
        }

        int leftCount = blockPositionsList_Left.Length;
        int rightCount = blockPositionsList_Right.Length;
        int forwardCount = blockPositionsList_Forward.Length;
        int backCount = blockPositionsList_Back.Length;


        chunkData = new ChunkData()
        {
            gridPos = gridPos,

            Count = leftCount + rightCount + forwardCount + backCount,

            blockPositions_Left = new NativeArray<int3>(leftCount, Allocator.Persistent),
            blockPositions_Right = new NativeArray<int3>(rightCount, Allocator.Persistent),
            blockPositions_Forward = new NativeArray<int3>(forwardCount, Allocator.Persistent),
            blockPositions_Back = new NativeArray<int3>(backCount, Allocator.Persistent),
        };

        NativeArray<int3>.Copy(blockPositionsList_Left.AsArray(), chunkData.blockPositions_Left);
        NativeArray<int3>.Copy(blockPositionsList_Right.AsArray(), chunkData.blockPositions_Right);
        NativeArray<int3>.Copy(blockPositionsList_Forward.AsArray(), chunkData.blockPositions_Forward);
        NativeArray<int3>.Copy(blockPositionsList_Back.AsArray(), chunkData.blockPositions_Back);

        blockPositionsList_Left.Dispose();
        blockPositionsList_Right.Dispose();
        blockPositionsList_Forward.Dispose();
        blockPositionsList_Back.Dispose();



        MeshCalculatorJob.CallGenerateMeshJob(blockPositions.AsArray(), atlasSize, meshFilter.mesh, GetComponent<MeshCollider>());

        blockPositions.Dispose();
    }

    private int ClampPerlinValueUnderMax(int value, int max)
    {
        if (value > max)
        {
            value = max;
        }

        return value;
    }



#if UNITY_EDITOR

    private Vector3[] debugVerts;
    private int[] debugTris;
    private List<int3> blockDebug = new List<int3>();


    private void Update()
    {
        if (drawMeshVerticesGizmos)
        {
            debugVerts = meshFilter.mesh.vertices;
        }

        if (drawMeshEdgesGizmos)
        {
            debugTris = meshFilter.mesh.triangles;
        }
    }


    public bool drawMeshGizmos;
    public bool drawMeshVerticesGizmos;
    public bool drawMeshEdgesGizmos;
    public bool drawChunkGizmos;

    private void OnDrawGizmos()
    {
        if (drawMeshGizmos)
        {
            Gizmos.DrawWireCube(transform.position + new Vector3(chunkSize * 0.5f - 0.5f, maxChunkHeight / 2 - 0.5f, chunkSize * 0.5f - 0.5f), new Vector3(chunkSize, maxChunkHeight, chunkSize));
        }


        Gizmos.color = Color.black;
        if (drawMeshVerticesGizmos)
        {
            foreach (Vector3 vertex in debugVerts)
            {
                Gizmos.DrawCube(vertex + transform.position, .1f * Vector3.one);
            }
        }

        if (drawMeshEdgesGizmos)
        {
            for (int i = 0; i < debugTris.Length; i += 3)
            {
                Gizmos.DrawLine(debugVerts[debugTris[i]] + transform.position, debugVerts[debugTris[i + 1]] + transform.position);
                Gizmos.DrawLine(debugVerts[debugTris[i + 1]] + transform.position, debugVerts[debugTris[i + 2]] + transform.position);
                Gizmos.DrawLine(debugVerts[debugTris[i + 2]] + transform.position, debugVerts[debugTris[i]] + transform.position);
            }
        }
        if (drawChunkGizmos)
        {
            if (blockDebug.Count == 0)
            {
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
                            blockDebug.Add(new int3(x, y, z));
                        }
                    }
                }
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = 0; y < maxChunkHeight; y++)
                    {
                        Gizmos.color = Color.black;
                        if (blockDebug.Contains(new int3(x, y, z)))
                        {
                            Gizmos.color = Color.white;
                        }

                        Gizmos.DrawCube(transform.position + new Vector3(x, y, z), Vector3.one * .2f);
                    }
                }
            }
        }
    }
#endif


    private void Example()
    {
        int arrayCount = 20;
        int arraySize = 20;

        NativeArray<float> example2DArray = new NativeArray<float>(arrayCount * arraySize, Allocator.TempJob);


        for (int i = 0; i < arrayCount; i++)
        {
            for (int index = 0; index < arraySize; index++)
            {
                example2DArray[i * arrayCount + index] = 1;
            }
        }



        int indexX = 0;
        int indexY = 4;


        //pak waarde 0, 4 van de array
        float value = example2DArray[arrayCount * indexX + indexY];


        example2DArray.Dispose();
    }
}



[System.Serializable]
public struct ChunkData
{
    public int3 gridPos;

    public int Count;

    public NativeArray<int3> blockPositions_Left;
    public NativeArray<int3> blockPositions_Right;
    public NativeArray<int3> blockPositions_Forward;
    public NativeArray<int3> blockPositions_Back;
}
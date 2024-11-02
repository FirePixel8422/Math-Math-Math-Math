using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;
using Unity.Transforms;



[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[BurstCompile]
public class Chunk : MonoBehaviour
{
    public ChunkData chunkData;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;


    public ChunkState chunkState;


    public bool isRenderEdgeChunk;

    public void Init(bool _isRenderEdgeChunk)
    {
        ChunkManager.Instance.AddChunksToQue(this);

        isRenderEdgeChunk = _isRenderEdgeChunk;
    }



    [BurstCompile]
    public void LoadChunk(int chunkSize, int maxChunkHeight, int seed, float scale, int octaves, float persistence, float lacunarity)
    {
        chunkState = ChunkState.Loaded;

        int3 worldPos = new int3((int)transform.position.x, 0, (int)transform.position.z);
        int3 gridPos = worldPos / chunkSize;

        NativeArray<float> noiseMap = NoiseMapJob.GenerateNoiseMap(chunkSize, seed, scale, octaves, persistence, lacunarity, new int2(worldPos.x, worldPos.z));

        GenerateBlockPos(noiseMap, chunkSize, maxChunkHeight, gridPos);

        noiseMap.Dispose();
    }




    [BurstCompile]
    private void GenerateBlockPos(NativeArray<float> noiseMap, int chunkSize, int maxChunkHeight, int3 gridPos)
    {
        int chunkSize_X_MaxHeight = chunkSize * maxChunkHeight;

        NativeList<int3> blockPositionsList = new NativeList<int3>(chunkSize * chunkSize_X_MaxHeight, Allocator.Temp);

        NativeList<int3> blockPositionsList_Left = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Right = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Forward = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);
        NativeList<int3> blockPositionsList_Back = new NativeList<int3>(chunkSize_X_MaxHeight, Allocator.Temp);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Get height from the noise map (assuming noiseMap is already normalized between 0 and 1)
                int perlinValue = (int)(noiseMap[x * chunkSize + z] * maxChunkHeight);
                int maxY = ClampPerlinValueUnderMax(perlinValue, maxChunkHeight);

                // Add block positions up to the max height
                for (int y = 0; y < maxY; y++)
                {
                    if (x == 0)
                    {
                        blockPositionsList_Left.Add(new int3(x + chunkSize, y, z + chunkSize));
                    }
                    else if (x == chunkSize - 1)
                    {
                        blockPositionsList_Right.Add(new int3(x + chunkSize, y, z + chunkSize));
                    }

                    if (z == 0)
                    {
                        blockPositionsList_Forward.Add(new int3(x + chunkSize, y, z + chunkSize));
                    }
                    else if (z == chunkSize - 1)
                    {
                        blockPositionsList_Back.Add(new int3(x + chunkSize, y, z + chunkSize));
                    }


                    blockPositionsList.Add(new int3(x, y, z));
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

            blockPositions = new NativeArray<int3>(blockPositionsList.Length, Allocator.Persistent),

            blockPositions_Left = new NativeArray<int3>(leftCount, Allocator.Persistent),
            blockPositions_Right = new NativeArray<int3>(rightCount, Allocator.Persistent),
            blockPositions_Forward = new NativeArray<int3>(forwardCount, Allocator.Persistent),
            blockPositions_Back = new NativeArray<int3>(backCount, Allocator.Persistent),
        };

        NativeArray<int3>.Copy(blockPositionsList.AsArray(), chunkData.blockPositions);

        NativeArray<int3>.Copy(blockPositionsList_Left.AsArray(), chunkData.blockPositions_Left);
        NativeArray<int3>.Copy(blockPositionsList_Right.AsArray(), chunkData.blockPositions_Right);
        NativeArray<int3>.Copy(blockPositionsList_Forward.AsArray(), chunkData.blockPositions_Forward);
        NativeArray<int3>.Copy(blockPositionsList_Back.AsArray(), chunkData.blockPositions_Back);


        blockPositionsList.Dispose();

        blockPositionsList_Left.Dispose();
        blockPositionsList_Right.Dispose();
        blockPositionsList_Forward.Dispose();
        blockPositionsList_Back.Dispose();
    }


    [BurstCompile]
    public void RenderChunk(int atlasSize)
    {
        chunkState = ChunkState.Rendered;

        MeshCalculatorJob.CallGenerateMeshJob(chunkData.gridPos, chunkData.blockPositions, atlasSize, meshFilter.mesh, meshCollider);
    }



    [BurstCompile]
    private int ClampPerlinValueUnderMax(int value, int max)
    {
        if (value > max)
        {
            value = max;
        }

        return value;
    }




#if UNITY_EDITOR
    #region EditorOnly_Debug

    public Stopwatch sw;

    public Vector3[] debugVerts;
    public int[] debugTris;
    public Vector3[] debugNormals;
    public List<int3> blockDebug = new List<int3>();


    [Header("If this is disabled before staring play mode, no gizmos will exist")]
    public bool debugMode;


    public bool drawMeshGizmos;
    public bool drawMeshVerticesGizmos;
    public bool drawMeshEdgesGizmos;
    public bool drawMeshNormalsGizmos;
    public bool drawChunkGizmos;
    public bool drawNeigbourConnectionsGizmos;

    private void OnDrawGizmos()
    {
        if (debugMode == false)
        {
            return;
        }


        Vector3 transformPos = transform.position;

        float chunkSize = ChunkManager.staticChunkSize;
        float maxChunkHeight = ChunkManager.Instance.bs.maxChunkHeight;

        if (drawMeshGizmos)
        {
            Gizmos.DrawWireCube(transformPos + new Vector3(chunkSize * 0.5f - 0.5f, maxChunkHeight / 2 - 0.5f, chunkSize * 0.5f - 0.5f), new Vector3(chunkSize, maxChunkHeight, chunkSize));
        }

        if (Application.isPlaying == false)
        {
            return;
        }


        if (drawMeshVerticesGizmos)
        {
            if (debugVerts.Length == 0)
            {
                debugVerts = meshFilter.mesh.vertices;
            }
            else
            {
                meshFilter.mesh.vertices = debugVerts;
            }

            foreach (Vector3 vertex in debugVerts)
            {
                Gizmos.color = Color.black;

                if (Vector3.Distance(vertex, Vector3.zero) < 0.001f)
                {
                    Gizmos.color = Color.red;
                }

                Gizmos.DrawCube(vertex + transformPos, .1f * Vector3.one);
            }
        }

        Gizmos.color = Color.black;
        if (drawMeshEdgesGizmos)
        {
            if (debugTris.Length == 0)
            {
                debugTris = meshFilter.mesh.triangles;
            }
            else
            {
                meshFilter.mesh.triangles = debugTris;
            }

            for (int i = 0; i < debugTris.Length; i += 3)
            {
                // Get the vertex indices for this triangle
                int vertexIndex1 = debugTris[i];
                int vertexIndex2 = debugTris[i + 1];
                int vertexIndex3 = debugTris[i + 2];

                // Draw the triangle's edges
                Gizmos.DrawLine(debugVerts[vertexIndex1] + transformPos, debugVerts[vertexIndex2] + transformPos);
                Gizmos.DrawLine(debugVerts[vertexIndex2] + transformPos, debugVerts[vertexIndex3] + transformPos);
                Gizmos.DrawLine(debugVerts[vertexIndex3] + transformPos, debugVerts[vertexIndex1] + transformPos);
            }
        }

        Gizmos.color = Color.blue;
        if (drawMeshNormalsGizmos)
        {
            if (debugNormals.Length == 0)
            {
                debugNormals = meshFilter.mesh.normals;
            }
            else
            {
                meshFilter.mesh.normals = debugNormals;
            }

            Vector3 totalPositions = Vector3.zero;
            for (int i = 0; i < debugTris.Length; i += 6)
            {
                for (int i2 = 0; i2 < 6; i2++)
                {
                    totalPositions += debugVerts[debugTris[i + i2]];
                }

                Vector3 linePos = totalPositions * 16666666666666666666666666666667f;

                Gizmos.DrawLine(linePos, linePos + debugNormals[i]);
            }
        }

        Gizmos.color = Color.black;
        if (drawChunkGizmos)
        {
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

                        Gizmos.DrawCube(transformPos + new Vector3(x, y, z), Vector3.one * .2f);
                    }
                }
            }
        }

        Gizmos.color = Color.white;
        if (drawNeigbourConnectionsGizmos)
        {
            NativeArray<int3> blockPositions = ChunkManager.GetConnectedChunkEdgePositionsCount(chunkData.gridPos);

            foreach (int3 blockPosition in blockPositions)
            {
                Gizmos.DrawWireCube(transformPos + new Vector3(blockPosition.x, blockPosition.y, blockPosition.z), Vector3.one);
            }
        }
    }

    #endregion



    private void Example()
    {
        int arrayCount = 20;
        int arraySize = 20;

        NativeArray<float> example2DArray = new NativeArray<float>(arrayCount * arraySize, Allocator.TempJob);


        for (int i = 0; i < arrayCount; i++)
        {
            for (int index = 0; index < arraySize; index++)
            {
                example2DArray[i * arraySize + index] = 1;
            }
        }



        int indexX = 0;
        int indexY = 4;


        //pak waarde 0, 4 van de array
        float value = example2DArray[indexX *arraySize + indexY];


        example2DArray.Dispose();
    }
#endif
}



[System.Serializable]
public struct ChunkData
{
    public int3 gridPos;

    public NativeArray<int3> blockPositions;

    public NativeArray<int3> blockPositions_Left;
    public NativeArray<int3> blockPositions_Right;
    public NativeArray<int3> blockPositions_Forward;
    public NativeArray<int3> blockPositions_Back;
}


public enum ChunkState
{
    Unloaded,
    Loaded,
    Rendered,
};
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;
using System.Collections;



[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[BurstCompile]
public class Chunk : MonoBehaviour
{
    public ChunkData chunkData;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public ChunkState chunkState;




    public byte isRenderEdgeChunk;

    [BurstCompile]
    public void Init(byte _isRenderEdgeChunk)
    {
        ChunkManager.Instance.AddChunksToQue(this);

        isRenderEdgeChunk = _isRenderEdgeChunk;
    }





    [BurstCompile]
    public void LoadChunk(sbyte chunkSize, byte maxChunkHeight, int seed, float scale, int octaves, float persistence, float lacunarity)
    {
        chunkState = ChunkState.Loaded;

        int3 worldPos = new int3((int)transform.position.x, 0, (int)transform.position.z);
        int3 gridPos = worldPos / chunkSize;


        NativeArray<byte> noiseMap = NoiseMapJob.GenerateNoiseMap(chunkSize, maxChunkHeight, seed, scale, octaves, persistence, lacunarity, new int2(worldPos.x, worldPos.z));

        GenerateBlockPos(noiseMap, chunkSize, maxChunkHeight, gridPos);


        noiseMap.Dispose();
    }



    [BurstCompile]
    private void GenerateBlockPos(NativeArray<byte> noiseMap, sbyte chunkSize, byte maxChunkHeight, int3 gridPos)
    {
        int chunkSize_Times_MaxHeight = chunkSize * maxChunkHeight;

        NativeList<BlockPos> blockPositionsList = new NativeList<BlockPos>(chunkSize * chunkSize_Times_MaxHeight, Allocator.Persistent);

        NativeList<BlockPos> blockPositionsList_Left = new NativeList<BlockPos>(chunkSize_Times_MaxHeight, Allocator.Persistent);
        NativeList<BlockPos> blockPositionsList_Right = new NativeList<BlockPos>(chunkSize_Times_MaxHeight, Allocator.Persistent);
        NativeList<BlockPos> blockPositionsList_Forward = new NativeList<BlockPos>(chunkSize_Times_MaxHeight, Allocator.Persistent);
        NativeList<BlockPos> blockPositionsList_Back = new NativeList<BlockPos>(chunkSize_Times_MaxHeight, Allocator.Persistent);

        for (sbyte x = 0; x < chunkSize; x++)
        {
            for (sbyte z = 0; z < chunkSize; z++)
            {

                byte perlinValue = noiseMap[x * chunkSize + z];
                byte maxY = ClampUnderMax(perlinValue, ClampUnderMax(maxChunkHeight, 255));

                // Add block positions up to the max height
                for (byte y = 0; y < maxY; y++)
                {
                    sbyte posX = (sbyte)(x + chunkSize);
                    sbyte posZ = (sbyte)(z + chunkSize);

                    if (x == 0)
                    {
                        blockPositionsList_Left.Add(new BlockPos(posX, y, posZ));
                    }
                    else if (x == chunkSize - 1)
                    {
                        blockPositionsList_Right.Add(new BlockPos(posX, y, posZ));
                    }

                    if (z == 0)
                    {
                        blockPositionsList_Forward.Add(new BlockPos(posX, y, posZ));
                    }
                    else if (z == chunkSize - 1)
                    {
                        blockPositionsList_Back.Add(new BlockPos(posX, y, posZ));
                    }


                    blockPositionsList.Add(new BlockPos(x, y, z));
                }
            }
        }

        chunkData = new ChunkData(gridPos,
            blockPositionsList.AsArray(),
            blockPositionsList_Left.AsArray(),
            blockPositionsList_Right.AsArray(),
            blockPositionsList_Forward.AsArray(),
            blockPositionsList_Back.AsArray());
    }


    [BurstCompile]
    public void RenderChunk()
    {
        chunkState = ChunkState.Rendered;

        MeshCalculatorJob.CallGenerateMeshJob(chunkData.gridPos, ref chunkData.blockPositions, meshFilter.mesh, meshCollider);
    }



    [BurstCompile]
    private byte ClampUnderMax(byte value, byte max)
    {
        if (value > max)
        {
            value = max;
        }

        return value;
    }




#if UNITY_EDITOR

    public static Stopwatch sw;
    public static Stopwatch sw2;
    public static Stopwatch sw3;

    public Vector3[] debugVerts;
    public int[] debugTris;
    public List<int3> blockDebug = new List<int3>();


    [Header("If this is disabled before staring play mode, no gizmos will exist")]
    public bool debugMode;


    public bool drawMeshGizmos;
    public bool drawMeshVerticesGizmos;
    public bool drawMeshEdgesGizmos;
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

            NativeArray<BlockPos> blockPositions = ChunkManager.GetConnectedChunkEdgePositionsCount(chunkData.gridPos, out _);

            foreach (BlockPos blockPosition in blockPositions)
            {
                Gizmos.DrawWireCube(transformPos + new Vector3(blockPosition.x, blockPosition.y, blockPosition.z), Vector3.one);
            }
        }
    }




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

    public NativeArray<BlockPos> blockPositions;

    public NativeArray<BlockPos> blockPositions_Left;
    public NativeArray<BlockPos> blockPositions_Right;
    public NativeArray<BlockPos> blockPositions_Forward;
    public NativeArray<BlockPos> blockPositions_Back;
    

    public ChunkData(int3 _gridPos, NativeArray<BlockPos> _blockPositions, NativeArray<BlockPos> left, NativeArray<BlockPos> right, NativeArray<BlockPos> forward, NativeArray<BlockPos> back)
    {
        gridPos = _gridPos;

        blockPositions = _blockPositions;

        blockPositions_Left = left;
        blockPositions_Right = right;
        blockPositions_Forward = forward;
        blockPositions_Back = back;
    }
}

public enum Blocks : byte
{
    dirt = 1,
    oak =2,

birch = 3
}

public enum ChunkState : byte
{
    Unloaded,
    Loaded,
    Rendered,
};
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Diagnostics;
using System.Collections;
using Unity.Jobs;
using System.Threading;



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
    public void LoadChunk(sbyte chunkSize, byte maxChunkHeight, int seed, float scale, byte octaves, float persistence, float lacunarity)
    {
        sw = Stopwatch.StartNew();

        chunkState = ChunkState.Loaded;

        int3 worldPos = new int3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        int3 gridPos = worldPos / chunkSize;




        //calculate maxY from every XZ pos, and then fill the arrays...

        #region Calculate Noise Map and BlockPositions

        NativeArray<byte> noiseMap = NoiseMapJob.GenerateNoiseMap(chunkSize, maxChunkHeight, seed, scale, octaves, persistence, lacunarity, new int2(worldPos.x, worldPos.z));

        NativeArray<int> blockPositions_Amounts = new NativeArray<int>(7, Allocator.TempJob);


        CalculateChunkBlockDataJob calculateBlockData = new CalculateChunkBlockDataJob()
        {
            noiseMap = noiseMap,
            blockPositions_Amounts = blockPositions_Amounts,

            chunkSize = chunkSize,
        };

        JobHandle mainJobHandle = calculateBlockData.Schedule();
        mainJobHandle.Complete();

        if (isRenderEdgeChunk == 0)
        {
            print(sw.ElapsedTicks + "ticks final result total");
        }

        #endregion




        #region Set Block Data To Arrays and ChunkData JobParallel

        NativeArray<BlockPos> blockPositions = new NativeArray<BlockPos>(blockPositions_Amounts[0], Allocator.Persistent);

        NativeArray<BlockPos> blockPositions_Left = new NativeArray<BlockPos>(blockPositions_Amounts[1], Allocator.Persistent);
        NativeArray<BlockPos> blockPositions_Right = new NativeArray<BlockPos>(blockPositions_Amounts[2], Allocator.Persistent);
        //NativeArray<BlockPos> blockPositions_Bottom = new NativeArray<BlockPos>(blockPositions_Amounts[3], Allocator.Persistent);
        //NativeArray<BlockPos> blockPositions_Top = new NativeArray<BlockPos>(blockPositions_Amounts[4], Allocator.Persistent);
        NativeArray<BlockPos> blockPositions_Forward = new NativeArray<BlockPos>(blockPositions_Amounts[5], Allocator.Persistent);
        NativeArray<BlockPos> blockPositions_Back = new NativeArray<BlockPos>(blockPositions_Amounts[6], Allocator.Persistent);

        SetChunkBlockDataJobParallel setBlockData = new SetChunkBlockDataJobParallel()
        {
            noiseMap = noiseMap,

            blockPositions = blockPositions,

            blockPositions_Left = blockPositions_Left,
            blockPositions_Right = blockPositions_Right,
            //blockPositions_Bottom = blockPositions_Bottom,
            //blockPositions_Top = blockPositions_Top,
            blockPositions_Forward = blockPositions_Forward,
            blockPositions_Back = blockPositions_Back,

            chunkSize = chunkSize,
        };    

        mainJobHandle = setBlockData.Schedule(chunkSize * chunkSize, chunkSize * chunkSize);
        mainJobHandle.Complete();



        chunkData = new ChunkData(gridPos,
            blockPositions,
            blockPositions_Left,
            blockPositions_Right,
            //blockPositions_Bottom,
            //blockPositions_Top,
            blockPositions_Forward,
            blockPositions_Back);

        #endregion




        noiseMap.Dispose();
        blockPositions_Amounts.Dispose();

        if (isRenderEdgeChunk == 0)
        {
            print(sw.ElapsedTicks + "ticks final result total");
        }
    }




    [BurstCompile]
    private struct CalculateChunkBlockDataJob : IJob
    {
        [NoAlias][ReadOnly] public NativeArray<byte> noiseMap;

        [NoAlias] public NativeArray<int> blockPositions_Amounts;

        [NoAlias][ReadOnly] public sbyte chunkSize;


        [BurstCompile]
        public void Execute()
        {
            for (sbyte x = 0; x < chunkSize; x++)
            {
                for (sbyte z = 0; z < chunkSize; z++)
                {
                    int blockIndex = x * chunkSize + z;

                    byte maxY = noiseMap[blockIndex];


                    // Add block positions up to the max height
                    for (byte y = 0; y < maxY; y++)
                    {
                        blockPositions_Amounts[0] += 1;

                        if (x == 0)
                        {
                            blockPositions_Amounts[1] += 1;
                        }
                        else if (x == chunkSize - 1)
                        {
                            blockPositions_Amounts[2] += 1;
                        }

                        if (y == 0)
                        {
                            blockPositions_Amounts[3] += 1;
                        }
                        else if (y == chunkSize - 1)
                        {
                            blockPositions_Amounts[4] += 1;
                        }

                        if (z == 0)
                        {
                            blockPositions_Amounts[5] += 1;
                        }
                        else if (z == chunkSize - 1)
                        {
                            blockPositions_Amounts[6] += 1;
                        }
                    }
                }
            }
        }
    }




    [BurstCompile]
    private struct SetChunkBlockDataJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<byte> noiseMap;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions;


        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Left;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Right;

        //[NativeDisableParallelForRestriction]
        //[NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Bottom;

        //[NativeDisableParallelForRestriction]
        //[NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Top;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Forward;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> blockPositions_Back;

        [NoAlias][ReadOnly] public sbyte chunkSize;

        [NoAlias] private int cBlockIndex;
        [NoAlias] private int cBlockIndex_Left;
        [NoAlias] private int cBlockIndex_Right;
        //[NoAlias] private int cBlockIndex_Bottom;
        //[NoAlias] private int cBlockIndex_Top;
        [NoAlias] private int cBlockIndex_Forward;
        [NoAlias] private int cBlockIndex_Back;


        [BurstCompile]
        public void Execute(int blockIndex)
        {
            sbyte blockIndexX = (sbyte)(blockIndex / chunkSize);
            sbyte blockIndexZ = (sbyte)(blockIndex % chunkSize);
            //18 > 1, 0, 1
            //4  > 3, 0, 0
            //64 > 1, 1, 0



            byte maxY = noiseMap[blockIndex];


            for (byte blockIndexY = 0; blockIndexY < maxY; blockIndexY++)
            {
                BlockPos targetBlockPos = new BlockPos(blockIndexX, blockIndexY, blockIndexZ);


                blockPositions[cBlockIndex] = targetBlockPos;
                Interlocked.Increment(ref cBlockIndex);


                if (blockIndexX == 0)
                {
                    blockPositions_Left[cBlockIndex_Left] = targetBlockPos;
                    Interlocked.Increment(ref cBlockIndex_Left);
                }
                else if (blockIndexX == chunkSize - 1)
                {
                    blockPositions_Right[cBlockIndex_Right] = targetBlockPos;
                    Interlocked.Increment(ref cBlockIndex_Right);
                }


                //if (blockIndexY == 0)
                //{
                //    blockPositions_Bottom[cBlockIndex_Bottom] = targetBlockPos;
                //    Interlocked.Increment(ref cBlockIndex_Bottom);
                //}
                //else if (blockIndexY == 255)
                //{
                //    blockPositions_Top[cBlockIndex_Top] = targetBlockPos;
                //    Interlocked.Increment(ref cBlockIndex_Top);
                //}


                if (blockIndexZ == 0)
                {
                    blockPositions_Forward[cBlockIndex_Forward] = targetBlockPos;
                    Interlocked.Increment(ref cBlockIndex_Forward);
                }
                else if (blockIndexZ == chunkSize - 1)
                {
                    blockPositions_Back[cBlockIndex_Back] = targetBlockPos;
                    Interlocked.Increment(ref cBlockIndex_Back);
                }
            }
        }
    }








    [BurstCompile]
    public void RenderChunk()
    {
        chunkState = ChunkState.Rendered;

        MeshCalculatorJob.CallGenerateMeshJob(chunkData.gridPos, ref chunkData.blockPositions, meshFilter.mesh, meshCollider);
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
        float value = example2DArray[indexX * arraySize + indexY];


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
    //public NativeArray<BlockPos> blockPositions_Bottom;
    //public NativeArray<BlockPos> blockPositions_Top;
    public NativeArray<BlockPos> blockPositions_Forward;
    public NativeArray<BlockPos> blockPositions_Back;
    

    public ChunkData(int3 _gridPos,
        NativeArray<BlockPos> _blockPositions,
        NativeArray<BlockPos> left,
        NativeArray<BlockPos> right,
        //NativeArray<BlockPos> bottom,
        //NativeArray<BlockPos> top,
        NativeArray<BlockPos> forward,
        NativeArray<BlockPos> back)
    {
        gridPos = _gridPos;

        blockPositions = _blockPositions;

        blockPositions_Left = left;
        blockPositions_Right = right;
        //blockPositions_Bottom = bottom;
        //blockPositions_Top = top;
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
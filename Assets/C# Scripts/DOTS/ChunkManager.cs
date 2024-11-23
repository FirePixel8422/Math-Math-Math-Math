using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance;
    private void Awake()
    {
        Instance = this;
    }




    private static List<Chunk> chunkList;
    private static List<Chunk> chunkListMarkedForLoading;


    [Header("Chunk Load And Render Config")]
    public int chunkLoadCallsPerFrame;
    public int chunkRenderCallsPerFrame;


    public bool TEST_useAsyncLoading;
    public int TEST_chunkListQuadrantSize;


    [Header("World Gen Config")]
    public sbyte chunkSize;
    public static sbyte staticChunkSize;

    public int seed;
    public bool reSeedOnStart;

    public BiomeSettingsSO bs;

    private static NativeHashMap<int3, ChunkData> chunks;

    private Unity.Mathematics.Random random;




    [BurstCompile]
    public void Init()
    {
        if (reSeedOnStart)
        {
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
            seed = random.NextInt(-1000000, 1000001);
        }

        staticChunkSize = chunkSize;

        chunkList = new List<Chunk>(100);
        chunks = new NativeHashMap<int3, ChunkData>(TEST_chunkListQuadrantSize, Allocator.Persistent);

        chunkManagerSetupState = ChunkManagerSetupState.Loading;

        if (TEST_useAsyncLoading == false)
        {
            StartCoroutine(CallChunks());
        }
    }


    public static void AddChunksToQue(Chunk chunk)
    {
        if (chunkList.Contains(chunk) == false)
        {
            chunkList.Add(chunk);
        }
    }


    [BurstCompile]
    private IEnumerator CallChunks()
    {
        while (true)
        {
            yield return new WaitUntil(() => chunkList.Count > 0);

            int chunkCount = chunkList.Count;
            int chunksLoaded = 0;

            while (chunkCount > chunksLoaded)
            {
                for (int i = 0; i < chunkLoadCallsPerFrame; i++)
                {

                    if (chunkList[chunksLoaded].chunkState == ChunkState.Unloaded)
                    {
                        chunkList[chunksLoaded].ForceLoadChunk(chunkSize, bs.maxChunkHeight, seed, bs.scale, bs.octaves, bs.persistence, bs.lacunarity, bs.subChunkHeight, bs.typeOfChunkToGenerate);

                        chunks.TryAdd(chunkList[chunksLoaded].chunkData.gridPos, chunkList[chunksLoaded].chunkData);
                    }

                    chunksLoaded += 1;

                    if (chunksLoaded == chunkCount)
                    {
                        break;
                    }
                }

                if (chunksLoaded == chunkCount)
                {
                    break;
                }
                yield return null;
            }


            while (chunkList.Count > 0)
            {
                for (int i = 0; i < chunkRenderCallsPerFrame; i++)
                {
                    if (chunkList[0].chunkState == ChunkState.Loaded && chunkList[0].isRenderEdgeChunk == false)
                    {
                        chunkList[0].ForceRenderChunk();
                    }

                    chunkList.RemoveAt(0);

                    if (chunkList.Count == 0)
                    {
                        break;
                    }
                }

                yield return null;
            }
        }
    }





    private int cChunkIndex;
    public int chunkListCount;
    private JobHandle chunkJobHandle;


    public ChunkManagerSetupState chunkManagerSetupState;
    public enum ChunkManagerSetupState : byte
    {
        Idle,
        Loading,
        Rendering
    }




    [BurstCompile]
    private void Update()
    {
        if (TEST_useAsyncLoading == false)
        {
            return;
        }

        if (chunkManagerSetupState == ChunkManagerSetupState.Loading)
        {
            if (chunkListCount != 0)
            {
                LoadChunks();
            }
            else if (chunkList.Count > 0)
            {
                chunkListMarkedForLoading = new List<Chunk>(chunkList);
                chunkList.Clear();

                chunkListCount = chunkListMarkedForLoading.Count;

                LoadChunks();
            }
        }

        else if (chunkManagerSetupState == ChunkManagerSetupState.Rendering)
        {
            RenderChunks();
        }




        [BurstCompile]
        void LoadChunks()
        {
            //call chunkLoadCallsPerFrame amount of chunks for loading
            for (int i = 0; i < chunkLoadCallsPerFrame; i++)
            {
                chunkListMarkedForLoading[cChunkIndex].ForceLoadChunk(chunkSize, bs.maxChunkHeight, seed, bs.scale, bs.octaves, bs.persistence, bs.lacunarity, bs.subChunkHeight, bs.typeOfChunkToGenerate);

                //if all chunks that still have to load are called for loading, return and start rendering all the loaded chunks
                if (cChunkIndex == chunkListCount - 1)
                {
                    chunkManagerSetupState = ChunkManagerSetupState.Rendering;
                    cChunkIndex = 0;

                    break;
                }

                cChunkIndex += 1;
            }
        }


        [BurstCompile]
        void RenderChunks()
        {
            //only if chunks are currently called for rendering, check if all jobs are finished
            if (chunkJobHandle.IsCompleted)
            {

                //call chunkRenderCallsPerFrame amount of chunks for rendering
                for (int i = 0; i < chunkRenderCallsPerFrame; i++)
                {
                    JobHandle mainJobHandle = new JobHandle();

                    chunkListMarkedForLoading[cChunkIndex].ForceRenderChunk();

                    chunkJobHandle = JobHandle.CombineDependencies(chunkJobHandle, mainJobHandle);


                    //if all chunks that still have to render are called for rendering, return and start loading new batch of chunks
                    if (cChunkIndex == chunkListCount - 1)
                    {
                        chunkManagerSetupState = ChunkManagerSetupState.Loading;
                        chunkListCount = 0;
                        cChunkIndex = 0;

                        break;
                    }

                    cChunkIndex += 1;
                }
            }
        }
    }





    #region GetConnectedChunkEdges

    private static readonly int3[] directionalOffsets = new int3[4]
    {
        new int3(-1, 0, 0),     // Left
        new int3(1, 0, 0),      // Right
        //new int3(0, 1, 0),      // Bottom
        //new int3(0, -1, 0),     // Top
        new int3(0, 0, -1),     // Forward
        new int3(0, 0, 1),      // Back
    };

    public static NativeArray<BlockPos> GetConnectedChunkEdgePositionsCount(int3 requesterChunkPos, out JobHandle jobHandle)
    {
        #region Setup Chunk Neigbour Data

        int leftAmount = 0, rightAmount = 0, backAmount = 0, forwardAmount = 0;


        NativeArray<BlockPos>[] neighbourBlockPositions = new NativeArray<BlockPos>[4];

        for (byte i = 0; i < directionalOffsets.Length; i++)
        {
            if (chunks.TryGetValue(requesterChunkPos + directionalOffsets[i], out ChunkData chunkDataNeighbor))
            {
                switch (i)
                {
                    case 0: // Left

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Right;
                        leftAmount = chunkDataNeighbor.blockPositions_Right.Length;

                        break;

                    case 1: // Right

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Left;
                        rightAmount = chunkDataNeighbor.blockPositions_Left.Length;
                        
                        break;

                    //case 2: // Bottom

                    //    neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Top;
                    //    bottomAmount = chunkDataNeighbor.blockPositions_Top.Length;

                    //    break;

                    //case 3: // Top

                    //    neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Bottom;
                    //    topAmount = chunkDataNeighbor.blockPositions_Bottom.Length;

                    //    break;

                    case 2: // Forward

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Back;
                        forwardAmount = chunkDataNeighbor.blockPositions_Back.Length;
                        
                        break;
                    
                    case 3: // Back

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Forward;
                        backAmount = chunkDataNeighbor.blockPositions_Forward.Length;
                        
                        break;
                }
            }
        }

        int totalAmount = leftAmount + rightAmount + forwardAmount + backAmount;

        #endregion

        //40 ticks for setting up neighbour data




        NativeArray<BlockPos> connectedChunkEdgePositions = new NativeArray<BlockPos>(totalAmount, Allocator.TempJob);


        jobHandle = new JobHandle();
        int startIndex = 0;

        if (leftAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesLeft = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[0],

                dirModifier = new BlockPos((sbyte)-staticChunkSize, 0, 0),
            };

            startIndex += leftAmount;

            jobHandle = addConnectedChunkEdgesLeft.Schedule(leftAmount, leftAmount);
        }


        if (rightAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesRight = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[1],

                startIndex = startIndex,

                dirModifier = new BlockPos(staticChunkSize, 0, 0),
            };

            startIndex += rightAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesRight.Schedule(rightAmount, rightAmount), jobHandle);
        }


        /*if (bottomAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesForward = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[2],

                startIndex = startIndex,
            };

            startIndex += bottomAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesForward.Schedule(bottomAmount, bottomAmount, mainJobHandle), jobHandle);
        }


        if (topAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesBack = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[3],

                startIndex = startIndex,
            };

            startIndex += topAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesBack.Schedule(topAmount, topAmount, mainJobHandle), jobHandle);
        }*/


        if (forwardAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesForward = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[2],

                startIndex = startIndex,

                dirModifier = new BlockPos(0, 0, (sbyte)-staticChunkSize),
            };

            startIndex += forwardAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesForward.Schedule(forwardAmount, forwardAmount), jobHandle);
        }


        if (backAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesBack = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[3],

                startIndex = startIndex,

                dirModifier = new BlockPos(0, 0, staticChunkSize),
            };

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesBack.Schedule(backAmount, backAmount), jobHandle);
        }
        //250 ticks per job





        return connectedChunkEdgePositions;
    }




    [BurstCompile]
    private struct AddConnectedChunkEdgeJobParallel : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [NoAlias][WriteOnly] public NativeArray<BlockPos> connectedChunkEdgePositions;

        [NoAlias][ReadOnly] public NativeArray<BlockPos> blockPositions;

        [NoAlias][ReadOnly] public int startIndex;

        [NoAlias][ReadOnly] public BlockPos dirModifier;


        [BurstCompile]
        public void Execute(int index)
        {
            connectedChunkEdgePositions[startIndex + index] = new BlockPos((sbyte)(blockPositions[index].x + dirModifier.x), blockPositions[index].y, (sbyte)(blockPositions[index].z + dirModifier.z));
        }
    }

    #endregion
}

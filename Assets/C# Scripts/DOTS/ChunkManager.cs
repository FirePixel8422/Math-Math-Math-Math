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

    public int chunkCount;
    public int chunksLoaded;
    public int chunksRendered;

    [Header("")]
    public int chunkLoadCallsPerFrame;

    public int chunkRenderCallsPerFrame;


    [Header("")]
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
            seed = random.NextInt(-1000000, 1000001);
        }

        staticChunkSize = chunkSize;

        Chunk[] chunkArray = FindObjectsOfType<Chunk>();

        chunkList = new List<Chunk>(chunkArray.Length);

        chunkList.AddRange(chunkArray);

        chunks = new NativeHashMap<int3, ChunkData>(chunkArray.Length, Allocator.Persistent);

        StartCoroutine(CallChunks());
    }


    public void AddChunksToQue(Chunk chunk)
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

            chunkCount = chunkList.Count;
            chunksLoaded = 0;

            while (chunkCount > chunksLoaded)
            {
                for (int i = 0; i < chunkLoadCallsPerFrame; i++)
                {

                    if (chunkList[chunksLoaded].chunkState == ChunkState.Unloaded)
                    {
                        chunkList[chunksLoaded].LoadChunk(chunkSize, bs.maxChunkHeight, seed, bs.scale, bs.octaves, bs.persistence, bs.lacunarity);

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
                    if (chunkList[0].chunkState == ChunkState.Loaded && chunkList[0].isRenderEdgeChunk == 0)
                    {
                        chunkList[0].RenderChunk();
                    }

                    chunksRendered += 1;

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

    





    private static readonly int3[] directionalOffsets = new int3[]
    {
        new int3(-1, 0, 0),     // Left
        new int3(1, 0, 0),      // Right
        new int3(0, 1, 0),      // Bottom
        new int3(0, -1, 0),     // Top
        new int3(0, 0, -1),     // Forward
        new int3(0, 0, 1),      // Back
    };

    public static NativeArray<BlockPos> GetConnectedChunkEdgePositionsCount(int3 requesterChunkPos, out JobHandle jobHandle)
    {
        #region Setup Chunk Neigbour Data

        JobHandle mainJobHandle = new JobHandle();

        int leftAmount = 0, rightAmount = 0, bottomAmount = 0, topAmount = 0, backAmount = 0, forwardAmount = 0;


        NativeArray<BlockPos>[] neighbourBlockPositions = new NativeArray<BlockPos>[6];

        for (byte i = 0; i < 6; i++)
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

        int totalAmount = leftAmount + rightAmount + bottomAmount + topAmount + forwardAmount + backAmount;

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
}

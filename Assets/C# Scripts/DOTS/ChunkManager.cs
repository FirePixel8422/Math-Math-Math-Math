using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
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
    public int chunkSize;

    public static int staticChunkSize;

    public int seed;
    public bool reSeedOnStart;

    public int atlasSize;

    public BiomeSettingsSO bs;

    private static NativeHashMap<int3, ChunkData> chunks;



    public void Init()
    {
        if (reSeedOnStart)
        {
            seed = UnityEngine.Random.Range(-1000000, 1000001);
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
        chunkList.Add(chunk);
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
                    chunkList[chunksLoaded].LoadChunk(chunkSize, bs.maxChunkHeight, seed, bs.scale, bs.octaves, bs.persistence, bs.lacunarity);

                    chunks.TryAdd(chunkList[chunksLoaded].chunkData.gridPos, chunkList[chunksLoaded].chunkData);

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
                    chunkList[0].RenderChunk(atlasSize);

                    chunkList.RemoveAt(0);

                    chunksRendered += 1;

                    if (chunkList.Count == 0)
                    {
                        break;
                    }
                }

                yield return null;
            }
        }
    }


    public static NativeArray<int3> GetConnectedChunkEdgePositionsCount(int3 requesterChunkPos)
    {
        #region Setup Chunk Neigbour Data

        int totalAmount = 0;
        int leftAmount = 0, rightAmount = 0, backAmount = 0, forwardAmount = 0;

        ChunkData chunkDataFront, chunkDataRight, chunkDataBack, chunkDataLeft;

        if (chunks.TryGetValue(requesterChunkPos + new int3(0, 0, -1), out chunkDataFront))
        {
            //chunk in front retuns its edge at its back to requesterChunk
            forwardAmount = chunkDataFront.blockPositions_Back.Length;
            totalAmount += forwardAmount;
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(1, 0, 0), out chunkDataRight))
        {
            //chunk at right retuns its edge at its left to requesterChunk
            rightAmount = chunkDataRight.blockPositions_Left.Length;
            totalAmount += rightAmount;
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(0, 0, 1), out chunkDataBack))
        {
            //chunk behind retuns its edge at its front to requesterChunk
            backAmount = chunkDataBack.blockPositions_Forward.Length;
            totalAmount += backAmount;
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(-1, 0, 0), out chunkDataLeft))
        {
            //chunk at left retuns its edge at its right to requesterChunk
            leftAmount = chunkDataLeft.blockPositions_Right.Length;
            totalAmount += leftAmount;
        }

        #endregion

        NativeArray<int3> connectedChunkEdgePositions = new NativeArray<int3>(totalAmount, Allocator.TempJob);




        JobHandle jobHandle = new JobHandle();
        int startIndex = 0;

        if (leftAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesLeft = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataLeft.blockPositions_Right,

                dirModifier = new int3(-Instance.chunkSize, 0, 0),

                chunkSize = staticChunkSize,
            };

            startIndex += leftAmount;

            jobHandle = addConnectedChunkEdgesLeft.Schedule(leftAmount, leftAmount);
        }

        if (rightAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesRight = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataRight.blockPositions_Left,

                startIndex = startIndex,

                dirModifier = new int3(Instance.chunkSize, 0, 0),

                chunkSize = staticChunkSize,
            };

            startIndex += rightAmount;

            jobHandle = addConnectedChunkEdgesRight.Schedule(rightAmount, rightAmount, jobHandle);
        }

        if (forwardAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesForward = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataFront.blockPositions_Back,

                startIndex = startIndex,

                dirModifier = new int3(0, 0, -Instance.chunkSize),

                chunkSize = staticChunkSize,
            };

            startIndex += forwardAmount;

            jobHandle = addConnectedChunkEdgesForward.Schedule(forwardAmount, forwardAmount, jobHandle);
        }

        if (backAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesBack = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataBack.blockPositions_Forward,

                startIndex = startIndex,

                dirModifier = new int3(0, 0, Instance.chunkSize),

                chunkSize = staticChunkSize,
            };

            jobHandle = addConnectedChunkEdgesBack.Schedule(backAmount, backAmount, jobHandle);
        }


        jobHandle.Complete();

        return connectedChunkEdgePositions;
    }




    [BurstCompile]
    private struct AddConnectedChunkEdge : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int3> connectedChunkEdgePositions;

        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NoAlias][ReadOnly] public int startIndex;

        [NoAlias][ReadOnly] public int3 dirModifier;

        [NoAlias][ReadOnly] public int chunkSize;


        [BurstCompile]
        public void Execute(int index)
        {
            connectedChunkEdgePositions[startIndex + index] = blockPositions[index] + dirModifier - new int3(chunkSize, 0, chunkSize);
        }
    }
}

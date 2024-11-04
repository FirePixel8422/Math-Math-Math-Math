using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public int chunkSize;

    public static int staticChunkSize;

    public int seed;
    public bool reSeedOnStart;

    public int atlasSize;

    public BiomeSettingsSO bs;

    private static NativeHashMap<int3, ChunkData> chunks;


    [BurstCompile]
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
                    if (chunkList[0].chunkState == ChunkState.Loaded && chunkList[0].isRenderEdgeChunk == false)
                    {
                        chunkList[0].RenderChunk(atlasSize);
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

    public static Stopwatch sw;

    public static NativeArray<int3> GetConnectedChunkEdgePositionsCount(int3 requesterChunkPos, out JobHandle jobHandle)
    {
        sw = Stopwatch.StartNew();

        #region Setup Chunk Neigbour Data

        int totalAmount = 0;
        int leftAmount = 0, rightAmount = 0, backAmount = 0, forwardAmount = 0;

        int3[] offsets = new int3[]
        {
            new int3(0, 0, -1),   // Front
            new int3(1, 0, 0),    // Right
            new int3(0, 0, 1),    // Back
            new int3(-1, 0, 0)    // Left
        };


        NativeArray<int3>[] neighbourBlockPositions = new NativeArray<int3>[4];

        for (int i = 0; i < offsets.Length; i++)
        {
            if (chunks.TryGetValue(requesterChunkPos + offsets[i], out ChunkData chunkDataNeighbor))
            {
                switch (i)
                {
                    case 0: // Front

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Back;
                        forwardAmount = chunkDataNeighbor.blockPositions_Back.Length;
                        
                        break;
                    
                    case 1: // Right

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Left;
                        rightAmount = chunkDataNeighbor.blockPositions_Left.Length;
                        
                        break;

                    case 2: // Back

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Forward;
                        backAmount = chunkDataNeighbor.blockPositions_Forward.Length;
                        
                        break;

                    case 3: // Left

                        neighbourBlockPositions[i] = chunkDataNeighbor.blockPositions_Right;
                        leftAmount = chunkDataNeighbor.blockPositions_Right.Length;
                        
                        break;
                }

                totalAmount = forwardAmount + rightAmount + backAmount + leftAmount;
            }
        }

        #endregion

        print(sw.ElapsedTicks + " ticks for setupNeighbour data");
        sw = Stopwatch.StartNew();

        NativeArray<int3> connectedChunkEdgePositions = new NativeArray<int3>(totalAmount, Allocator.TempJob);



        jobHandle = new JobHandle();
        int startIndex = 0;

        if (leftAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesLeft = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[3],

                dirModifier = new int3(-Instance.chunkSize, 0, 0),

                chunkSize = staticChunkSize,
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

                dirModifier = new int3(Instance.chunkSize, 0, 0),

                chunkSize = staticChunkSize,
            };

            startIndex += rightAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesRight.Schedule(rightAmount, rightAmount, jobHandle), jobHandle);
        }

        if (forwardAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesForward = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[0],

                startIndex = startIndex,

                dirModifier = new int3(0, 0, -Instance.chunkSize),

                chunkSize = staticChunkSize,
            };

            startIndex += forwardAmount;

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesForward.Schedule(forwardAmount, forwardAmount, jobHandle), jobHandle);
        }

        if (backAmount != 0)
        {
            AddConnectedChunkEdgeJobParallel addConnectedChunkEdgesBack = new AddConnectedChunkEdgeJobParallel()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = neighbourBlockPositions[2],

                startIndex = startIndex,

                dirModifier = new int3(0, 0, Instance.chunkSize),

                chunkSize = staticChunkSize,
            };

            jobHandle = JobHandle.CombineDependencies(addConnectedChunkEdgesBack.Schedule(backAmount, backAmount, jobHandle), jobHandle);
        }

        print(sw.ElapsedTicks + " ticks for the rest");

        return connectedChunkEdgePositions;
    }




    [BurstCompile]
    private struct AddConnectedChunkEdgeJobParallel : IJobParallelFor
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

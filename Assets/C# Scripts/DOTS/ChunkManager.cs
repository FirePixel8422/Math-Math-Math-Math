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

    [Header("")]
    public int chunkLoadCallsPerFrame;
    public float chunkLoadBatchDelay;

    public int chunkRenderCallsPerFrame;
    public float chunkRenderBatchDelay;


    [Header("")]
    public int atlasSize;

    public int chunkSize, maxChunkHeight;


    public static int staticChunkSize;

    public int seed;
    public bool reSeedOnStart;


    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;

    private static NativeHashMap<int3, ChunkData> chunks;



    private void Start()
    {
        if (reSeedOnStart)
        {
            seed = UnityEngine.Random.Range(-100000, 100001);
        }

        staticChunkSize = chunkSize;

        Chunk[] chunkArray = FindObjectsOfType<Chunk>();

        chunkList = new List<Chunk>(chunkArray.Length);

        chunkList.AddRange(chunkArray);

        chunkCount = chunkList.Count;

        chunks = new NativeHashMap<int3, ChunkData>(chunkArray.Length, Allocator.Persistent);

        StartCoroutine(CallChunks());
    }


    public static void AddChunksToQue(Chunk chunk)
    {
        chunkList.Add(chunk);
    }

    [BurstCompile]
    private IEnumerator CallChunks()
    {
        WaitForSeconds loadWait = new WaitForSeconds(chunkLoadBatchDelay);
        WaitForSeconds renderWait = new WaitForSeconds(chunkRenderBatchDelay);

        chunksLoaded = 0;

        while (chunkCount > chunksLoaded)
        {
            for (int i = 0; i < chunkLoadCallsPerFrame; i++)
            {
                chunkList[chunksLoaded].LoadChunk(chunkSize, maxChunkHeight, seed, scale, octaves, persistence, lacunarity);


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

            yield return loadWait;
        }


        while (chunkList.Count > 0)
        {
            for (int i = 0; i < chunkRenderCallsPerFrame; i++)
            {
                chunkList[0].RenderChunk(atlasSize);

                chunkList.RemoveAt(0);

                if (chunkList.Count == 0)
                {
                    break;
                }
            }

            yield return renderWait;
        }
    }


    public static NativeArray<int3> GetConnectedChunkEdgePositionsCount(int3 requesterChunkPos, bool debug = false)
    {
        #region Setup Chunk Neigbour Data

        if (debug)
        {
            print(requesterChunkPos);
        }

        int totalAmount = 0;
        int leftAmount = 0, rightAmount = 0, backAmount = 0, forwardAmount = 0;

        ChunkData chunkDataFront, chunkDataRight, chunkDataBack, chunkDataLeft;

        if (chunks.TryGetValue(requesterChunkPos + new int3(0, 0, -1), out chunkDataFront))
        {
            //chunk in front retuns its edge at its back to requesterChunk
            forwardAmount = chunkDataFront.blockPositions_Back.Length;
            totalAmount += forwardAmount;

            if (debug)
            {
                print(forwardAmount);
            }
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(1, 0, 0), out chunkDataRight))
        {
            //chunk at right retuns its edge at its left to requesterChunk
            rightAmount = chunkDataRight.blockPositions_Left.Length;
            totalAmount += rightAmount;

            if (debug)
            {
                print(rightAmount);
            }
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(0, 0, 1), out chunkDataBack))
        {
            //chunk behind retuns its edge at its front to requesterChunk
            backAmount = chunkDataBack.blockPositions_Forward.Length;
            totalAmount += backAmount;

            if (debug)
            {
                print(backAmount);
            }
        }

        if (chunks.TryGetValue(requesterChunkPos + new int3(-1, 0, 0), out chunkDataLeft))
        {
            //chunk at left retuns its edge at its right to requesterChunk
            leftAmount = chunkDataLeft.blockPositions_Right.Length;
            totalAmount += leftAmount;

            if (debug)
            {
                print(leftAmount);
            }
        }

        #endregion

        NativeArray<int3> connectedChunkEdgePositions = new NativeArray<int3>(totalAmount, Allocator.TempJob);




        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(4, Allocator.TempJob);
        int startIndex = 0;

        if(leftAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesLeft = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataLeft.blockPositions_Right,

                dirModifier = new int3(-staticChunkSize, 0, 0),
            };

            startIndex += leftAmount;

            jobHandles.Add(addConnectedChunkEdgesLeft.Schedule(leftAmount, leftAmount));
        }

        if (rightAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesRight = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataRight.blockPositions_Left,

                startIndex = startIndex,

                dirModifier = new int3(staticChunkSize, 0, 0),
            };

            startIndex += rightAmount;

            jobHandles.Add(addConnectedChunkEdgesRight.Schedule(rightAmount, rightAmount, jobHandles.Length == 0 ? default : jobHandles[jobHandles.Length -1]));
        }

        if(forwardAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesForward = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataFront.blockPositions_Back,

                startIndex = startIndex,

                dirModifier = new int3(0, 0, -staticChunkSize),
            };

            startIndex += forwardAmount;

            jobHandles.Add(addConnectedChunkEdgesForward.Schedule(forwardAmount, forwardAmount, jobHandles.Length == 0 ? default : jobHandles[jobHandles.Length - 1]));
        }

        if(backAmount != 0)
        {
            AddConnectedChunkEdge addConnectedChunkEdgesBack = new AddConnectedChunkEdge()
            {
                connectedChunkEdgePositions = connectedChunkEdgePositions,

                blockPositions = chunkDataBack.blockPositions_Forward,

                startIndex = startIndex,

                dirModifier = new int3(0, 0, staticChunkSize),
            };

            jobHandles.Add(addConnectedChunkEdgesBack.Schedule(backAmount, backAmount, jobHandles.Length == 0 ? default : jobHandles[jobHandles.Length - 1]));
        }


        JobHandle.CompleteAll(jobHandles.AsArray());

        if (debug)
        {
            Instance.debugBlockPositions = new int3[connectedChunkEdgePositions.Length];

            for (int i = 0; i < Instance.debugBlockPositions.Length; i++)
            {
                Instance.debugBlockPositions[i] = connectedChunkEdgePositions[i];
            }
        }

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


        [BurstCompile]
        public void Execute(int index)
        {
            connectedChunkEdgePositions[startIndex + index] = blockPositions[index] + dirModifier - new int3(16, 0, 16);
        }
    }



    public int3[] debugBlockPositions;
    public Vector3 offset;

    private void OnDrawGizmos()
    {
        if(debugBlockPositions.Length == 0)
        {
            return;
        }

        for (int i = 0; i < debugBlockPositions.Length; i++)
        {
            Gizmos.DrawWireCube(new Vector3(debugBlockPositions[i].x, debugBlockPositions[i].y, debugBlockPositions[i].z) + offset, Vector3.one);
        }
    }

}

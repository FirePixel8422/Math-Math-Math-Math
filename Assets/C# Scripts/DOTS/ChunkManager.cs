using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public class ChunkManager : MonoBehaviour
{
    private static List<Chunk> chunkList;



    public int chunkCallsPerFrame;

    public float batchDelay;

    private NativeHashMap<int3, ChunkData> chunks;



    private void Start()
    {
        Chunk[] chunkArray = FindObjectsOfType<Chunk>();

        chunkList = new List<Chunk>(chunkArray.Length);

        chunkList.AddRange(chunkArray);

        chunks = new NativeHashMap<int3, ChunkData>(chunkArray.Length, Allocator.Persistent);

        StartCoroutine(CallChunks());
    }



    public static void AddChunksToQue(Chunk chunk)
    {
        chunkList.Add(chunk);
    }

    private IEnumerator CallChunks()
    {
        WaitForSeconds wait = new WaitForSeconds(batchDelay);

        while (true)
        {
            yield return new WaitUntil(() => chunkList.Count > 0);

            for (int i = 0; i < chunkCallsPerFrame; i++)
            {
                chunkList[0].Init();

                chunks.TryAdd(chunkList[0].chunkData.gridPos, chunkList[0].chunkData);

                chunkList.RemoveAt(0);

                if (chunkList.Count == 0)
                {
                    break;
                }
            }

            yield return wait;
        }
    }
}

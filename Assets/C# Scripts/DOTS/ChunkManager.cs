using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance;
    private void Awake()
    {
        Instance = this;
    }



    public int chunkCallsPerFrame;

    public float batchDelay;

    public List<Chunk> chunkList;



    private void Start()
    {
        Chunk[] chunkArray = FindObjectsOfType<Chunk>();

        chunkList.AddRange(chunkArray);

        StartCoroutine(CallChunks());
    }



    public void AddChunksToQue(Chunk chunk)
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
                chunkList[0].GenerateBlockPos();
                chunkList.RemoveAt(0);

                if(chunkList.Count == 0)
                {
                    break;
                }
            }

            yield return wait;
        }
    }
}

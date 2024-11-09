using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public class ChunkRenderer : MonoBehaviour
{
    public GameObject chunkPrefab;

    public int renderDistance;



    private Vector3 lastPlayerPosition;
    private int chunkSize;
    //private int halfChunkSize;

    private HashSet<Chunk> chunksList;
    private HashSet<int3> chunksPosList;



    [BurstCompile]
    private void Start()
    {
        MeshCalculatorJob.Init();
        ChunkManager.Instance.Init();

        chunkSize = ChunkManager.Instance.chunkSize;

        chunksList = new HashSet<Chunk>();
        chunksPosList = new HashSet<int3>();

        GenerateInitialChunks();
    }


    [BurstCompile]
    private void GenerateInitialChunks()
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                int3 initialChunkPosition = new int3(x * chunkSize, 0, z * chunkSize);
                GenerateChunk(initialChunkPosition, (byte)((x == -renderDistance || z == -renderDistance || x == renderDistance || z == renderDistance) ? 1 : 0));
            }
        }
    }

    [BurstCompile]
    private void Update()
    {
        Vector3 xzPos = new Vector3(transform.position.x, 0, transform.position.z);
        
        if (Vector3.Distance(xzPos, lastPlayerPosition) > chunkSize)
        {
            lastPlayerPosition = xzPos;
            CheckIfChunkIsWithinRenderDistance();
            DisableChunksOutsideRenderDistance();
        }
    }

    [BurstCompile]
    public void GenerateChunk(int3 position, byte isRenderEdgeChunk)
    {
        Chunk chunk = Instantiate(chunkPrefab, new Vector3(position.x, position.y, position.z), Quaternion.identity).GetComponent<Chunk>();
        chunk.Init(isRenderEdgeChunk);

        if (isRenderEdgeChunk == 0)
        {
            chunksList.Add(chunk);
            chunksPosList.Add(position);
        }
    }

    [BurstCompile]
    public void CheckIfChunkIsWithinRenderDistance()
    {
        int playerChunkX = Mathf.FloorToInt(transform.position.x / chunkSize);
        int playerChunkZ = Mathf.FloorToInt(transform.position.z / chunkSize);

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                int3 chunkPosition = new int3((playerChunkX + x) * chunkSize, 0, (playerChunkZ + z) * chunkSize);

                if (!chunksPosList.Contains(chunkPosition))
                {
                    GenerateChunk(chunkPosition, (byte)((x == -renderDistance || z == -renderDistance || x == renderDistance || z == renderDistance) ? 1 : 0));
                }
            }
        }
    }

    [BurstCompile]
    private void DisableChunksOutsideRenderDistance()
    {
        int playerChunkX = Mathf.FloorToInt(transform.position.x / chunkSize);
        int playerChunkZ = Mathf.FloorToInt(transform.position.z / chunkSize);

        foreach (Chunk chunk in chunksList)
        {
            Vector3 chunkPosition = chunk.transform.position;
            int chunkX = Mathf.FloorToInt(chunkPosition.x / chunkSize);
            int chunkZ = Mathf.FloorToInt(chunkPosition.z / chunkSize);

            if (Mathf.Abs(chunkX - playerChunkX) > renderDistance || Mathf.Abs(chunkZ - playerChunkZ) > renderDistance)
            {
                chunk.gameObject.SetActive(false);
            }
            else
            {
                chunk.gameObject.SetActive(true);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public class ChunkRenderer : MonoBehaviour
{
    public GameObject chunkPrefab;

    [Header("<Render Settings>")]
    public int renderDistance;

    [Header("Render chunks around the player in a sphere, instead of a square")]
    public bool TEST_useSphericalRendering;

    [Header("Render less chunks out of vision")]
    public bool TEST_useForwardRendering;


    private Vector3 lastPlayerPosition;
    private int chunkSize;


    //private int halfChunkSize;

    private Dictionary<int3, Chunk> chunksList;


    [BurstCompile]
    private void Start()
    {
        MeshCalculatorJob.Init();
        ChunkManager.Instance.Init();

        chunkSize = ChunkManager.Instance.chunkSize;

        chunksList = new Dictionary<int3, Chunk>();

        GenerateInitialChunks();
    }


    [BurstCompile]
    private void GenerateInitialChunks()
    {
        int renderDistanceSqr = renderDistance * renderDistance;

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                //check if chunk is in sphereRenderDistance and calculate isRenderEdgeChunk
                if (IsChunkOutRenderDistance(x, z, renderDistanceSqr, out bool isRenderEdgeChunk))
                {
                    continue;
                }


                int3 initialChunkPosition = new int3(x * chunkSize, 0, z * chunkSize);
                GenerateChunk(initialChunkPosition, isRenderEdgeChunk);
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

            int playerChunkX = Mathf.FloorToInt(xzPos.x / chunkSize);
            int playerChunkZ = Mathf.FloorToInt(xzPos.z / chunkSize);

            int renderDistanceSqr = renderDistance * renderDistance;

            CreateChunksWithinRenderDistance(playerChunkX, playerChunkZ, renderDistanceSqr);
            ToggleChunksByRenderDistance(playerChunkX, playerChunkZ, renderDistanceSqr);
        }
    }


    [BurstCompile]
    public void CreateChunksWithinRenderDistance(int playerChunkX, int playerChunkZ, int renderDistanceSqr)
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                //check if chunk is in sphereRenderDistance and calculate isRenderEdgeChunk
                if (IsChunkOutRenderDistance(x, z, renderDistanceSqr, out bool isRenderEdgeChunk))
                {
                    continue;
                }


                int3 chunkPosition = new int3((playerChunkX + x) * chunkSize, 0, (playerChunkZ + z) * chunkSize);

                if (!chunksList.TryGetValue(chunkPosition, out Chunk chunk) || chunk.isRenderEdgeChunk)
                {
                    GenerateChunk(chunkPosition, isRenderEdgeChunk);
                }
            }
        }
    }


    [BurstCompile]
    private void ToggleChunksByRenderDistance(int playerChunkX, int playerChunkZ, int renderDistanceSqr)
    {
        foreach (KeyValuePair<int3, Chunk> chunk in chunksList)
        {
            int3 chunkGridPos = chunk.Value.chunkData.gridPos;


            if (TEST_useSphericalRendering)
            {
                // Calculate squared distance between player and targetChunk
                int distanceSqr = (chunkGridPos.x - playerChunkX) * (chunkGridPos.x - playerChunkX) + (chunkGridPos.z - playerChunkZ) * (chunkGridPos.z - playerChunkZ);

                //toggle chunk based on sphere around player
                if (distanceSqr >= renderDistanceSqr)
                {
                    chunk.Value.gameObject.SetActive(false);
                }
                else
                {
                    chunk.Value.gameObject.SetActive(true);
                }
            }
            else
            {
                //toggle chunks based on square around player
                if (math.abs(chunkGridPos.x - playerChunkX) > renderDistance || math.abs(chunkGridPos.z - playerChunkZ) > renderDistance)
                {
                    chunk.Value.gameObject.SetActive(false);
                }
                else
                {
                    chunk.Value.gameObject.SetActive(true);
                }
            }
        }
    }




    [BurstCompile]
    public void GenerateChunk(int3 position, bool isRenderEdgeChunk)
    {
        if (chunksList.TryGetValue(position, out Chunk chunk))
        {
            chunk.Init(isRenderEdgeChunk);
        }
        else
        {
            Chunk newChunk = Instantiate(chunkPrefab, new Vector3(position.x, position.y, position.z), Quaternion.identity).GetComponent<Chunk>();
            newChunk.Init(isRenderEdgeChunk);

            chunksList.Add(position, newChunk);
        }
    }


    [BurstCompile]
    private bool IsChunkOutRenderDistance(int x, int z, int renderDistanceSqr, out bool isRenderEdgeChunk)
    {
        //"x * x + z * z > renderDistanceSqr" checks if the chunk is in the RenderDistance Sphere
        if (TEST_useSphericalRendering)
        {
            // Calculate squared distance from the point (x, z) to the origin (0, 0)
            int distanceSqr = x * x + z * z - 2;

            //return function if chunk is outside of sphere
            if (distanceSqr > renderDistanceSqr)
            {
                isRenderEdgeChunk = false;
                return true;
            }

            //if chunk is on one of the sides of the sphere based render distance
            isRenderEdgeChunk = math.abs(distanceSqr - renderDistanceSqr) < renderDistance * 2 - 2;
        }
        else
        {
            //if chunk is on one of the sides of the square based render distance
            isRenderEdgeChunk = x == -renderDistance || z == -renderDistance || x == renderDistance || z == renderDistance;
        }

        return false;
    }
}

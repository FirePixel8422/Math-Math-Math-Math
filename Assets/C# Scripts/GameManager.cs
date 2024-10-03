//using System.Collections.Generic;
//using UnityEngine;

//public class GameManager : MonoBehaviour
//{
//    public static GameManager Instance;
//    public int chunkRenderDist;
//    public List<Chunk> chunksList;
//    public int chunkSize, maxChunkHeight;
//    public int seed;
//    public float scale;
//    public int octaves;
//    public float persistence;
//    public float lacunarity;
//    public GameObject chunkPrefab;
//    public GameObject player;


//    private Vector3 lastPlayerPosition;
//    public static HashSet<Vector3> allBlockPositions;

//    public void Awake()
//    {
//        Instance = this;
//        chunksList = new List<Chunk>();
//        seed = Random.Range(-1000000, 1000000);
//        allBlockPositions = new HashSet<Vector3>();
//        lastPlayerPosition = player.transform.position;
//    }



//    void Start()
//    {
//        GenerateInitialChunks();
//    }

//    void Update()
//    {
//        if (Vector3.Distance(player.transform.position, lastPlayerPosition) > chunkSize)
//        {
//            lastPlayerPosition = player.transform.position;
//            CheckIfChunkIsWithinRenderDistance();
//            DisableChunksOutsideRenderDistance();
//        }
//    }
//    public void CheckIfChunkIsWithinRenderDistance()
//    {
//        int playerChunkX = Mathf.FloorToInt(player.transform.position.x / chunkSize);
//        int playerChunkZ = Mathf.FloorToInt(player.transform.position.z / chunkSize);
//        HashSet<Vector3> activeChunks = new HashSet<Vector3>(chunksList.Count);
//        foreach (var chunk in chunksList)
//        {
//            activeChunks.Add(chunk.transform.position);
//        }

//        for (int x = -chunkRenderDist; x <= chunkRenderDist; x++)
//        {
//            for (int z = -chunkRenderDist; z <= chunkRenderDist; z++)
//            {
//                Vector3 chunkPosition = new Vector3((playerChunkX + x) * chunkSize, 0, (playerChunkZ + z) * chunkSize);
//                if (!activeChunks.Contains(chunkPosition))
//                {
//                    GenerateChunk(chunkPosition);
//                }
//            }
//        }
//    }

//    public void GenerateChunk(Vector3 position)
//    {
//        Chunk chunk = Instantiate(chunkPrefab, position, Quaternion.identity).GetComponent<Chunk>();
//        chunk.seed = seed;
//        chunk.scale = scale;
//        chunk.octaves = octaves;
//        chunk.persistence = persistence;
//        chunk.chunkSize = chunkSize;
//        chunk.maxChunkHeight = maxChunkHeight;
//        chunk.lacunarity = lacunarity;
//        chunk.chunkGridPos = new Vector2Int(Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));
//        chunksList.Add(chunk);
//    }

//    private void GenerateInitialChunks()
//    {
//        for (int x = -chunkRenderDist; x <= chunkRenderDist; x++)
//        {
//            for (int z = -chunkRenderDist; z <= chunkRenderDist; z++)
//            {
//                Vector3 initialChunkPosition = new Vector3(x * chunkSize, 0, z * chunkSize);
//                GenerateChunk(initialChunkPosition);
//            }
//        }
//    }
//    private void DisableChunksOutsideRenderDistance()
//    {
//        int playerChunkX = Mathf.FloorToInt(player.transform.position.x / chunkSize);
//        int playerChunkZ = Mathf.FloorToInt(player.transform.position.z / chunkSize);

//        foreach (Chunk chunk in chunksList)
//        {
//            Vector3 chunkPosition = chunk.transform.position;
//            int chunkX = Mathf.FloorToInt(chunkPosition.x / chunkSize);
//            int chunkZ = Mathf.FloorToInt(chunkPosition.z / chunkSize);

//            if (Mathf.Abs(chunkX - playerChunkX) > chunkRenderDist || Mathf.Abs(chunkZ - playerChunkZ) > chunkRenderDist)
//            {
//                chunk.gameObject.SetActive(false);
//            }
//            else
//            {
//                chunk.gameObject.SetActive(true);
//            }
//        }
//    }
//}
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ChunkRetriever : MonoBehaviour
//{
//    public static ChunkRetriever Instance;



//    public Chunk


//    public int gridCollectionSize;
//    public Vector2Int[,] gridChunkCollections;

//    private void Awake()
//    {
//        Instance = this;



//        gridChunkCollections = new Vector2Int[gridCollectionSize, gridCollectionSize];

//        for (int x = 0; x < gridCollectionSize; x++)
//        {
//            for (int z = 0; z < gridCollectionSize; z++)
//            {
//                gridChunkCollections[x, z] = new Vector2Int(x, z);
//            }
//        }
//    }



//    public bool GetChunk(Vector2Int gridPos, out Chunk chunk)
//    {
//        chunk = null;



//        return false;
//    }

//    public bool IsInGrid(Vector2Int gridPos)
//    {
//        return gridPos.x >= 0 && gridPos.x < gridSizeX && gridPos.y >= 0 && gridPos.y < gridSizeZ;
//    }
//}
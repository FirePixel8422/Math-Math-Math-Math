using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;

    public int renderAmount;



    private void Start()
    {
        ChunkManager.Instance.Init();

        int chunkSize = ChunkManager.Instance.chunkSize;
        Vector3 centerOffset = new Vector3(renderAmount * 0.5f * chunkSize, 0, renderAmount * 0.5f * chunkSize);
        if (renderAmount % 2 != 0)
        {
            print("offset applied");
            centerOffset.x -= chunkSize * 0.5f;
            centerOffset.z -= chunkSize * 0.5f;
        }

        for (int x = 0; x < renderAmount; x++)
        {
            for (int z = 0; z < renderAmount; z++)
            {
                Instantiate(chunkPrefab, centerOffset - new Vector3(chunkSize * x, 0, chunkSize * z), Quaternion.identity);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;

    public int renderAmountHorizontal;
    public int renderAmountVertical;



    private void Start()
    {
        ChunkManager.Instance.Init();

        Vector3 centerOffset = new Vector3(renderAmountHorizontal * 0.5f * ChunkManager.Instance.chunkSize, 0, renderAmountVertical * 0.5f * ChunkManager.Instance.chunkSize);

        for (int x = 0; x < renderAmountHorizontal; x++)
        {
            for (int z = 0; z < renderAmountVertical; z++)
            {
                Instantiate(chunkPrefab, centerOffset - new Vector3(ChunkManager.Instance.chunkSize * x, 0, ChunkManager.Instance.chunkSize * z), Quaternion.identity);
            }
        }
    }
}

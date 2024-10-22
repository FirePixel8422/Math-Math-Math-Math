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

        int chunkSize = ChunkManager.Instance.chunkSize;
        Vector3 centerOffset = new Vector3(renderAmountHorizontal * 0.5f * chunkSize - chunkSize * 0.5f, 0, renderAmountVertical * 0.5f * chunkSize - chunkSize * 0.5f);

        for (int x = 0; x < renderAmountHorizontal; x++)
        {
            for (int z = 0; z < renderAmountVertical; z++)
            {
                Instantiate(chunkPrefab, centerOffset - new Vector3(chunkSize * x, 0, chunkSize * z), Quaternion.identity);
            }
        }
    }
}

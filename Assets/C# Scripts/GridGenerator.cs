using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public int width;
    public int lenght;
    public int layers;

    public float gridGenDelay;
    public GameObject chunkGO;
    public int chunkSize;
    public int chunkHeight;

    private List<Chunk> chunkList = new List<Chunk>();

    [ContextMenu("CallGenerateGrid")]
    public void CallGenerateGrid()
    {
        StartCoroutine(GenerateGrid());
    }
    public IEnumerator GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < lenght; z++)
            {
              Instantiate(chunkGO, new Vector3(x * chunkSize, 0, z * chunkSize), Quaternion.identity, gameObject.transform).GetComponent<Chunk>().Init(new byte());
            }
            yield return new WaitForSeconds(gridGenDelay);
        }
    }
}

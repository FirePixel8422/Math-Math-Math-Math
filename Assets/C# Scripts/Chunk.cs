using UnityEngine;

public class Chunk : MonoBehaviour
{
    public int chunkSize = 16;
    public GameObject blockPrefab;

    public Mesh mesh;


    private Vector3[] vertices;
    private int[] triangles;
    private int index;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateChunk();
    }

    void CreateChunk()
    {
        vertices = new Vector3[chunkSize * chunkSize * 4]; // Assuming 4 vertices per block face
        triangles = new int[chunkSize * chunkSize * 6]; // 2 triangles per block face

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                CreateBlock(x, z);
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void CreateBlock(int x, int z)
    {
        // Example for one face of a block (repeat for each face)
        vertices[index] = new Vector3(x, 0, z);
        // Define triangles here
        // Increase index as needed for vertices and triangles
        index++;
    }
}

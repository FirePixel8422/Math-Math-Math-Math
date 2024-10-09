using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[BurstCompile]
public class MeshCalculator : MonoBehaviour
{
    public static MeshCalculator Instance;
    private void Awake()
    {
        Instance = this;
    }




    public float cubeSize = 1;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;


    public int atlasSize;

    public int randomSpawnAmount;
    public Vector3Int spawnBounds;

    public bool drawBoundsGizmos;
    public bool drawGridGizmos;
    public Color gridGizmoColor;

    private Stopwatch stopwatch;

    public Vector3[] debugVerts;
    public int[] debugTris;

    public bool drawMeshVerticesGizmos;
    public bool drawMeshEdgesGizmos;





    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        stopwatch = new Stopwatch();

    }


    [ContextMenu("Create Random Mesh In Bounds")]
    public void DEBUG_CreateRandomMeshInBounds()
    {
        stopwatch.Start();
        CreateRandomMeshInBounds();
    }


    [BurstCompile]
    public void CreateRandomMeshInBounds()
    {
        NativeArray<float3> blockPositions = new NativeArray<float3>(randomSpawnAmount, Allocator.TempJob);

        if (randomSpawnAmount > 0)
        {
            NativeList<float3> possiblePositions = new NativeList<float3>(spawnBounds.x * spawnBounds.y * spawnBounds.z, Allocator.Temp);

            for (int x = 0; x < spawnBounds.x + 1; x++)
            {
                for (int y = 0; y < spawnBounds.y + 1; y++)
                {
                    for (int z = 0; z < spawnBounds.z + 1; z++)
                    {
                        possiblePositions.Add(new float3((x - spawnBounds.x * 0.5f) * cubeSize, (y - spawnBounds.y * 0.5f) * cubeSize, (z - spawnBounds.z * 0.5f) * cubeSize));
                    }
                }
            }


            int calculatedAmount = Mathf.Min(randomSpawnAmount, possiblePositions.Length);

            for (int i = 0; i < calculatedAmount; i++)
            {
                int r = UnityEngine.Random.Range(0, possiblePositions.Length);
                blockPositions[i] = possiblePositions[r];
                possiblePositions.RemoveAt(r);
            }


            possiblePositions.Dispose();
        }

#if UNITY_EDITOR
        DEBUG_CallTimer();
#endif

        MeshCalculatorJob.CallGenerateMeshJob(blockPositions, cubeSize);

        blockPositions.Dispose();
    }

    private void DEBUG_CallTimer()
    {
        print("Preperation Done In: " + stopwatch.ElapsedMilliseconds + "ms");
        stopwatch.Restart();
    }



    public void ApplyMeshToObject(NativeList<float3> vertices, NativeList<int> triangles, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs)
    {
        stopwatch.Restart();

        NativeArray<float2> uvs = new NativeArray<float2>(vertices.Length, Allocator.Persistent);
        TextureCalculator.ScheduleUVGeneration(uvs, vertices.Length, cubeFacesActiveState, textureIndexs, atlasSize);

        Vector2[] vectorUvs = new Vector2[vertices.Length];
        for (int i = 0; i < vectorUvs.Length; i++)
        {
            vectorUvs[i] = new Vector2(uvs[i].x, uvs[i].y);
        }

        print("Generating Texture UvMap Finished In " + stopwatch.ElapsedMilliseconds + "ms");
        stopwatch.Restart();



        // Create a new mesh and assign the vertices, triangles, and normals
        Mesh mesh = new Mesh();

        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }



        Vector3[] verticesVectors = new Vector3[vertices.Length];

        int loopIndex = 0;
        foreach (float3 vertex in vertices)
        {
            verticesVectors[loopIndex] = new Vector3(vertex.x, vertex.y, vertex.z);
            loopIndex += 1;
        }


        int[] trianglesVectors = new int[triangles.Length];

        loopIndex = 0;
        foreach (int triangle in triangles)
        {
            trianglesVectors[loopIndex] = triangle;
            loopIndex += 1;
        }


        debugVerts = verticesVectors;
        debugTris = trianglesVectors;

        mesh.vertices = verticesVectors;
        mesh.triangles = trianglesVectors;

        mesh.uv = vectorUvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        uvs.Dispose();

        stopwatch.Stop();

        print("Applying Mesh Finished In " + stopwatch.ElapsedMilliseconds + "ms, With " + triangles.Length / 3 + " Tris And " + vertices.Length + " Vertices.");
    }






    //public Vector3Int chunkPosition; // Position of the chunk in the chunk grid (chunk coordinates)

    //public List<Vector3Int> GetConnectedEdge(Chunk neighbor)
    //{
    //    List<Vector3Int> edgePositions = new List<Vector3Int>();

    //    // Determine which side is connected to the current chunk based on relative position
    //    if (neighbor.chunkPosition.x < this.chunkPosition.x)
    //    {
    //        // Neighbor is on the left (West), return right edge
    //        edgePositions = GetEdgePositionsRight();
    //    }
    //    else if (neighbor.chunkPosition.x > this.chunkPosition.x)
    //    {
    //        // Neighbor is on the right (East), return left edge
    //        edgePositions = GetEdgePositionsLeft();
    //    }
    //    else if (neighbor.chunkPosition.z > this.chunkPosition.z)
    //    {
    //        // Neighbor is in front (North), return back edge
    //        edgePositions = GetEdgePositionsBack();
    //    }
    //    else if (neighbor.chunkPosition.z < this.chunkPosition.z)
    //    {
    //        // Neighbor is behind (South), return front edge
    //        edgePositions = GetEdgePositionsFront();
    //    }

    //    return edgePositions;
    //}


    ////
    ////
    ////
    ////
    ////
    ////
    ////
    ////DELETE FLOAT
    ////
    ////
    //public Vector3Int chunkSize;
    ////
    ////
    ////DELETE FLOAT
    ////
    ////
    ////
    ////
    ////
    ////
    ////
    ////
    ////


    //// Get the right edge (x = 15) of this chunk
    //private List<Vector3Int> GetEdgePositionsRight()
    //{
    //    List<Vector3Int> edgePositions = new List<Vector3Int>();
    //    for (int y = 0; y < chunkSize.y; y++)
    //    {
    //        for (int z = 0; z < chunkSize.z; z++)
    //        {
    //            edgePositions.Add(new Vector3Int(chunkSize.x - 1, y, z));
    //        }
    //    }
    //    return edgePositions;
    //}

    //// Get the left edge (x = 0) of this chunk
    //private List<Vector3Int> GetEdgePositionsLeft()
    //{
    //    List<Vector3Int> edgePositions = new List<Vector3Int>();
    //    for (int y = 0; y < chunkSize.y; y++)
    //    {
    //        for (int z = 0; z < chunkSize.z; z++)
    //        {
    //            edgePositions.Add(new Vector3Int(0, y, z));
    //        }
    //    }
    //    return edgePositions;
    //}

    //// Get the back edge (z = 15) of this chunk
    //private List<Vector3Int> GetEdgePositionsBack()
    //{
    //    List<Vector3Int> edgePositions = new List<Vector3Int>();
    //    for (int y = 0; y < chunkSize.y; y++)
    //    {
    //        for (int x = 0; x < chunkSize.x; x++)
    //        {
    //            edgePositions.Add(new Vector3Int(x, y, chunkSize.z - 1));
    //        }
    //    }
    //    return edgePositions;
    //}

    //// Get the front edge (z = 0) of this chunk
    //private List<Vector3Int> GetEdgePositionsFront()
    //{
    //    List<Vector3Int> edgePositions = new List<Vector3Int>();
    //    for (int y = 0; y < chunkSize.y; y++)
    //    {
    //        for (int x = 0; x < chunkSize.x; x++)
    //        {
    //            edgePositions.Add(new Vector3Int(x, y, 0));
    //        }
    //    }
    //    return edgePositions;
    //}



    private void OnDrawGizmos()
    {
        if (spawnBounds.x != 0 && spawnBounds.y != 0 && spawnBounds.z != 0)
        {
            if (drawGridGizmos)
            {
                Gizmos.color = gridGizmoColor;

                for (int x = 0; x < spawnBounds.x + 1; x++)
                {
                    for (int y = 0; y < spawnBounds.y + 1; y++)
                    {
                        for (int z = 0; z < spawnBounds.z + 1; z++)
                        {
                            Gizmos.DrawWireCube(transform.position + new Vector3((x - spawnBounds.x * 0.5f) * cubeSize, (y - spawnBounds.y * 0.5f) * cubeSize, (z - spawnBounds.z * 0.5f) * cubeSize), Vector3.one * cubeSize);
                        }
                    }
                }
            }

            if (drawBoundsGizmos)
            {
                Gizmos.color = Color.white;

                Gizmos.DrawWireCube(transform.position, spawnBounds + cubeSize * Vector3.one);
            }

            Gizmos.color = Color.black;
            if (drawMeshVerticesGizmos)
            {
                foreach (Vector3 vertex in debugVerts)
                {
                    Gizmos.DrawCube(vertex, Vector3.one * cubeSize * .1f);
                }
            }

            if (drawMeshEdgesGizmos)
            {
                for (int i = 0; i < debugTris.Length; i += 3)
                {
                    Gizmos.DrawLine(debugVerts[debugTris[i]], debugVerts[debugTris[i + 1]]);
                    Gizmos.DrawLine(debugVerts[debugTris[i + 1]], debugVerts[debugTris[i + 2]]);
                    Gizmos.DrawLine(debugVerts[debugTris[i + 2]], debugVerts[debugTris[i]]);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && meshFilter != null && meshFilter.mesh != null)
        {
            meshFilter.mesh.vertices = debugVerts;
            meshFilter.mesh.triangles = debugTris;

            meshFilter.mesh.RecalculateNormals();
        }
    }
}

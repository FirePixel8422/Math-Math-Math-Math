using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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





    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        stopwatch = Stopwatch.StartNew();


        NativeList<float3> blockPositions = new NativeList<float3>(Allocator.Persistent);

        if (randomSpawnAmount > 0)
        {
            List<float3> possiblePositions = new List<float3>();

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


            int calculatedAmount = Mathf.Min(randomSpawnAmount, possiblePositions.Count);

            for (int i = 0; i < calculatedAmount; i++)
            {
                int r = UnityEngine.Random.Range(0, possiblePositions.Count);
                blockPositions.Add(possiblePositions[r]);

                possiblePositions.RemoveAt(r);
            }
        }

        MeshCalculatorJob.CallGenerateMeshJob(blockPositions);

        blockPositions.Dispose();
    }





    #region Performance ANTI Local Variables

    private float3 halfCubeSize;

    private Stopwatch stopwatch;
    #endregion


    [BurstCompile]
    public void CreateCombinedMesh(NativeList<float3> gridPositions)
    {
        if (gridPositions.Length == 0 || atlasSize == 0)
        {
            return;
        }



        NativeList<float3> newGridPositions = new NativeList<float3>(gridPositions.Length, Allocator.Temp);

        NativeArray<int> textureIndexs = new NativeArray<int>(gridPositions.Length, Allocator.Temp);


        NativeArray<BoolArray> activeCubeFacesTotalList = new NativeArray<BoolArray>(gridPositions.Length, Allocator.Temp);
        for (int i = 0; i < activeCubeFacesTotalList.Length; i++)
        {
            activeCubeFacesTotalList[i] = new BoolArray()
            {
                data = new NativeArray<byte>(6, Allocator.Temp),
            };
        }

        NativeArray<byte> activeCubeFaces = new NativeArray<byte>(6, Allocator.Temp);



        int atlasSizeSquared = atlasSize * atlasSize;

        for (int i = 0; i < gridPositions.Length; i++)
        {
            newGridPositions.Add(gridPositions[i]);

            //random Texture from Texture Atlas
            textureIndexs[i] = 0;
        }
        gridPositions = newGridPositions;






        halfCubeSize = 0.5f * cubeSize * Vector3.one;

        NativeArray<float3> faceVerticesOffsets = new NativeArray<float3>(24, Allocator.Temp);

        #region faceVerticesOffsets Data Setup

        faceVerticesOffsets[0] = new float3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[1] = new float3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[2] = new float3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[3] = new float3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[4] = new float3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[5] = new float3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[6] = new float3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[7] = new float3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[8] = new float3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[9] = new float3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[10] = new float3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[11] = new float3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[12] = new float3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[13] = new float3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[14] = new float3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[15] = new float3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[16] = new float3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[17] = new float3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[18] = new float3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[19] = new float3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[20] = new float3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[21] = new float3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z);
        faceVerticesOffsets[22] = new float3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        faceVerticesOffsets[23] = new float3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z);
        #endregion



        NativeArray<float3> gridNeighbourOffsets = new NativeArray<float3>(6, Allocator.Temp);

        gridNeighbourOffsets[0] = new float3(0, 0, cubeSize);     // Z+
        gridNeighbourOffsets[1] = new float3(0, 0, -cubeSize);    // Z-
        gridNeighbourOffsets[2] = new float3(-cubeSize, 0, 0);    // X-
        gridNeighbourOffsets[3] = new float3(cubeSize, 0, 0);     // X+
        gridNeighbourOffsets[4] = new float3(0, cubeSize, 0);     // Y+
        gridNeighbourOffsets[5] = new float3(0, -cubeSize, 0);    // Y-




        // 24 vertices per cube
        NativeList<float3> vertices = new NativeList<float3>(gridPositions.Length * 24, Allocator.Temp);
        NativeList<float3> sortedFaceVertices = new NativeList<float3>(24, Allocator.Temp);

        // 36 triangles per cube
        NativeList<int> triangles = new NativeList<int>(gridPositions.Length * 36, Allocator.Temp);
        NativeList<int> sortedFaceTriangles = new NativeList<int>(36, Allocator.Temp);



        int vertexOffset = 0;

        int frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

        for (int cubeIndex = 0; cubeIndex < gridPositions.Length; cubeIndex++)
        {
            float3 gridPosition = gridPositions[cubeIndex];

            // Define the 6 faces with separate vertices for flat shading
            float3[] faceVertices = new float3[]
            {
                // Back face (Z-)
                gridPosition + faceVerticesOffsets[0], // 0
                gridPosition + faceVerticesOffsets[1], // 1
                gridPosition + faceVerticesOffsets[2], // 2
                gridPosition + faceVerticesOffsets[3], // 3

                // Front face (Z+)
                gridPosition + faceVerticesOffsets[4],  // 4
                gridPosition + faceVerticesOffsets[5],  // 5
                gridPosition + faceVerticesOffsets[6],  // 6
                gridPosition + faceVerticesOffsets[7],  // 7

                // Right face (X+)
                gridPosition + faceVerticesOffsets[8],  // 8
                gridPosition + faceVerticesOffsets[9],  // 9
                gridPosition + faceVerticesOffsets[10], // 10
                gridPosition + faceVerticesOffsets[11], // 11

                // Left face (X-)
                gridPosition + faceVerticesOffsets[12], // 12
                gridPosition + faceVerticesOffsets[13], // 13
                gridPosition + faceVerticesOffsets[14], // 14
                gridPosition + faceVerticesOffsets[15], // 15

                // Top face (Y+)
                gridPosition + faceVerticesOffsets[16], // 16
                gridPosition + faceVerticesOffsets[17], // 17
                gridPosition + faceVerticesOffsets[18], // 18
                gridPosition + faceVerticesOffsets[19], // 19

                // Bottom face (Y-)
                gridPosition + faceVerticesOffsets[20], // 20
                gridPosition + faceVerticesOffsets[21], // 21
                gridPosition + faceVerticesOffsets[22], // 22
                gridPosition + faceVerticesOffsets[23]  // 23
            };



            Vector3 neighborPositionZPlus = gridPosition + gridNeighbourOffsets[0];
            Vector3 neighborPositionZMinus = gridPosition + gridNeighbourOffsets[1];
            Vector3 neighborPositionXMinus = gridPosition + gridNeighbourOffsets[2];
            Vector3 neighborPositionXPlus = gridPosition + gridNeighbourOffsets[3];
            Vector3 neighborPositionYPlus = gridPosition + gridNeighbourOffsets[4];
            Vector3 neighborPositionYMinus = gridPosition + gridNeighbourOffsets[5];

            // Check face visibility
            frontFaceVisible = !gridPositions.Contains(neighborPositionZPlus) ? 1 : 0;
            backFaceVisible = !gridPositions.Contains(neighborPositionZMinus) ? 1 : 0;
            leftFaceVisible = !gridPositions.Contains(neighborPositionXMinus) ? 1 : 0;
            rightFaceVisible = !gridPositions.Contains(neighborPositionXPlus) ? 1 : 0;
            topFaceVisible = !gridPositions.Contains(neighborPositionYPlus) ? 1 : 0;
            bottomFaceVisible = !gridPositions.Contains(neighborPositionYMinus) ? 1 : 0;


            sortedFaceVertices.Clear();
            sortedFaceTriangles.Clear();

            int faceMissedVertOffset = 0;



            #region Add face vertices and triangles if the face is visible

            if (backFaceVisible == 1)
            {
                activeCubeFaces[0] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 1 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset,
                    vertexOffset + 3 - faceMissedVertOffset, vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (frontFaceVisible == 1)
            {
                activeCubeFaces[1] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[4], faceVertices[5], faceVertices[6], faceVertices[7]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 5 - faceMissedVertOffset, vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset,
                    vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 7 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (rightFaceVisible == 1)
            {
                activeCubeFaces[2] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[8], faceVertices[9], faceVertices[10], faceVertices[11]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 9 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset,
                    vertexOffset + 11 - faceMissedVertOffset, vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (leftFaceVisible == 1)
            {
                activeCubeFaces[3] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[12], faceVertices[13], faceVertices[14], faceVertices[15]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 13 - faceMissedVertOffset, vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset,
                    vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 15 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (topFaceVisible == 1)
            {
                activeCubeFaces[4] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[16], faceVertices[17], faceVertices[18], faceVertices[19]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset, vertexOffset + 17 - faceMissedVertOffset,
                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 19 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (bottomFaceVisible == 1)
            {
                activeCubeFaces[5] = 1;

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.Temp)
                {
                    faceVertices[20], faceVertices[21], faceVertices[22], faceVertices[23]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.Temp)
                {
                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 21 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset,
                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset, vertexOffset + 23 - faceMissedVertOffset
                }.AsArray());
            }
            else
            {
                faceMissedVertOffset += 4;
            }
            #endregion

            activeCubeFacesTotalList[cubeIndex].data.CopyFrom(activeCubeFaces);


            vertices.AddRange(sortedFaceVertices.AsArray());
            triangles.AddRange(sortedFaceTriangles.AsArray());

            vertexOffset += 24 - faceMissedVertOffset;
        }


        ApplyMeshToObject(vertices, triangles, activeCubeFacesTotalList, textureIndexs);
    }


    private void ApplyMeshToObject(NativeList<float3> vertices, NativeList<int> triangles, NativeArray<BoolArray> activeFacesPerCube, NativeArray<int> textureIndexs)
    {
        // Create a new mesh and assign the vertices, triangles, and normals
        Mesh mesh = new Mesh();

        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }


        NativeList<Vector3> verticesVectors = new NativeList<Vector3>(vertices.Length, Allocator.Temp);
        for (int i = 0; i < vertices.Length; i++)
        {
            verticesVectors[i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
        }

        mesh.vertices = verticesVectors.ToArray();
        mesh.triangles = triangles.ToArray();


        NativeArray<Vector2> uvs = new NativeArray<Vector2>(vertices.Length, Allocator.TempJob);
        TextureCalculator.ScheduleUVGeneration(ref uvs, vertices.Length, activeFacesPerCube, textureIndexs, atlasSize);

        Vector2[] vectorUvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vectorUvs[i] = uvs[i];
        }

        mesh.uv = vectorUvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        stopwatch.Stop();

        print("Generated Mesh After " + stopwatch.ElapsedMilliseconds + "ms, With " + triangles.Length / 3 + " Tris And " + vertices.Length + " Vertices.");
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
        }
    }
}

[BurstCompile]
[NativeContainer]
public struct BoolArray
{
    public NativeArray<byte> data;
}

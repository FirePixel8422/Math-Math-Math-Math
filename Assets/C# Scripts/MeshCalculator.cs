using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MeshCalculator : MonoBehaviour
{
    public static MeshCalculator Instance;
    private void Awake()
    {
        Instance = this;
    }




    public List<Vector3> blockPositions;

    public float cubeSize = 1;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;


    public int atlasSize;

    public int randomSpawnAmount;
    public Vector3Int spawnBounds;

    public bool drawBoundsGizmos;
    public bool drawGridGizmos;
    public Color gridGizmoColor;




    private async void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        stopwatch = new Stopwatch();

        allBlockPositions = new HashSet<Vector3>();



        if (randomSpawnAmount > 0)
        {
            List<Vector3> possiblePositions = new List<Vector3>();

            for (int x = 0; x < spawnBounds.x + 1; x++)
            {
                for (int y = 0; y < spawnBounds.y + 1; y++)
                {
                    for (int z = 0; z < spawnBounds.z + 1; z++)
                    {
                        possiblePositions.Add(new Vector3((x - spawnBounds.x * 0.5f) * cubeSize, (y - spawnBounds.y * 0.5f) * cubeSize, (z - spawnBounds.z * 0.5f) * cubeSize));
                    }
                }
            }


            int calculatedAmount = Mathf.Min(randomSpawnAmount, possiblePositions.Count);
            blockPositions = new List<Vector3>();

            for (int i = 0; i < calculatedAmount; i++)
            {
                int r = Random.Range(0, possiblePositions.Count);
                blockPositions.Add(possiblePositions[r]);

                possiblePositions.RemoveAt(r);
            }
        }

        await CreateCombinedMesh(blockPositions);
    }





    #region Performance ANTI Local Variables

    private int[] textureIndexs;
    private List<bool[]> activeFacesPerCube;

    private List<Vector3> vertices;
    private List<int> triangles;

    private List<Vector3> sortedFaceVertices;
    private List<int> sortedFaceTriangles;

    private int[] faceTriangles;

    private Vector3 halfCubeSize;
    private Vector3[] faceVerticesOffset;


    private Stopwatch stopwatch;
    #endregion

    //position of every cube in current mesh
    public HashSet<Vector3> allBlockPositions;


    public async Task CreateCombinedMesh(List<Vector3> gridPositions)
    {
        if (gridPositions.Count == 0 || atlasSize == 0)
        {
            UnityEngine.Debug.LogWarning("Cant Add Mesh, No Positions Added To List Or Atlas Size is < 1");
            return;
        }



        stopwatch.Reset();
        stopwatch.Start();


        HashSet<Vector3> blockPositionsSet = new HashSet<Vector3>(allBlockPositions);

        List<Vector3> newGridPositions = new List<Vector3>(gridPositions.Count);

        textureIndexs = new int[gridPositions.Count];
        activeFacesPerCube = new List<bool[]>(gridPositions.Count);

        for (int i = 0; i < gridPositions.Count; i++)
        {
            if (blockPositionsSet.Contains(gridPositions[i]))
            {
                continue;
            }
            else
            {
                blockPositionsSet.Add(gridPositions[i]);
                newGridPositions.Add(gridPositions[i]);

                //random Texture from Texture Atlas
                textureIndexs[i] = Random.Range(0, atlasSize * atlasSize + 1);

                //6 faces per cube MAX
                activeFacesPerCube.Add(new bool[6]);
            }
        }
        gridPositions = newGridPositions;

        allBlockPositions = blockPositionsSet;




        halfCubeSize = 0.5f * Instance.cubeSize * Vector3.one;

        faceVerticesOffset = new Vector3[]{
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 0
            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 1
            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 2
            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 3
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),  // 4
            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 5
            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 6
            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 7
            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 8
            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 9
            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 10
            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 11
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 12
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),  // 13
            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 14
            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 15
            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 16
            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 17
            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 18
            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 19
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 20
            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 21
            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 22
            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z)   // 23
                };

        Vector3[] gridNeighbourOffsets = new Vector3[]
        {
            new Vector3(0, 0, cubeSize), // Z+
            new Vector3(0, 0, -cubeSize), // Z-
            new Vector3(-cubeSize, 0, 0), // X-
            new Vector3(cubeSize, 0, 0), // X+
            new Vector3(0, cubeSize, 0), // Y+
            new Vector3(0, -cubeSize, 0) // Y-
        };


        print("Pre-Calculated Values For Cube Including World And Texture Positions After: " + stopwatch.ElapsedMilliseconds + "ms");





        vertices = new List<Vector3>();
        triangles = new List<int>();


        int vertexOffset = 0;
        int cubeIndex = 0;

        bool frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

        for (int i = 0; i < gridPositions.Count; i++)
        {
            Vector3 gridPosition = gridPositions[i];

            // Define the 6 faces with separate vertices for flat shading
            Vector3[] faceVertices = new Vector3[]
            {
                // Back face (Z-)
                gridPosition + faceVerticesOffset[0], // 0
                gridPosition + faceVerticesOffset[1], // 1
                gridPosition + faceVerticesOffset[2], // 2
                gridPosition + faceVerticesOffset[3], // 3

                // Front face (Z+)
                gridPosition + faceVerticesOffset[4],  // 4
                gridPosition + faceVerticesOffset[5],  // 5
                gridPosition + faceVerticesOffset[6],  // 6
                gridPosition + faceVerticesOffset[7],  // 7

                // Right face (X+)
                gridPosition + faceVerticesOffset[8],  // 8
                gridPosition + faceVerticesOffset[9],  // 9
                gridPosition + faceVerticesOffset[10], // 10
                gridPosition + faceVerticesOffset[11], // 11

                // Left face (X-)
                gridPosition + faceVerticesOffset[12], // 12
                gridPosition + faceVerticesOffset[13], // 13
                gridPosition + faceVerticesOffset[14], // 14
                gridPosition + faceVerticesOffset[15], // 15

                // Top face (Y+)
                gridPosition + faceVerticesOffset[16], // 16
                gridPosition + faceVerticesOffset[17], // 17
                gridPosition + faceVerticesOffset[18], // 18
                gridPosition + faceVerticesOffset[19], // 19

                // Bottom face (Y-)
                gridPosition + faceVerticesOffset[20], // 20
                gridPosition + faceVerticesOffset[21], // 21
                gridPosition + faceVerticesOffset[22], // 22
                gridPosition + faceVerticesOffset[23]  // 23
            };



            // Check for visibility of each face before adding
            frontFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[0]); // Z+
            backFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[1]); // Z-
            leftFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[2]); // X-
            rightFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[3]); // X+
            topFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[4]); // Y+
            bottomFaceVisible = !allBlockPositions.Contains(gridPosition + gridNeighbourOffsets[5]); // Y-


            sortedFaceVertices = new List<Vector3>();
            sortedFaceTriangles = new List<int>();

            int faceMissedVertOffset = 0;


            // Add face vertices and triangles if the face is visible
            if (backFaceVisible)
            {
                activeFacesPerCube[cubeIndex][0] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
                faceTriangles = new int[] {
                    vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 1 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset,
                    vertexOffset + 3 - faceMissedVertOffset, vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (frontFaceVisible)
            {
                activeFacesPerCube[cubeIndex][1] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[4], faceVertices[5], faceVertices[6], faceVertices[7] });
                faceTriangles = new int[] {
                    vertexOffset + 5 - faceMissedVertOffset, vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset,
                    vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 7 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (rightFaceVisible)
            {
                activeFacesPerCube[cubeIndex][2] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[8], faceVertices[9], faceVertices[10], faceVertices[11] });
                faceTriangles = new int[] {
                    vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 9 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset,
                    vertexOffset + 11 - faceMissedVertOffset, vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (leftFaceVisible)
            {
                activeFacesPerCube[cubeIndex][3] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[12], faceVertices[13], faceVertices[14], faceVertices[15] });
                faceTriangles = new int[] {
                    vertexOffset + 13 - faceMissedVertOffset, vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset,
                    vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 15 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (topFaceVisible)
            {
                activeFacesPerCube[cubeIndex][4] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[16], faceVertices[17], faceVertices[18], faceVertices[19] });
                faceTriangles = new int[] {
                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset, vertexOffset + 17 - faceMissedVertOffset,
                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 19 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }

            if (bottomFaceVisible)
            {
                activeFacesPerCube[cubeIndex][5] = true;

                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[20], faceVertices[21], faceVertices[22], faceVertices[23] });
                faceTriangles = new int[] {
                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 21 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset,
                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset, vertexOffset + 23 - faceMissedVertOffset
                };
                sortedFaceTriangles.AddRange(faceTriangles);
            }
            else
            {
                faceMissedVertOffset += 4;
            }


            vertices.AddRange(sortedFaceVertices);
            triangles.AddRange(sortedFaceTriangles);

            vertexOffset += 24 - faceMissedVertOffset;


            //for loop
            cubeIndex += 1;
        }

        print("Calculated Vertexes And Tris After: " + stopwatch.ElapsedMilliseconds + "ms");





        // Create a new mesh and assign the vertices, triangles, and normals
        Mesh mesh = new Mesh();

        if (vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        await TextureCalculator.GenerateBoxMappingUVs(mesh, activeFacesPerCube, textureIndexs, atlasSize);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;


        stopwatch.Stop();
        print("Generated Mesh After " + stopwatch.ElapsedMilliseconds + "ms, With " + triangles.Count / 3 + " Tris And " + vertices.Count + " Vertices.");
    }



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



    [System.Serializable]
    public struct Block
    {
        public Vector3 position;
    }
}

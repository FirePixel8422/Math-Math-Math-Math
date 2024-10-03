
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using UnityEngine;
//using Unity.Burst;
//using UnityEngine.Jobs;
//using Unity.VisualScripting;
//[RequireComponent(typeof(MeshRenderer))]
//[RequireComponent(typeof(MeshFilter))]
//[RequireComponent(typeof(MeshCollider))]
//public class Chunk : MonoBehaviour
//{
//    public HashSet<Vector3> blockPositions = new HashSet<Vector3>();
//    public float cubeSize;

//    public MeshRenderer meshRenderer;
//    public MeshFilter meshFilter;
//    public MeshCollider meshCollider;

//    public bool autoUpdate;

//    public int atlasSize;

//    public int chunkSize, maxChunkHeight;
//    public int seed;
//    public float[,] noiseMap;

//    public float scale;
//    public int octaves;
//    public float persistence;
//    public float lacunarity;

//    public Vector2Int chunkGridPos;


//    private int adjustedX;
//    private int adjustedZ;
//    private int perlinValue;
//    private int maxY;


//    public async void Start()
//    {
//        noiseMap = NoiseMap.GenerateNoiseMap(chunkSize, chunkSize, seed, scale, octaves, persistence, lacunarity, new(transform.position.x, transform.position.z));

//        meshRenderer = GetComponent<MeshRenderer>();
//        meshFilter = GetComponent<MeshFilter>();
//        meshCollider = GetComponent<MeshCollider>();
//        stopwatch = new Stopwatch();
//        allBlockPositions = new HashSet<Vector3>();
//        var chunkpos = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
//        await GenerateBlockPos(chunkpos);

//        await CreateCombinedMesh(blockPositions.ToList());
//    }
//    public async Task GenerateBlockPos(Vector3Int chunkPosition)
//    {
//        await Task.Delay(Random.Range(1, 10));
//        stopwatch.Start();

//        for (int x = 0; x < chunkSize; x++)
//        {
//            for (int z = 0; z < chunkSize; z++)
//            {
//                // Get height from the noise map (assuming noiseMap is already normalized between 0 and 1)
//                int perlinValue = Mathf.FloorToInt(noiseMap[x, z] * maxChunkHeight);
//                int maxY = Mathf.Clamp(perlinValue, 0, maxChunkHeight);

//                // Add block positions up to the max height
//                for (int y = 0; y < maxY; y++)
//                {
//                    blockPositions.Add(new Vector3(x, y, z));
//                }
//            }
//        }

//        stopwatch.Stop();
//    }

//    private int CalculatePerlinNoiseHeight(int x, int z, Vector2Int resolution, float scale, int seed)
//    {
//        float noiseHeight = 0;
//        float amplitude = 1;
//        float frequency = 1;
//        float maxPossibleHeight = 0;

//        for (int i = 0; i < octaves; i++)
//        {
//            float xCoord = (x + seed) / (float)resolution.x * scale * frequency;
//            float zCoord = (z + seed) / (float)resolution.y * scale * frequency;

//            float sample = Mathf.PerlinNoise(xCoord, zCoord);

//            noiseHeight += sample * amplitude;
//            maxPossibleHeight += amplitude;

//            amplitude *= persistence;
//            frequency *= lacunarity;
//        }

//        noiseHeight = noiseHeight / maxPossibleHeight;
//        return Mathf.FloorToInt(noiseHeight * maxChunkHeight);
//    }

//    private void OnValidate()
//    {
//        if (autoUpdate)
//        {
//            //Invoke(nameof(CallSendMessage), 0.1f); // Delay for next frame
//        }
//    }



//    #region Performance ANTI Local Variables

//    private int[] textureIndexs;
//    private CubeFace[] activeCubeFaces;

//    private List<Vector3> vertices;
//    private List<int> triangles;

//    private List<Vector3> sortedFaceVertices;
//    private List<int> sortedFaceTriangles;

//    private int[] faceTriangles;

//    private Vector3 halfCubeSize;
//    private Vector3[] faceVerticesOffsets;
//    private Vector3[] gridNeighbourOffsets;


//    private Stopwatch stopwatch;
//    #endregion

//    //position of every cube in current mesh
//    public HashSet<Vector3> allBlockPositions;


//    public async Task CreateCombinedMesh(List<Vector3> gridPositions)
//    {
//        if (gridPositions.Count == 0 || atlasSize == 0)
//        {
//            UnityEngine.Debug.LogWarning("Cant Add Mesh, No Positions Added To List Or Atlas Size is < 1");
//            return;
//        }



//        stopwatch.Reset();
//        stopwatch.Start();


//        HashSet<Vector3> blockPositionsSet = new HashSet<Vector3>(allBlockPositions);

//        List<Vector3> newGridPositions = new List<Vector3>(gridPositions.Count);

//        textureIndexs = new int[gridPositions.Count];
//        activeCubeFaces = new CubeFace[gridPositions.Count];

//        int atlasSizeSquared = atlasSize * atlasSize;

//        for (int i = 0; i < gridPositions.Count; i++)
//        {
//            if (blockPositionsSet.Contains(gridPositions[i]))
//            {
//                continue;
//            }
//            else
//            {
//                blockPositionsSet.Add(gridPositions[i]);
//                newGridPositions.Add(gridPositions[i]);

//                //random Texture from Texture Atlas
//                textureIndexs[i] = UnityEngine.Random.Range(0, atlasSizeSquared + 1);

//                //6 faces per cube MAX
//                activeCubeFaces[i].activeFaces = new bool[6];
//            }
//        }
//        gridPositions = newGridPositions;

//        allBlockPositions.AddRange(blockPositionsSet);


//        //Chunk leftNeighbor = GameManager.Instance.GetChunk(new Vector2Int(chunkGridPos.x - 1, chunkGridPos.y));  // West
//        //Chunk rightNeighbor = chunkGrid(chunkGridPos.x + 1, chunkGridPos.y); // East
//        //Chunk frontNeighbor = chunkGrid(chunkGridPos.x, chunkGridPos.y + 1); // North
//        //Chunk backNeighbor = chunkGrid(chunkGridPos.x, chunkGridPos.y - 1);  // South

//        //List<Vector3Int> leftEdge = leftNeighbor.GetConnectedEdge(this);
//        //List<Vector3Int> rightEdge = rightNeighbor.GetConnectedEdge(this);
//        //List<Vector3Int> frontEdge = frontNeighbor.GetConnectedEdge(this);
//        //List<Vector3Int> backEdge = backNeighbor.GetConnectedEdge(this);






//        halfCubeSize = 0.5f * cubeSize * Vector3.one;

//        faceVerticesOffsets = new Vector3[]{
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 0
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 1
//            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 2
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 3
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),  // 4
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 5
//            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 6
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 7
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 8
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 9
//            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 10
//            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 11
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 12
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),  // 13
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 14
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 15
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),  // 16
//            new Vector3(halfCubeSize.x, halfCubeSize.y, -halfCubeSize.z),   // 17
//            new Vector3(halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),    // 18
//            new Vector3(-halfCubeSize.x, halfCubeSize.y, halfCubeSize.z),   // 19
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z), // 20
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, -halfCubeSize.z),  // 21
//            new Vector3(halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z),   // 22
//            new Vector3(-halfCubeSize.x, -halfCubeSize.y, halfCubeSize.z)   // 23
//                };

//        gridNeighbourOffsets = new Vector3[]
//        {
//            new Vector3(0, 0, cubeSize), // Z+
//            new Vector3(0, 0, -cubeSize), // Z-
//            new Vector3(-cubeSize, 0, 0), // X-
//            new Vector3(cubeSize, 0, 0), // X+
//            new Vector3(0, cubeSize, 0), // Y+
//            new Vector3(0, -cubeSize, 0) // Y-
//        };


//        print("Pre-Calculated Values For Cube Including World And Texture Positions After: " + stopwatch.ElapsedMilliseconds + "ms");





//        // 24 vertices per cube
//        vertices = new List<Vector3>(gridPositions.Count * 24);

//        // 36 triangles per cube
//        triangles = new List<int>(gridPositions.Count * 36);



//        int vertexOffset = 0;
//        int cubeIndex = 0;

//        bool frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

//        for (int i = 0; i < gridPositions.Count; i++)
//        {
//            Vector3 gridPosition = gridPositions[i];

//            // Define the 6 faces with separate vertices for flat shading
//            Vector3[] faceVertices = new Vector3[]
//            {
//                // Back face (Z-)
//                gridPosition + faceVerticesOffsets[0], // 0
//                gridPosition + faceVerticesOffsets[1], // 1
//                gridPosition + faceVerticesOffsets[2], // 2
//                gridPosition + faceVerticesOffsets[3], // 3

//                // Front face (Z+)
//                gridPosition + faceVerticesOffsets[4],  // 4
//                gridPosition + faceVerticesOffsets[5],  // 5
//                gridPosition + faceVerticesOffsets[6],  // 6
//                gridPosition + faceVerticesOffsets[7],  // 7

//                // Right face (X+)
//                gridPosition + faceVerticesOffsets[8],  // 8
//                gridPosition + faceVerticesOffsets[9],  // 9
//                gridPosition + faceVerticesOffsets[10], // 10
//                gridPosition + faceVerticesOffsets[11], // 11

//                // Left face (X-)
//                gridPosition + faceVerticesOffsets[12], // 12
//                gridPosition + faceVerticesOffsets[13], // 13
//                gridPosition + faceVerticesOffsets[14], // 14
//                gridPosition + faceVerticesOffsets[15], // 15

//                // Top face (Y+)
//                gridPosition + faceVerticesOffsets[16], // 16
//                gridPosition + faceVerticesOffsets[17], // 17
//                gridPosition + faceVerticesOffsets[18], // 18
//                gridPosition + faceVerticesOffsets[19], // 19

//                // Bottom face (Y-)
//                gridPosition + faceVerticesOffsets[20], // 20
//                gridPosition + faceVerticesOffsets[21], // 21
//                gridPosition + faceVerticesOffsets[22], // 22
//                gridPosition + faceVerticesOffsets[23]  // 23
//            };



//            Vector3 neighborPositionZPlus = gridPosition + gridNeighbourOffsets[0];
//            Vector3 neighborPositionZMinus = gridPosition + gridNeighbourOffsets[1];
//            Vector3 neighborPositionXMinus = gridPosition + gridNeighbourOffsets[2];
//            Vector3 neighborPositionXPlus = gridPosition + gridNeighbourOffsets[3];
//            Vector3 neighborPositionYPlus = gridPosition + gridNeighbourOffsets[4];
//            Vector3 neighborPositionYMinus = gridPosition + gridNeighbourOffsets[5];

//            // Check face visibility
//            frontFaceVisible = !allBlockPositions.Contains(neighborPositionZPlus);
//            backFaceVisible = !allBlockPositions.Contains(neighborPositionZMinus);
//            leftFaceVisible = !allBlockPositions.Contains(neighborPositionXMinus);
//            rightFaceVisible = !allBlockPositions.Contains(neighborPositionXPlus);
//            topFaceVisible = !allBlockPositions.Contains(neighborPositionYPlus);
//            bottomFaceVisible = !allBlockPositions.Contains(neighborPositionYMinus);


//            sortedFaceVertices = new List<Vector3>();
//            sortedFaceTriangles = new List<int>();

//            int faceMissedVertOffset = 0;


//            // Add face vertices and triangles if the face is visible
//            if (backFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[0] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
//                faceTriangles = new int[] {
//                    vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 1 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset,
//                    vertexOffset + 3 - faceMissedVertOffset, vertexOffset + 2 - faceMissedVertOffset, vertexOffset + 0 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }

//            if (frontFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[1] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[4], faceVertices[5], faceVertices[6], faceVertices[7] });
//                faceTriangles = new int[] {
//                    vertexOffset + 5 - faceMissedVertOffset, vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset,
//                    vertexOffset + 6 - faceMissedVertOffset, vertexOffset + 7 - faceMissedVertOffset, vertexOffset + 4 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }

//            if (rightFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[2] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[8], faceVertices[9], faceVertices[10], faceVertices[11] });
//                faceTriangles = new int[] {
//                    vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 9 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset,
//                    vertexOffset + 11 - faceMissedVertOffset, vertexOffset + 10 - faceMissedVertOffset, vertexOffset + 8 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }

//            if (leftFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[3] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[12], faceVertices[13], faceVertices[14], faceVertices[15] });
//                faceTriangles = new int[] {
//                    vertexOffset + 13 - faceMissedVertOffset, vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset,
//                    vertexOffset + 14 - faceMissedVertOffset, vertexOffset + 15 - faceMissedVertOffset, vertexOffset + 12 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }

//            if (topFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[4] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[16], faceVertices[17], faceVertices[18], faceVertices[19] });
//                faceTriangles = new int[] {
//                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset, vertexOffset + 17 - faceMissedVertOffset,
//                    vertexOffset + 16 - faceMissedVertOffset, vertexOffset + 19 - faceMissedVertOffset, vertexOffset + 18 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }

//            if (bottomFaceVisible)
//            {
//                activeCubeFaces[cubeIndex].activeFaces[5] = true;

//                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[20], faceVertices[21], faceVertices[22], faceVertices[23] });
//                faceTriangles = new int[] {
//                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 21 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset,
//                    vertexOffset + 20 - faceMissedVertOffset, vertexOffset + 22 - faceMissedVertOffset, vertexOffset + 23 - faceMissedVertOffset
//                };
//                sortedFaceTriangles.AddRange(faceTriangles);
//            }
//            else
//            {
//                faceMissedVertOffset += 4;
//            }


//            vertices.AddRange(sortedFaceVertices);
//            triangles.AddRange(sortedFaceTriangles);

//            vertexOffset += 24 - faceMissedVertOffset;


//            //for loop
//            cubeIndex += 1;
//        }

//        print("Calculated Vertexes And Tris After: " + stopwatch.ElapsedMilliseconds + "ms");







//        // Create a new mesh and assign the vertices, triangles, and normals
//        Mesh mesh = new Mesh();

//        if (vertices.Count > 65535)
//        {
//            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
//        }

//        mesh.vertices = vertices.ToArray();
//        mesh.triangles = triangles.ToArray();

//        await TextureCalculator.GenerateBoxMappingUVs(mesh, activeCubeFaces, textureIndexs, atlasSize);

//        mesh.RecalculateNormals();
//        //mesh.RecalculateBounds();

//        meshFilter.mesh = mesh;

//        stopwatch.Stop();
//        print("Generated Mesh After " + stopwatch.ElapsedMilliseconds + "ms, With " + triangles.Count / 3 + " Tris And " + vertices.Count + " Vertices.");
//    }







//    public Vector3Int chunkPosition; // Position of the chunk in the chunk grid (chunk coordinates)

//    public List<Vector3Int> GetConnectedEdge(Chunk neighbor)
//    {
//        List<Vector3Int> edgePositions = new List<Vector3Int>();

//        // Determine which side is connected to the current chunk based on relative position
//        if (neighbor.chunkPosition.x < this.chunkPosition.x)
//        {
//            // Neighbor is on the left (West), return right edge
//            edgePositions = GetEdgePositionsRight();
//        }
//        else if (neighbor.chunkPosition.x > this.chunkPosition.x)
//        {
//            // Neighbor is on the right (East), return left edge
//            edgePositions = GetEdgePositionsLeft();
//        }
//        else if (neighbor.chunkPosition.z > this.chunkPosition.z)
//        {
//            // Neighbor is in front (North), return back edge
//            edgePositions = GetEdgePositionsBack();
//        }
//        else if (neighbor.chunkPosition.z < this.chunkPosition.z)
//        {
//            // Neighbor is behind (South), return front edge
//            edgePositions = GetEdgePositionsFront();
//        }

//        return edgePositions;
//    }



//    // Get the right edge (x = 15) of this chunk
//    private List<Vector3Int> GetEdgePositionsRight()
//    {
//        List<Vector3Int> edgePositions = new List<Vector3Int>();
//        for (int y = 0; y < chunkSize; y++)
//        {
//            for (int z = 0; z < chunkSize; z++)
//            {
//                edgePositions.Add(new Vector3Int(chunkSize - 1, y, z));
//            }
//        }
//        return edgePositions;
//    }

//    // Get the left edge (x = 0) of this chunk
//    private List<Vector3Int> GetEdgePositionsLeft()
//    {
//        List<Vector3Int> edgePositions = new List<Vector3Int>();
//        for (int y = 0; y < chunkSize; y++)
//        {
//            for (int z = 0; z < chunkSize; z++)
//            {
//                edgePositions.Add(new Vector3Int(0, y, z));
//            }
//        }
//        return edgePositions;
//    }

//    // Get the back edge (z = 15) of this chunk
//    private List<Vector3Int> GetEdgePositionsBack()
//    {
//        List<Vector3Int> edgePositions = new List<Vector3Int>();
//        for (int y = 0; y < chunkSize; y++)
//        {
//            for (int x = 0; x < chunkSize; x++)
//            {
//                edgePositions.Add(new Vector3Int(x, y, chunkSize - 1));
//            }
//        }
//        return edgePositions;
//    }

//    // Get the front edge (z = 0) of this chunk
//    private List<Vector3Int> GetEdgePositionsFront()
//    {
//        List<Vector3Int> edgePositions = new List<Vector3Int>();
//        for (int y = 0; y < chunkSize; y++)
//        {
//            for (int x = 0; x < chunkSize; x++)
//            {
//                edgePositions.Add(new Vector3Int(x, y, 0));
//            }
//        }
//        return edgePositions;
//    }
//    [System.Serializable]
//    public struct Block
//    {
//        public Vector3 position;
//    }
//    public struct CubeFace
//    {
//        public bool[] activeFaces;
//    }
//}
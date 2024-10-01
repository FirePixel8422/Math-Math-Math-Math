using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteAlways]
public class MeshCalculator : MonoBehaviour
{
    public List<Vector3> blockPositions;
    public Vector3 cubeSize = new Vector3(1, 1, 1);
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public bool autoUpdate;

    private void Start()
    {
        CreateCombinedMesh(blockPositions);
        Debug.Log("Mesh generated.");
    }

    private void OnValidate()
    {
        if (autoUpdate)
        {
            Invoke(nameof(CallSendMessage), 0.1f); // Delay for next frame
        }
    }

    private void CallSendMessage()
    {
        CreateCombinedMesh(blockPositions);
        Debug.Log("Mesh updated.");
    }

    public void CreateCombinedMesh(List<Vector3> gridPositions)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0; // To track the offset of the vertices for triangle indices

        foreach (Vector3 gridPosition in gridPositions)
        {
            // Calculate half size of the cube
            Vector3 halfSize = cubeSize * 0.5f;

            // Define the 6 faces with separate vertices for flat shading
            Vector3[] faceVertices = new Vector3[]
            {
                // Back face (Z-)
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), // 0
                gridPosition + new Vector3( halfSize.x, -halfSize.y, -halfSize.z), // 1
                gridPosition + new Vector3( halfSize.x,  halfSize.y, -halfSize.z), // 2
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z), // 3

                // Front face (Z+)
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),  // 4
                gridPosition + new Vector3( halfSize.x, -halfSize.y, halfSize.z),   // 5
                gridPosition + new Vector3( halfSize.x,  halfSize.y, halfSize.z),   // 6
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, halfSize.z),   // 7

                // Right face (X+)
                gridPosition + new Vector3( halfSize.x, -halfSize.y, -halfSize.z),  // 8
                gridPosition + new Vector3( halfSize.x, -halfSize.y, halfSize.z),   // 9
                gridPosition + new Vector3( halfSize.x,  halfSize.y, halfSize.z),   // 10
                gridPosition + new Vector3( halfSize.x,  halfSize.y, -halfSize.z),  // 11

                // Left face (X-)
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),  // 12
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),   // 13
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, halfSize.z),   // 14
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),  // 15

                // Top face (Y+)
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),   // 16
                gridPosition + new Vector3( halfSize.x,  halfSize.y, -halfSize.z),    // 17
                gridPosition + new Vector3( halfSize.x,  halfSize.y, halfSize.z),     // 18
                gridPosition + new Vector3(-halfSize.x,  halfSize.y, halfSize.z),     // 19

                // Bottom face (Y-)
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),   // 20
                gridPosition + new Vector3( halfSize.x, -halfSize.y, -halfSize.z),    // 21
                gridPosition + new Vector3( halfSize.x, -halfSize.y, halfSize.z),     // 22
                gridPosition + new Vector3(-halfSize.x, -halfSize.y, halfSize.z)      // 23
            };


            // Check for visibility of each face before adding
            bool frontFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(0, 0, 1)); // Z+
            bool backFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(0, 0, -1)); // Z-
            bool leftFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(-1, 0, 0)); // X-
            bool rightFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(1, 0, 0)); // X+
            bool topFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(0, 1, 0)); // Y+
            bool bottomFaceVisible = !gridPositions.Contains(gridPosition + new Vector3(0, -1, 0)); // Y-



            List<Vector3> sortedFaceVertices = new List<Vector3>();
            List<int> sortedFaceTriangles = new List<int>();

            int faceMissedVertOffset = 0;


            // Add face vertices and triangles if the face is visible
            if (backFaceVisible)
            {
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
                int[] faceTriangles = new int[] {
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
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[4], faceVertices[5], faceVertices[6], faceVertices[7] });
                int[] faceTriangles = new int[] {
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
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[8], faceVertices[9], faceVertices[10], faceVertices[11] });
                int[] faceTriangles = new int[] {
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
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[12], faceVertices[13], faceVertices[14], faceVertices[15] });
                int[] faceTriangles = new int[] {
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
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[16], faceVertices[17], faceVertices[18], faceVertices[19] });
                int[] faceTriangles = new int[] {
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
                sortedFaceVertices.AddRange(new Vector3[] { faceVertices[20], faceVertices[21], faceVertices[22], faceVertices[23] });
                int[] faceTriangles = new int[] {
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
        }

        print(triangles.Count / 6 + " Planes");

        // Create a new mesh and assign the vertices, triangles, and normals
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();

        // Assign the created mesh to the mesh filter
        meshFilter.mesh = mesh;
    }



    [System.Serializable]
    public struct Block
    {
        public Vector3 position;
    }
}

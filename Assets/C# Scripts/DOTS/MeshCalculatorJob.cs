using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public class MeshCalculatorJob : MonoBehaviour
{
    public int debugMathComplexity;

    public int jobGroupAmount;
    public int jobTotalAmount;

    public int batchSize;

    public int completions;
    public float elapsed;

    public float timeCheck;

    public bool paused;



    public static void CallGenerateMeshJob(NativeList<float3> blockPositions)
    {
        GenerateMeshJob job = new GenerateMeshJob
        {
            gridPositions = blockPositions,
            vertices = new NativeList<float3>(Allocator.TempJob),
            triangles = new NativeList<int>(Allocator.TempJob),
        };

        // Schedule the job
        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();
    }
}

[BurstCompile]
public struct GenerateMeshJob : IJob
{
    public NativeList<float3> gridPositions;
    public NativeList<float3> vertices;
    public NativeList<int> triangles;

    public float cubeSize;

    public int atlasSize;

    public float3 halfCubeSize;



    [BurstCompile]
    public void Execute()
    {
        NativeList<float3> newGridPositions = new NativeList<float3>(gridPositions.Length, Allocator.TempJob);

        NativeArray<int> textureIndexs = new NativeArray<int>(gridPositions.Length, Allocator.TempJob);


        NativeArray<BoolArray> activeCubeFacesTotalList = new NativeArray<BoolArray>(gridPositions.Length, Allocator.TempJob);
        for (int i = 0; i < activeCubeFacesTotalList.Length; i++)
        {
            activeCubeFacesTotalList[i] = new BoolArray()
            {
                data = new NativeArray<byte>(6, Allocator.TempJob),
            };
        }

        NativeArray<byte> activeCubeFaces = new NativeArray<byte>(6, Allocator.TempJob);



        int atlasSizeSquared = atlasSize * atlasSize;

        for (int i = 0; i < gridPositions.Length; i++)
        {
            newGridPositions.Add(gridPositions[i]);

            //random Texture from Texture Atlas
            textureIndexs[i] = 0;
        }
        gridPositions = newGridPositions;






        halfCubeSize = 0.5f * cubeSize * Vector3.one;

        NativeArray<float3> faceVerticesOffsets = new NativeArray<float3>(24, Allocator.TempJob);

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



        NativeArray<float3> gridNeighbourOffsets = new NativeArray<float3>(6, Allocator.TempJob);

        gridNeighbourOffsets[0] = new float3(0, 0, cubeSize);     // Z+
        gridNeighbourOffsets[1] = new float3(0, 0, -cubeSize);    // Z-
        gridNeighbourOffsets[2] = new float3(-cubeSize, 0, 0);    // X-
        gridNeighbourOffsets[3] = new float3(cubeSize, 0, 0);     // X+
        gridNeighbourOffsets[4] = new float3(0, cubeSize, 0);     // Y+
        gridNeighbourOffsets[5] = new float3(0, -cubeSize, 0);    // Y-




        // 24 vertices per cube
        NativeList<float3> vertices = new NativeList<float3>(gridPositions.Length * 24, Allocator.TempJob);
        NativeList<float3> sortedFaceVertices = new NativeList<float3>(24, Allocator.TempJob);

        // 36 triangles per cube
        NativeList<int> triangles = new NativeList<int>(gridPositions.Length * 36, Allocator.TempJob);
        NativeList<int> sortedFaceTriangles = new NativeList<int>(36, Allocator.TempJob);



        int vertexOffset = 0;

        int frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

        for (int cubeIndex = 0; cubeIndex < gridPositions.Length; cubeIndex++)
        {
            float3 gridPosition = gridPositions[cubeIndex];
            

            // Define the 6 faces with separate vertices for flat shading
            NativeArray<float3> faceVertices = new NativeArray<float3>(24, Allocator.TempJob);
            for (int i = 0; i < 6; i++) // 6 faces
            {
                // Calculate the index offsets for each face's vertices
                int baseIndex = i * 4; // 4 vertices per face
                faceVertices[baseIndex + 0] = gridPosition + faceVerticesOffsets[baseIndex + 0];
                faceVertices[baseIndex + 1] = gridPosition + faceVerticesOffsets[baseIndex + 1];
                faceVertices[baseIndex + 2] = gridPosition + faceVerticesOffsets[baseIndex + 2];
                faceVertices[baseIndex + 3] = gridPosition + faceVerticesOffsets[baseIndex + 3];
            }



            float3 neighborPositionZPlus = gridPosition + gridNeighbourOffsets[0];
            float3 neighborPositionZMinus = gridPosition + gridNeighbourOffsets[1];
            float3 neighborPositionXMinus = gridPosition + gridNeighbourOffsets[2];
            float3 neighborPositionXPlus = gridPosition + gridNeighbourOffsets[3];
            float3 neighborPositionYPlus = gridPosition + gridNeighbourOffsets[4];
            float3 neighborPositionYMinus = gridPosition + gridNeighbourOffsets[5];

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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[4], faceVertices[5], faceVertices[6], faceVertices[7]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[8], faceVertices[9], faceVertices[10], faceVertices[11]
                }.AsArray());


                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[12], faceVertices[13], faceVertices[14], faceVertices[15]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[16], faceVertices[17], faceVertices[18], faceVertices[19]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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

                sortedFaceVertices.AddRange(new NativeList<float3>(Allocator.TempJob)
                {
                    faceVertices[20], faceVertices[21], faceVertices[22], faceVertices[23]
                }.AsArray());

                sortedFaceTriangles.AddRange(new NativeList<int>(Allocator.TempJob)
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
    }
}

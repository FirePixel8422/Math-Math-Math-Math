using System;
using System.Collections;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public struct MeshCalculatorJob
{
    public static void CallGenerateMeshJob(NativeArray<int3> blockPositions, int atlasSize, Mesh mesh, MeshCollider coll)
    {
        #region Main Data NativeContainers

        JobHandle jobHandle;


        int blockPositionsLength = blockPositions.Length;

        NativeHashMap<int3, bool> blockPositionsMap = new NativeHashMap<int3, bool>(blockPositionsLength, Allocator.TempJob);


        NativeArray<float3> vertices = new NativeArray<float3>(blockPositionsLength * 24, Allocator.TempJob);

        NativeArray<int> triangles = new NativeArray<int>(blockPositionsLength * 36, Allocator.TempJob);


        NativeArray<int> textureIndexs = new NativeArray<int>(blockPositionsLength, Allocator.TempJob);


        NativeArray<byte> cubeFacesActiveState = new NativeArray<byte>(blockPositionsLength * 6, Allocator.TempJob);

        NativeArray<float3> faceVerticesOffsets = new NativeArray<float3>(24, Allocator.TempJob);

        float3 halfCubeSize = 0.5f * Vector3.one;

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


        NativeArray<float3> faceVertices = new NativeArray<float3>(24, Allocator.TempJob);

        for (int i = 0; i < 6; i++) // 6 faces
        {
            // Calculate the index offsets for each face's vertices
            int baseIndex = i * 4; // 4 vertices per face
            faceVertices[baseIndex + 0] = faceVerticesOffsets[baseIndex + 0];
            faceVertices[baseIndex + 1] = faceVerticesOffsets[baseIndex + 1];
            faceVertices[baseIndex + 2] = faceVerticesOffsets[baseIndex + 2];
            faceVertices[baseIndex + 3] = faceVerticesOffsets[baseIndex + 3];
        }

        #endregion




        #region SetupData Job

        SetupDataJobParallel setupDataJobParallel = new SetupDataJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,
            triangles = triangles,
        };

        jobHandle = setupDataJobParallel.Schedule(blockPositionsLength, blockPositionsLength);
        jobHandle.Complete();

        #endregion




        #region GenerateMeshCubes Job

        GenerateMeshCubesJobParallel generateMeshCubesJob = new GenerateMeshCubesJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,

            vertices = vertices,
            faceVertices = faceVertices,

            triangles = triangles,

            cubeFacesActiveState = cubeFacesActiveState,
        };

        jobHandle = generateMeshCubesJob.Schedule(blockPositionsLength, 4096);
        jobHandle.Complete();

        #endregion


        #region Dispose All NativeContainers That Are Done Being Used

        faceVertices.Dispose();

        blockPositions.Dispose();
        blockPositionsMap.Dispose();

        #endregion




        #region FinalizeMesh Job

        NativeList<float3> filteredVertices = new NativeList<float3>(vertices.Length, Allocator.TempJob);

        NativeList<int> filteredTriangles = new NativeList<int>(triangles.Length, Allocator.TempJob);

        FinalizeMeshMathJob finalizeMeshMathJob = new FinalizeMeshMathJob
        {
            vertices = vertices,
            triangles = triangles,
            filteredVertices = filteredVertices,
            filteredTriangles = filteredTriangles,
        };

        jobHandle = finalizeMeshMathJob.Schedule();
        jobHandle.Complete();

        #endregion


        #region Dispose All NativeContainers That Are Done Being Used

        vertices.Dispose();

        triangles.Dispose();

        #endregion




        ApplyMeshToObject(filteredVertices, filteredTriangles, cubeFacesActiveState, textureIndexs, atlasSize, mesh, coll);


        #region Dispose All Remaining Native Containers That Are Done Being Used After ApplyMeshToObject

        textureIndexs.Dispose();

        cubeFacesActiveState.Dispose();

        filteredVertices.Dispose();

        filteredTriangles.Dispose();

        #endregion

    }


    public static void ApplyMeshToObject(NativeList<float3> vertices, NativeList<int> triangles, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs, int atlasSize, Mesh mesh, MeshCollider coll)
    {
        NativeArray<float2> uvs = new NativeArray<float2>(vertices.Length, Allocator.Persistent);
        TextureCalculator.ScheduleUVGeneration(uvs, vertices.Length, cubeFacesActiveState, textureIndexs, atlasSize);



        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        Vector3[] verticesVectors = new Vector3[vertices.Length];

        for (int i = 0; i < verticesVectors.Length; i++)
        {
            verticesVectors[i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
        }

        mesh.vertices = vertices.AsArray().Reinterpret<Vector3>().ToArray();
        mesh.triangles = triangles.AsArray().ToArray();

        mesh.uv = uvs.Reinterpret<Vector2>().ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        coll.sharedMesh = mesh;

        uvs.Dispose();
    }





    [BurstCompile]
    public struct SetupDataJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeHashMap<int3, bool> blockPositionsMap;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int> triangles;

        public void Execute(int index)
        {
            int3 gridPosition = blockPositions[index];
            blockPositionsMap.TryAdd(gridPosition, false);

            for (int i = 0; i < 36; i++)
            {
                triangles[index * 36 + i] = -1;
            }
        }
    }




    [BurstCompile]
    public struct GenerateMeshCubesJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NoAlias][ReadOnly] public NativeHashMap<int3, bool> blockPositionsMap;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float3> vertices;

        [NoAlias][ReadOnly] public NativeArray<float3> faceVertices;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int> triangles;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<byte> cubeFacesActiveState;

        readonly static int3[] neighborOffsets = new int3[]
        {
        new int3(0, 0, 1),     // Z+
        new int3(0, 0, -1),    // Z-
        new int3(-1, 0, 0),    // X-
        new int3(1, 0, 0),     // X+
        new int3(0, 1, 0),     // Y+
        new int3(0, -1, 0)     // Y-
        };



        [BurstCompile]
        public void Execute(int cubeIndex)
        {
            byte frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

            int3 cubePosition = blockPositions[cubeIndex];


            int3 neighborPositionZPlus = cubePosition + neighborOffsets[0];      // Z+
            int3 neighborPositionZMinus = cubePosition + neighborOffsets[1];    // Z-
            int3 neighborPositionXMinus = cubePosition + neighborOffsets[2];    // X-
            int3 neighborPositionXPlus = cubePosition + neighborOffsets[3];      // X+
            int3 neighborPositionYPlus = cubePosition + neighborOffsets[4];      // Y+
            int3 neighborPositionYMinus = cubePosition + neighborOffsets[5];    // Y-



            // Use the NativeHashMap to check neighbor visibility
            frontFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionZPlus) ? 0 : 1);
            backFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionZMinus) ? 0 : 1);
            leftFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionXMinus) ? 0 : 1);
            rightFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionXPlus) ? 0 : 1);
            topFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionYPlus) ? 0 : 1);
            bottomFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionYMinus) ? 0 : 1);


            if (frontFaceVisible == 0 && backFaceVisible == 0 && leftFaceVisible == 0 && rightFaceVisible == 0 && bottomFaceVisible == 0 && topFaceVisible == 0) 
            {
                //if there will be no faces for this cube
                return; // Skip this cube entirely
            }




            int cCubeActiveStateIndex = cubeIndex * 6;
            int cVerticeIndex = cubeIndex * 24;
            int cTriangleIndex = cubeIndex * 36;


            #region Add face vertices and triangles if the face is visible

            if (backFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 0] = 1;

                vertices[cVerticeIndex + 0] = faceVertices[0] + cubePosition;
                vertices[cVerticeIndex + 1] = faceVertices[1] + cubePosition;
                vertices[cVerticeIndex + 2] = faceVertices[2] + cubePosition;
                vertices[cVerticeIndex + 3] = faceVertices[3] + cubePosition;

                triangles[cTriangleIndex + 0] = cVerticeIndex + 2;
                triangles[cTriangleIndex + 1] = cVerticeIndex + 1;
                triangles[cTriangleIndex + 2] = cVerticeIndex + 0;
                triangles[cTriangleIndex + 3] = cVerticeIndex + 3;
                triangles[cTriangleIndex + 4] = cVerticeIndex + 2;
                triangles[cTriangleIndex + 5] = cVerticeIndex + 0;
            }

            if (frontFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 1] = 1;

                vertices[cVerticeIndex + 4] = faceVertices[4] + cubePosition;
                vertices[cVerticeIndex + 5] = faceVertices[5] + cubePosition;
                vertices[cVerticeIndex + 6] = faceVertices[6] + cubePosition;
                vertices[cVerticeIndex + 7] = faceVertices[7] + cubePosition;

                triangles[cTriangleIndex + 6] = cVerticeIndex + 5;
                triangles[cTriangleIndex + 7] = cVerticeIndex + 6;
                triangles[cTriangleIndex + 8] = cVerticeIndex + 4;
                triangles[cTriangleIndex + 9] = cVerticeIndex + 6;
                triangles[cTriangleIndex + 10] = cVerticeIndex + 7;
                triangles[cTriangleIndex + 11] = cVerticeIndex + 4;
            }

            if (rightFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 2] = 1;

                vertices[cVerticeIndex + 8] = faceVertices[8] + cubePosition;
                vertices[cVerticeIndex + 9] = faceVertices[9] + cubePosition;
                vertices[cVerticeIndex + 10] = faceVertices[10] + cubePosition;
                vertices[cVerticeIndex + 11] = faceVertices[11] + cubePosition;

                triangles[cTriangleIndex + 12] = cVerticeIndex + 10;
                triangles[cTriangleIndex + 13] = cVerticeIndex + 9;
                triangles[cTriangleIndex + 14] = cVerticeIndex + 8;
                triangles[cTriangleIndex + 15] = cVerticeIndex + 11;
                triangles[cTriangleIndex + 16] = cVerticeIndex + 10;
                triangles[cTriangleIndex + 17] = cVerticeIndex + 8;
            }

            if (leftFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 3] = 1;

                vertices[cVerticeIndex + 12] = faceVertices[12] + cubePosition;
                vertices[cVerticeIndex + 13] = faceVertices[13] + cubePosition;
                vertices[cVerticeIndex + 14] = faceVertices[14] + cubePosition;
                vertices[cVerticeIndex + 15] = faceVertices[15] + cubePosition;

                triangles[cTriangleIndex + 18] = cVerticeIndex + 13;
                triangles[cTriangleIndex + 19] = cVerticeIndex + 14;
                triangles[cTriangleIndex + 20] = cVerticeIndex + 12;
                triangles[cTriangleIndex + 21] = cVerticeIndex + 14;
                triangles[cTriangleIndex + 22] = cVerticeIndex + 15;
                triangles[cTriangleIndex + 23] = cVerticeIndex + 12;
            }

            if (topFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 4] = 1;

                vertices[cVerticeIndex + 16] = faceVertices[16] + cubePosition;
                vertices[cVerticeIndex + 17] = faceVertices[17] + cubePosition;
                vertices[cVerticeIndex + 18] = faceVertices[18] + cubePosition;
                vertices[cVerticeIndex + 19] = faceVertices[19] + cubePosition;

                triangles[cTriangleIndex + 24] = cVerticeIndex + 16;
                triangles[cTriangleIndex + 25] = cVerticeIndex + 18;
                triangles[cTriangleIndex + 26] = cVerticeIndex + 17;
                triangles[cTriangleIndex + 27] = cVerticeIndex + 16;
                triangles[cTriangleIndex + 28] = cVerticeIndex + 19;
                triangles[cTriangleIndex + 29] = cVerticeIndex + 18;
            }

            if (bottomFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 5] = 1;

                vertices[cVerticeIndex + 20] = faceVertices[20] + cubePosition;
                vertices[cVerticeIndex + 21] = faceVertices[21] + cubePosition;
                vertices[cVerticeIndex + 22] = faceVertices[22] + cubePosition;
                vertices[cVerticeIndex + 23] = faceVertices[23] + cubePosition;

                triangles[cTriangleIndex + 30] = cVerticeIndex + 20;
                triangles[cTriangleIndex + 31] = cVerticeIndex + 21;
                triangles[cTriangleIndex + 32] = cVerticeIndex + 22;
                triangles[cTriangleIndex + 33] = cVerticeIndex + 20;
                triangles[cTriangleIndex + 34] = cVerticeIndex + 22;
                triangles[cTriangleIndex + 35] = cVerticeIndex + 23;
            }
            #endregion
        }
    }




    [BurstCompile]
    public struct FinalizeMeshMathJob : IJob
    {
        [NoAlias][ReadOnly] public NativeArray<float3> vertices;
        [NoAlias][ReadOnly] public NativeArray<int> triangles;

        [NoAlias][WriteOnly] public NativeList<float3> filteredVertices;
        [NoAlias][WriteOnly] public NativeList<int> filteredTriangles;

        public void Execute()
        {
            // Counter for tracking the current position in the filtered vertices
            int currentVertexIndex = 0;
            int skippedVerticesCount = 0;

            int triangleCount = triangles.Length;

            // Iterate through the triangles in steps of 6 (since it's quad-based with 6 triangle indices)
            for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex += 6)
            {
                // Check the first triangle of the quad, if it's -1, skip this entire quad
                if (triangles[triangleIndex] == -1)
                {
                    // Skip the corresponding 4 vertices as well
                    currentVertexIndex += 4;
                    skippedVerticesCount += 4;
                    continue; // Skip this quad
                }

                // Add the 4 vertices corresponding to the current quad
                filteredVertices.Add(vertices[currentVertexIndex + 0]);
                filteredVertices.Add(vertices[currentVertexIndex + 1]);
                filteredVertices.Add(vertices[currentVertexIndex + 2]);
                filteredVertices.Add(vertices[currentVertexIndex + 3]);

                // Add the 6 triangles, adjusting indices relative to the new vertex list
                filteredTriangles.Add(triangles[triangleIndex + 0] - skippedVerticesCount);
                filteredTriangles.Add(triangles[triangleIndex + 1] - skippedVerticesCount);
                filteredTriangles.Add(triangles[triangleIndex + 2] - skippedVerticesCount);
                filteredTriangles.Add(triangles[triangleIndex + 3] - skippedVerticesCount);
                filteredTriangles.Add(triangles[triangleIndex + 4] - skippedVerticesCount);
                filteredTriangles.Add(triangles[triangleIndex + 5] - skippedVerticesCount);

                // After processing a valid quad, move to the next set of 4 vertices
                currentVertexIndex += 4;
            }
        }
    }
}
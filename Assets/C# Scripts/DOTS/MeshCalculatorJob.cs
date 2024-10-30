using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[BurstCompile]
public struct MeshCalculatorJob
{
    public static void CallGenerateMeshJob(int3 chunkGridPos, NativeArray<int3> blockPositions, int atlasSize, Mesh mesh, MeshCollider coll)
    {
        JobHandle mainJobHandle;

        int blockPositionsLength = blockPositions.Length;

        NativeHashMap<int3, bool> blockPositionsMap = new NativeHashMap<int3, bool>(blockPositionsLength, Allocator.TempJob);




        #region SetupData Job

        SetupDataJobParallel setupDataJobParallel = new SetupDataJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,
        };

        mainJobHandle = setupDataJobParallel.Schedule(blockPositionsLength, blockPositionsLength);

        #endregion




        #region Calculate ConnectedChunks Edge Positions Job

        NativeArray<int3> connectedChunkEdgePositions = ChunkManager.GetConnectedChunkEdgePositionsCount(chunkGridPos);

        CalculateChunkConnectionsJobParallel calculateChunkConnectionsJobParallel = new CalculateChunkConnectionsJobParallel
        {
            blockPositions = connectedChunkEdgePositions,
            blockPositionsMap = blockPositionsMap,
        };

        mainJobHandle = JobHandle.CombineDependencies(mainJobHandle, calculateChunkConnectionsJobParallel.Schedule(connectedChunkEdgePositions.Length, connectedChunkEdgePositions.Length, mainJobHandle));

        #endregion




        #region GenerateMeshCubes Job

        NativeArray<float3> vertices = new NativeArray<float3>(blockPositionsLength * 8, Allocator.TempJob);
        NativeReference<int> calcVertexCount = new NativeReference<int>(Allocator.TempJob);

        NativeArray<int> triangles = new NativeArray<int>(blockPositionsLength * 36, Allocator.TempJob);
        NativeReference<int> calcTriangleCount = new NativeReference<int>(Allocator.TempJob);


        NativeArray<int> textureIndexs = new NativeArray<int>(blockPositionsLength, Allocator.TempJob);

        NativeArray<byte> cubeFacesActiveState = new NativeArray<byte>(blockPositionsLength * 6, Allocator.TempJob);


        NativeArray<int3> neighborOffsetsAndNormals = new NativeArray<int3>(6, Allocator.TempJob);

        neighborOffsetsAndNormals[0] = new int3(0, 0, 1);     // Z+
        neighborOffsetsAndNormals[1] = new int3(0, 0, -1);    // Z-
        neighborOffsetsAndNormals[2] = new int3(-1, 0, 0);    // X-
        neighborOffsetsAndNormals[3] = new int3(1, 0, 0);     // X+
        neighborOffsetsAndNormals[4] = new int3(0, 1, 0);     // Y+
        neighborOffsetsAndNormals[5] = new int3(0, -1, 0);    // Y-


        NativeArray<float3> cubeVertices = new NativeArray<float3>(8, Allocator.TempJob);

        cubeVertices[0] = new float3(-0.5f, -0.5f, -0.5f);  // Vertex 0
        cubeVertices[1] = new float3(0.5f, -0.5f, -0.5f);   // Vertex 1
        cubeVertices[2] = new float3(0.5f, 0.5f, -0.5f);    // Vertex 2
        cubeVertices[3] = new float3(-0.5f, 0.5f, -0.5f);   // Vertex 3
        cubeVertices[4] = new float3(-0.5f, -0.5f, 0.5f);   // Vertex 4
        cubeVertices[5] = new float3(0.5f, -0.5f, 0.5f);    // Vertex 5
        cubeVertices[6] = new float3(0.5f, 0.5f, 0.5f);     // Vertex 6
        cubeVertices[7] = new float3(-0.5f, 0.5f, 0.5f);    // Vertex 7



        GenerateMeshCubesJobParallel generateMeshCubesJob = new GenerateMeshCubesJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,

            vertices = vertices,
            calcVertexCount = calcVertexCount,

            triangles = triangles,
            calcTriangleCount = calcTriangleCount,

            cubeFacesActiveState = cubeFacesActiveState,

            neighborOffsetsAndNormals = neighborOffsetsAndNormals,
            cubeVertices = cubeVertices,
        };

        mainJobHandle = generateMeshCubesJob.Schedule(blockPositionsLength, blockPositionsLength, mainJobHandle);
        mainJobHandle.Complete();

        #endregion




        #region Filter Vertices, Triangles and Normals, then apply call ApplyMeshToObject

        NativeArray<float3> filteredVertices = new NativeArray<float3>(calcVertexCount.Value, Allocator.TempJob);
        NativeArray<int> filteredTriangles = new NativeArray<int>(calcTriangleCount.Value, Allocator.TempJob);


        NativeArray<float3>.Copy(vertices, filteredVertices, calcVertexCount.Value);
        NativeArray<int>.Copy(triangles, filteredTriangles, calcTriangleCount.Value);

        vertices.Dispose();
        triangles.Dispose();

        calcVertexCount.Dispose();
        calcTriangleCount.Dispose();


        ApplyMeshToObject(filteredVertices, filteredTriangles, cubeFacesActiveState, textureIndexs, atlasSize, mesh, coll);

        #endregion




        #region Dispose All Native Containers That Are Done Being Used After ApplyMeshToObject

        blockPositions.Dispose();
        blockPositionsMap.Dispose();

        textureIndexs.Dispose();

        cubeFacesActiveState.Dispose();

        filteredVertices.Dispose();
        filteredTriangles.Dispose();

        connectedChunkEdgePositions.Dispose();

        #endregion
    }






    [BurstCompile]
    private struct SetupDataJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeHashMap<int3, bool> blockPositionsMap;

        public void Execute(int index)
        {
            int3 gridPosition = blockPositions[index];
            blockPositionsMap.TryAdd(gridPosition, false);
        }
    }




    [BurstCompile]
    private struct CalculateChunkConnectionsJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [NoAlias][WriteOnly] public NativeHashMap<int3, bool> blockPositionsMap;

        public void Execute(int index)
        {
            int3 gridPosition = blockPositions[index];
            blockPositionsMap.TryAdd(gridPosition, false);
        }
    }




    [BurstCompile]
    private struct GenerateMeshCubesJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int3> blockPositions;

        [NoAlias][ReadOnly] public NativeHashMap<int3, bool> blockPositionsMap;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float3> vertices;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int> triangles;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<byte> cubeFacesActiveState;


        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeReference<int> calcVertexCount;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeReference<int> calcTriangleCount;


        [NoAlias][ReadOnly] public NativeArray<int3> neighborOffsetsAndNormals;
        [NoAlias][ReadOnly] public NativeArray<float3> cubeVertices;


        [NoAlias] private int cVertexIndex;
        [NoAlias] private int cTriangleIndex;


        [BurstCompile]
        public void Execute(int cubeIndex)
        {
            byte frontFaceVisible, backFaceVisible, leftFaceVisible, rightFaceVisible, topFaceVisible, bottomFaceVisible;

            int3 cubePosition = blockPositions[cubeIndex];

            int3 neighborPositionZPlus = cubePosition + neighborOffsetsAndNormals[0];      // Z+
            int3 neighborPositionZMinus = cubePosition + neighborOffsetsAndNormals[1];    // Z-
            int3 neighborPositionXMinus = cubePosition + neighborOffsetsAndNormals[2];    // X-
            int3 neighborPositionXPlus = cubePosition + neighborOffsetsAndNormals[3];     // X+
            int3 neighborPositionYPlus = cubePosition + neighborOffsetsAndNormals[4];     // Y+
            int3 neighborPositionYMinus = cubePosition + neighborOffsetsAndNormals[5];    // Y-

            // Use the NativeHashMap to check neighbor visibility
            frontFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionZPlus) ? 0 : 1);
            backFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionZMinus) ? 0 : 1);
            leftFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionXMinus) ? 0 : 1);
            rightFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionXPlus) ? 0 : 1);
            topFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionYPlus) ? 0 : 1);
            bottomFaceVisible = (byte)(blockPositionsMap.ContainsKey(neighborPositionYMinus) ? 0 : 1);


            if (frontFaceVisible == 0 && backFaceVisible == 0 && leftFaceVisible == 0 && rightFaceVisible == 0 && bottomFaceVisible == 0 && topFaceVisible == 0)
            {
                // Skip this cube entirely if no faces are visible

                if (cubeIndex == (blockPositions.Length - 1))
                {
                    FinilizeJobData();
                }
                return;
            }


            int addedTriangles = 0;
            int addedVertices = 0;


            int cCubeActiveStateIndex = cubeIndex * 6; // 6 faces for each cube


            #region Add face vertices and triangles if the face is visible

            // Back face
            if (backFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 0] = 1;

                vertices[cVertexIndex + 0] = cubeVertices[0] + cubePosition;
                vertices[cVertexIndex + 2] = cubeVertices[2] + cubePosition;
                vertices[cVertexIndex + 1] = cubeVertices[1] + cubePosition;
                vertices[cVertexIndex + 3] = cubeVertices[3] + cubePosition;

                addedVertices = 4;

                // Add triangles for the back face
                triangles[cTriangleIndex + 0] = cVertexIndex + 0;
                triangles[cTriangleIndex + 1] = cVertexIndex + 2;
                triangles[cTriangleIndex + 2] = cVertexIndex + 1;
                triangles[cTriangleIndex + 3] = cVertexIndex + 0;
                triangles[cTriangleIndex + 4] = cVertexIndex + 3;
                triangles[cTriangleIndex + 5] = cVertexIndex + 2;

                addedTriangles += 6;
            }

            // Front face
            if (frontFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 1] = 1;


                vertices[cVertexIndex + 4] = cubeVertices[4] + cubePosition;
                vertices[cVertexIndex + 5] = cubeVertices[5] + cubePosition;
                vertices[cVertexIndex + 6] = cubeVertices[6] + cubePosition;
                vertices[cVertexIndex + 7] = cubeVertices[7] + cubePosition;

                addedVertices = 8;

                // Add triangles for the front face
                triangles[cTriangleIndex + addedTriangles + 0] = cVertexIndex + 4;
                triangles[cTriangleIndex + addedTriangles + 1] = cVertexIndex + 5;
                triangles[cTriangleIndex + addedTriangles + 2] = cVertexIndex + 6;
                triangles[cTriangleIndex + addedTriangles + 3] = cVertexIndex + 4;
                triangles[cTriangleIndex + addedTriangles + 4] = cVertexIndex + 6;
                triangles[cTriangleIndex + addedTriangles + 5] = cVertexIndex + 7;

                addedTriangles += 6;
            }

            // Right face
            if (rightFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 2] = 1;


                vertices[cVertexIndex + 1] = cubeVertices[1] + cubePosition;
                vertices[cVertexIndex + 6] = cubeVertices[6] + cubePosition;
                vertices[cVertexIndex + 2] = cubeVertices[2] + cubePosition;
                vertices[cVertexIndex + 5] = cubeVertices[5] + cubePosition;

                addedVertices = 7;

                // Add triangles for the right face
                triangles[cTriangleIndex + addedTriangles + 0] = cVertexIndex + 1;
                triangles[cTriangleIndex + addedTriangles + 1] = cVertexIndex + 2;
                triangles[cTriangleIndex + addedTriangles + 2] = cVertexIndex + 6;
                triangles[cTriangleIndex + addedTriangles + 3] = cVertexIndex + 1;
                triangles[cTriangleIndex + addedTriangles + 4] = cVertexIndex + 6;
                triangles[cTriangleIndex + addedTriangles + 5] = cVertexIndex + 5;

                addedTriangles += 6;
            }

            // Left face
            if (leftFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 3] = 1;


                vertices[cVertexIndex + 0] = cubeVertices[0] + cubePosition;
                vertices[cVertexIndex + 7] = cubeVertices[7] + cubePosition;
                vertices[cVertexIndex + 4] = cubeVertices[4] + cubePosition;
                vertices[cVertexIndex + 3] = cubeVertices[3] + cubePosition;

                addedVertices = 8;

                // Add triangles for the left face
                triangles[cTriangleIndex + addedTriangles + 0] = cVertexIndex + 0;
                triangles[cTriangleIndex + addedTriangles + 1] = cVertexIndex + 4;
                triangles[cTriangleIndex + addedTriangles + 2] = cVertexIndex + 7;
                triangles[cTriangleIndex + addedTriangles + 3] = cVertexIndex + 0;
                triangles[cTriangleIndex + addedTriangles + 4] = cVertexIndex + 7;
                triangles[cTriangleIndex + addedTriangles + 5] = cVertexIndex + 3;

                addedTriangles += 6;
            }

            // Top face
            if (topFaceVisible == 1)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 4] = 1;


                vertices[cVertexIndex + 3] = cubeVertices[3] + cubePosition;
                vertices[cVertexIndex + 6] = cubeVertices[6] + cubePosition;
                vertices[cVertexIndex + 2] = cubeVertices[2] + cubePosition;
                vertices[cVertexIndex + 7] = cubeVertices[7] + cubePosition;

                addedVertices = 8;

                // Add triangles for the top face
                triangles[cTriangleIndex + addedTriangles + 0] = cVertexIndex + 3;
                triangles[cTriangleIndex + addedTriangles + 1] = cVertexIndex + 6;
                triangles[cTriangleIndex + addedTriangles + 2] = cVertexIndex + 2;
                triangles[cTriangleIndex + addedTriangles + 3] = cVertexIndex + 3;
                triangles[cTriangleIndex + addedTriangles + 4] = cVertexIndex + 7;
                triangles[cTriangleIndex + addedTriangles + 5] = cVertexIndex + 6;

                addedTriangles += 6;
            }

            // Bottom face
            if (bottomFaceVisible == 1 && cubePosition.y != 0)
            {
                cubeFacesActiveState[cCubeActiveStateIndex + 5] = 1;


                vertices[cVertexIndex + 0] = cubeVertices[0] + cubePosition;
                vertices[cVertexIndex + 5] = cubeVertices[5] + cubePosition;
                vertices[cVertexIndex + 1] = cubeVertices[1] + cubePosition;
                vertices[cVertexIndex + 4] = cubeVertices[4] + cubePosition;

                addedVertices = 6;

                // Add triangles for the bottom face
                triangles[cTriangleIndex + addedTriangles + 0] = cVertexIndex + 0;
                triangles[cTriangleIndex + addedTriangles + 1] = cVertexIndex + 5;
                triangles[cTriangleIndex + addedTriangles + 2] = cVertexIndex + 1;
                triangles[cTriangleIndex + addedTriangles + 3] = cVertexIndex + 0;
                triangles[cTriangleIndex + addedTriangles + 4] = cVertexIndex + 4;
                triangles[cTriangleIndex + addedTriangles + 5] = cVertexIndex + 5;

                addedTriangles += 6;
            }

            #endregion

            
            Interlocked.Add(ref cVertexIndex, addedVertices);

            Interlocked.Add(ref cTriangleIndex, addedTriangles);

            
            if (cubeIndex == (blockPositions.Length - 1))
            {
                FinilizeJobData();
            }
        }


        private void FinilizeJobData()
        {
            calcVertexCount.Value = cVertexIndex;
            calcTriangleCount.Value = cTriangleIndex;
        }
    }




    private static void ApplyMeshToObject(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs, int atlasSize, Mesh mesh, MeshCollider coll)
    {
        //NativeArray<float2> uvs = new NativeArray<float2>(vertices.Length, Allocator.TempJob);
        //TextureCalculator.ScheduleUVGeneration(uvs, cubeFacesActiveState, textureIndexs, atlasSize);


        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        //mesh.SetUVs(0, uvs);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        mesh.tangents = null;

        mesh.Optimize();
        mesh.MarkDynamic();

        coll.sharedMesh = mesh;

        //uvs.Dispose();
    }

}
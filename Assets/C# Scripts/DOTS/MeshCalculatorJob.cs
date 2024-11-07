using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


[BurstCompile]
public struct MeshCalculatorJob
{
    private static NativeArray<float3> cubeVertices;
    public static Stopwatch sw;

    public static void Init()
    {
        cubeVertices = new NativeArray<float3>(8, Allocator.Persistent);

        cubeVertices[0] = new float3(-0.5f, -0.5f, -0.5f);  // Vertex 0
        cubeVertices[1] = new float3(0.5f, -0.5f, -0.5f);   // Vertex 1
        cubeVertices[2] = new float3(0.5f, 0.5f, -0.5f);    // Vertex 2
        cubeVertices[3] = new float3(-0.5f, 0.5f, -0.5f);   // Vertex 3
        cubeVertices[4] = new float3(-0.5f, -0.5f, 0.5f);   // Vertex 4
        cubeVertices[5] = new float3(0.5f, -0.5f, 0.5f);    // Vertex 5
        cubeVertices[6] = new float3(0.5f, 0.5f, 0.5f);     // Vertex 6
        cubeVertices[7] = new float3(-0.5f, 0.5f, 0.5f);    // Vertex 7
    }



    public static void CallGenerateMeshJob(int3 chunkGridPos, ref NativeArray<BlockPos> blockPositions, Mesh mesh, MeshCollider coll)
    {
        #region Data Creation

        JobHandle mainJobHandle;

        int blockPositionsLength = blockPositions.Length;

        NativeHashMap<BlockPos, byte> blockPositionsMap = new NativeHashMap<BlockPos, byte>(blockPositionsLength, Allocator.TempJob);


        NativeReference<int2> calcVertexTriangleCount = new NativeReference<int2>(Allocator.TempJob);

        NativeArray<float3> vertices = new NativeArray<float3>(blockPositionsLength * 8, Allocator.TempJob);

        NativeArray<int> triangles = new NativeArray<int>(blockPositionsLength * 36, Allocator.TempJob);


        NativeArray<ushort> textureIndexs = new NativeArray<ushort>(blockPositionsLength, Allocator.TempJob);

        NativeArray<byte> cubeFacesActiveState = new NativeArray<byte>(blockPositionsLength * 6, Allocator.TempJob);

        #endregion
        //13000 ticks




        #region SetupData Job

        AddArrayToHashMapJobParallel setupDataJobParallel = new AddArrayToHashMapJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,
        };

        mainJobHandle = setupDataJobParallel.Schedule(blockPositionsLength, blockPositionsLength);
        #endregion
        //7000 ticks



        #region Calculate ConnectedChunks Edge Positions Job

        NativeArray<BlockPos> connectedChunkEdgePositions = ChunkManager.GetConnectedChunkEdgePositionsCount(chunkGridPos, out JobHandle edgesJobHandle);

        AddArrayToHashMapJobParallel calculateChunkConnectionsJobParallel = new AddArrayToHashMapJobParallel
        {
            blockPositions = connectedChunkEdgePositions,
            blockPositionsMap = blockPositionsMap,
        };

        mainJobHandle = JobHandle.CombineDependencies(mainJobHandle, edgesJobHandle);
        mainJobHandle = JobHandle.CombineDependencies(mainJobHandle, calculateChunkConnectionsJobParallel.Schedule(connectedChunkEdgePositions.Length, connectedChunkEdgePositions.Length, mainJobHandle));

        #endregion

        //210 ticks for adding connected chunk edges




        #region GenerateMeshCubes Job

        GenerateMeshCubesJobParallel generateMeshCubesJob = new GenerateMeshCubesJobParallel
        {
            blockPositions = blockPositions,
            blockPositionsMap = blockPositionsMap,

            calcVertexTriangleCount = calcVertexTriangleCount,
            vertices = vertices,
            triangles = triangles,

            cubeFacesActiveState = cubeFacesActiveState,

            cubeVertices = cubeVertices,
        };

        mainJobHandle = generateMeshCubesJob.Schedule(blockPositionsLength, blockPositionsLength, mainJobHandle);

        mainJobHandle.Complete();

        #endregion

        //1000 ticks for Calculating verts, tris, facesAtciveState, textureIndexs




        #region Filter Vertices And Triangles, then apply call ApplyMeshToObject

        int calcVertexCount = calcVertexTriangleCount.Value.x;
        NativeArray<float3> filteredVertices = new NativeArray<float3>(calcVertexCount, Allocator.TempJob);

        FilterVerticesJobParallel filterVerticesJob = new FilterVerticesJobParallel
        {
            vertices = vertices,
            filterdVertices = filteredVertices,
        };

        int calcTriangleCount = calcVertexTriangleCount.Value.y;
        NativeArray<int> filteredTriangles = new NativeArray<int>(calcTriangleCount, Allocator.TempJob);

        FilterTrianglesJobParallel filterTrianglesJob = new FilterTrianglesJobParallel
        {
            triangles = triangles,
            filterdTriangles = filteredTriangles,
        };


        mainJobHandle = filterVerticesJob.Schedule(calcVertexCount, calcVertexCount);
        mainJobHandle = JobHandle.CombineDependencies(filterTrianglesJob.Schedule(calcTriangleCount, calcTriangleCount), mainJobHandle);


        mainJobHandle.Complete();


        calcVertexTriangleCount.Dispose();
        vertices.Dispose();
        triangles.Dispose();

        //8000 ticks for filtering verts and tris

        #endregion

        //750 ticks for filtering tris and verts




        ApplyMeshToObject(filteredVertices, filteredTriangles, cubeFacesActiveState, textureIndexs, mesh, coll);




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
    private struct AddArrayToHashMapJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<BlockPos> blockPositions;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeHashMap<BlockPos, byte> blockPositionsMap;

        public void Execute(int index)
        {
            BlockPos gridPosition = blockPositions[index];

            blockPositionsMap.TryAdd(gridPosition, 0);
        }
    }




    [BurstCompile]
    private struct GenerateMeshCubesJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<BlockPos> blockPositions;

        [NoAlias][ReadOnly] public NativeHashMap<BlockPos, byte> blockPositionsMap;


        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeReference<int2> calcVertexTriangleCount;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float3> vertices;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int> triangles;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<byte> cubeFacesActiveState;



        [NoAlias][ReadOnly] public NativeArray<float3> cubeVertices;


        [NoAlias] private int cVertexIndex;
        [NoAlias] private int cTriangleIndex;


        [BurstCompile]
        public void Execute(int blockIndex)
        {
            int3 cubePosition = blockPositions[blockIndex].ToInt3();

            BlockPos neighborPositionXPlus  = new BlockPos((sbyte)(cubePosition.x + 1), (byte)cubePosition.y,       (sbyte)cubePosition.z);
            BlockPos neighborPositionXMinus = new BlockPos((sbyte)(cubePosition.x - 1), (byte)cubePosition.y,       (sbyte)cubePosition.z);
            BlockPos neighborPositionYPlus  = new BlockPos((sbyte)cubePosition.x,       (byte)(cubePosition.y + 1), (sbyte)cubePosition.z);
            BlockPos neighborPositionYMinus = new BlockPos((sbyte)cubePosition.x,       (byte)(cubePosition.y - 1), (sbyte)cubePosition.z);
            BlockPos neighborPositionZPlus  = new BlockPos((sbyte)cubePosition.x,       (byte)cubePosition.y,       (sbyte)(cubePosition.z + 1));
            BlockPos neighborPositionZMinus = new BlockPos((sbyte)cubePosition.x,       (byte)cubePosition.y,       (sbyte)(cubePosition.z - 1));





            int addedTriangles = 0;
            int addedVertices = 0;

            int cCubeActiveStateIndex = blockIndex * 6; // 6 faces for each cube


            #region Add face vertices and triangles if the face is visible

            // Back face
            if (blockPositionsMap.ContainsKey(neighborPositionZMinus) == false)
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
            if (blockPositionsMap.ContainsKey(neighborPositionZPlus) == false)
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
            if (blockPositionsMap.ContainsKey(neighborPositionXPlus) == false)
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
            if (blockPositionsMap.ContainsKey(neighborPositionXMinus) == false)
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
            if (blockPositionsMap.ContainsKey(neighborPositionYPlus) == false)
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
            if ((blockPositionsMap.ContainsKey(neighborPositionYMinus) == false) && cubePosition.y != 0)
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

            
            if (blockIndex == (blockPositions.Length - 1))
            {
                FinilizeJobData();
            }
        }


        private void FinilizeJobData()
        {
            calcVertexTriangleCount.Value = new int2(cVertexIndex, cTriangleIndex);
        }
    }




    [BurstCompile]
    private struct FilterVerticesJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<float3> vertices;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float3> filterdVertices;


        [BurstCompile]
        public void Execute(int index)
        {
            filterdVertices[index] = vertices[index];
        }
    }

    [BurstCompile]
    private struct FilterTrianglesJobParallel : IJobParallelFor
    {
        [NoAlias][ReadOnly] public NativeArray<int> triangles;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<int> filterdTriangles;


        [BurstCompile]
        public void Execute(int index)
        {
            filterdTriangles[index] = triangles[index];
        }
    }



    
    private static void ApplyMeshToObject(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<byte> cubeFacesActiveState, NativeArray<ushort> textureIndexs, Mesh mesh, MeshCollider coll)
    {
        NativeArray<float4> uvs = new NativeArray<float4>(vertices.Length, Allocator.TempJob);
        NativeArray<float2> textureData = new NativeArray<float2>(vertices.Length, Allocator.TempJob);
        //4500 ticks


        TextureCalculator.ScheduleUVGeneration(uvs, textureData, cubeFacesActiveState, textureIndexs);
        //9000 ticks


        if (vertices.Length > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        // Use SetVertexBufferData instead of SetVertices
        mesh.SetVertexBufferParams(vertices.Length, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
        mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);

        // Use SetIndexBufferData instead of SetTriangles
        mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
        mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);

        mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));

        mesh.RecalculateBounds();

        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, textureData);

        mesh.Optimize();
        mesh.MarkDynamic();
        //20000 ticks


        coll.sharedMesh = mesh;
        //4000 ticks


        uvs.Dispose();
        textureData.Dispose();
        //2800 ticks
    }
}
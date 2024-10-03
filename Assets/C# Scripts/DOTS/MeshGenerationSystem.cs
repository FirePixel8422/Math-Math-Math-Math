//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Rendering;
//using UnityEngine;
//using Unity.Transforms; // Required for LocalTransform

//public partial class MeshGenerationSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        Entity entity = EntityManager.CreateEntity(typeof(MeshDataComponent));
//        EntityManager.SetComponentData(entity, new MeshDataComponent { Initialized = false });
//    }

//    protected override void OnUpdate()
//    {
//        var ecb = new EntityCommandBuffer(Allocator.Temp);

//        Entities.ForEach((Entity entity, ref MeshDataComponent meshData) =>
//        {
//            if (meshData.Initialized)
//                return;

//            GenerateMesh(entity, ecb);

//            meshData.Initialized = true;

//        }).WithoutBurst().Run();

//        ecb.Playback(EntityManager);
//        ecb.Dispose();
//    }

//    void GenerateMesh(Entity entity, EntityCommandBuffer ecb)
//    {
//        int gridSize = 100; // 100x100 grid (10,000 vertices)
//        var mesh = CreateMesh(gridSize);

//        // Create a material
//        Material material = new Material(Shader.Find("Unlit/Color"));
//        material.color = Color.red; // Set a visible color
//        if (material == null)
//        {
//            Debug.LogError("Material could not be created.");
//            return;
//        }

//        // Ensure the RenderMeshUnmanaged component is added to the entity
//        if (!EntityManager.HasComponent<RenderMeshUnmanaged>(entity))
//        {
//            // Create a new RenderMeshUnmanaged component
//            var renderMesh = new RenderMeshUnmanaged
//            {
//                mesh = mesh,
//                materialForSubMesh = material // Set the material here
//            };

//            ecb.AddComponent(entity, renderMesh);
//            ecb.AddComponent(entity, new LocalTransform
//            {
//                Position = new float3(0, 0, 0), // Initial position
//                Rotation = quaternion.identity, // Initial rotation
//                Scale = 1.0f // Initial scale
//            });
//        }
//        else
//        {
//            // Update the existing RenderMeshUnmanaged component
//            ecb.AddComponent(entity, new RenderMeshUnmanaged
//            {
//                mesh = mesh,
//                materialForSubMesh = material // Ensure the material is assigned
//            });
//        }

//        Debug.Log("Mesh generation complete.");
//    }

//    private Mesh CreateMesh(int gridSize)
//    {
//        int vertexCount = (gridSize + 1) * (gridSize + 1);
//        int triangleCount = gridSize * gridSize * 6;

//        NativeArray<float3> vertices = new NativeArray<float3>(vertexCount, Allocator.Persistent);
//        NativeArray<float3> normals = new NativeArray<float3>(vertexCount, Allocator.Persistent);
//        NativeArray<float2> uvs = new NativeArray<float2>(vertexCount, Allocator.Persistent);
//        NativeArray<int> triangles = new NativeArray<int>(triangleCount, Allocator.Persistent);

//        int vertIndex = 0;
//        int triIndex = 0;

//        for (int y = 0; y <= gridSize; y++)
//        {
//            for (int x = 0; x <= gridSize; x++)
//            {
//                vertices[vertIndex] = new float3(x, 0, y);
//                normals[vertIndex] = math.up();
//                uvs[vertIndex] = new float2((float)x / gridSize, (float)y / gridSize);

//                if (x < gridSize && y < gridSize)
//                {
//                    triangles[triIndex++] = vertIndex;
//                    triangles[triIndex++] = vertIndex + gridSize + 1;
//                    triangles[triIndex++] = vertIndex + 1;

//                    triangles[triIndex++] = vertIndex + 1;
//                    triangles[triIndex++] = vertIndex + gridSize + 1;
//                    triangles[triIndex++] = vertIndex + gridSize + 2;
//                }

//                vertIndex++;
//            }
//        }

//        var mesh = new Mesh();
//        mesh.SetVertices(vertices.Reinterpret<Vector3>());
//        mesh.SetNormals(normals.Reinterpret<Vector3>());
//        mesh.SetUVs(0, uvs.Reinterpret<Vector2>());
//        mesh.SetTriangles(triangles.ToArray(), 0);

//        vertices.Dispose();
//        normals.Dispose();
//        uvs.Dispose();
//        triangles.Dispose();

//        return mesh;
//    }
//}

//public struct MeshDataComponent : IComponentData
//{
//    public bool Initialized;
//}

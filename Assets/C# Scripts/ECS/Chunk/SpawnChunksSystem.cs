using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


[BurstCompile]
public partial class SpawnChunksSystem : SystemBase
{
    public Entity chunkPrefabEntity;


    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnChunksConfig>();
    }

    protected override void OnUpdate()
    {
        // Get the singleton configuration data
        chunkPrefabEntity = SystemAPI.GetSingleton<SpawnChunksConfig>().chunkPrefabEntity;

        Enabled = false;
    }



    [BurstCompile]
    public void SpawnChunk(float3 position)
    {
        Entity spawnedChunkEntity = EntityManager.Instantiate(chunkPrefabEntity);

        EntityManager.SetComponentData(spawnedChunkEntity, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1
        });

        EntityManager.SetComponentData(spawnedChunkEntity, new MaterialMeshInfo
        {
            Mesh = (int)EntityManager.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().RegisterMesh(CreateCubeMesh()).value,
            Material = (int)EntityManager.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().RegisterMaterial(Resources.Load<Material>("Atlas")).value
        });
        EntityManager.AddComponentData(spawnedChunkEntity, new ChunkDataComponent
        {
            gridPos = new int3(0, 0, 0),
        });
    }


    public Mesh CreateCubeMesh()
    {
        Mesh cubeMesh = new Mesh();

        // Define the vertices for each face of the cube
        Vector3[] vertices = new Vector3[]
        {
        // Front face
        new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-left
        new Vector3(0.5f, -0.5f, 0.5f),   // Bottom-right
        new Vector3(0.5f, 0.5f, 0.5f),    // Top-right
        new Vector3(-0.5f, 0.5f, 0.5f),   // Top-left

        // Back face
        new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-left
        new Vector3(-0.5f, 0.5f, -0.5f),  // Top-left
        new Vector3(0.5f, 0.5f, -0.5f),   // Top-right
        new Vector3(0.5f, -0.5f, -0.5f),  // Bottom-right

        // Left face
        new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-left
        new Vector3(-0.5f, 0.5f, -0.5f),  // Top-left
        new Vector3(-0.5f, 0.5f, 0.5f),   // Top-right
        new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-right

        // Right face
        new Vector3(0.5f, -0.5f, 0.5f),   // Bottom-left
        new Vector3(0.5f, 0.5f, 0.5f),    // Top-left
        new Vector3(0.5f, 0.5f, -0.5f),   // Top-right
        new Vector3(0.5f, -0.5f, -0.5f),  // Bottom-right

        // Top face
        new Vector3(-0.5f, 0.5f, 0.5f),   // Bottom-left
        new Vector3(0.5f, 0.5f, 0.5f),    // Bottom-right
        new Vector3(0.5f, 0.5f, -0.5f),   // Top-right
        new Vector3(-0.5f, 0.5f, -0.5f),  // Top-left

        // Bottom face
        new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-left
        new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-right
        new Vector3(0.5f, -0.5f, -0.5f),  // Top-right
        new Vector3(0.5f, -0.5f, 0.5f)    // Top-left
        };

        // Define the triangles for each face of the cube (winding order corrected)
        int[] triangles = new int[]
        {
        // Front face
        0, 1, 2, 0, 2, 3,
        // Back face
        4, 5, 6, 4, 6, 7,
        // Left face
        8, 10, 9, 8, 11, 10,
        // Right face
        12, 14, 13, 12, 15, 14,
        // Top face
        16, 17, 18, 16, 18, 19,
        // Bottom face
        20, 21, 22, 20, 22, 23
        };

        // Set the vertices and triangles to the mesh
        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;

        // Calculate normals for proper lighting
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();

        return cubeMesh;
    }
}

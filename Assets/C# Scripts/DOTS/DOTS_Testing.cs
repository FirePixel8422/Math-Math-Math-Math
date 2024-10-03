using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;


public class DOTS_Testing : MonoBehaviour
{
    public Mesh mesh;
    public Material material;



    private void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(MoveSpeedComponent),
            typeof(LocalTransform),
            typeof(RenderMeshArray),
            typeof(RenderBounds),
            typeof(LocalToWorld)
            );

        NativeArray<Entity> entityArray = new NativeArray<Entity>(25, Allocator.Temp);

        entityManager.CreateEntity(archetype, entityArray);



        Mesh[] meshes = new Mesh[] { mesh };

        UnityObjectRef<Mesh>[] meshRefs = new UnityObjectRef<Mesh>[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            meshRefs[i] = meshes[i];
        }



        for (int i = 0; i < entityArray.Length; i++)
        {
            Entity entity = entityArray[i];

            entityManager.SetComponentData(entity, new MoveSpeedComponent() { moveSpeed = new float2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)) });

            RenderMeshArray meshArray = new RenderMeshArray
            {
                MaterialMeshIndices = new MaterialMeshIndex[]
                {
                    new MaterialMeshIndex
                    {
                        MaterialIndex = 0,
                        MeshIndex = 0
                    }
                },
                MeshReferences = meshRefs
            };


            entityManager.SetSharedComponentManaged(entity, meshArray);
            entityManager.AddSharedComponentManaged(entity, meshArray);

            //RenderMeshUtility.AddComponents(entity, entityManager, new RenderMeshDescription(), meshArray, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        }

        entityArray.Dispose();
    }
}

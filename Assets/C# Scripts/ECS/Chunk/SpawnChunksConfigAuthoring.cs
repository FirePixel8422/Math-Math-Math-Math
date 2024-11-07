using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SpawnChunksConfigAuthoring : MonoBehaviour
{
    public GameObject chunkPrefab;


    public class Baker : Baker<SpawnChunksConfigAuthoring>
    {
        public override void Bake(SpawnChunksConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new SpawnChunksConfig
            {
                chunkPrefabEntity = GetEntity(authoring.chunkPrefab, TransformUsageFlags.None),
            });
        }
    }
}

public struct SpawnChunksConfig : IComponentData
{
    public Entity chunkPrefabEntity;
}
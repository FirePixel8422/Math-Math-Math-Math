using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[BurstCompile]
public class MeshTester : MonoBehaviour
{
    public static MeshTester Instance;
    private void Awake()
    {
        Instance = this;
    }



    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;


    public int atlasSize;

    public int randomSpawnAmount;
    public Vector3Int spawnBounds;



    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        ChunkManager.Instance.Init();
    }


    [ContextMenu("Create Random Mesh In Bounds")]
    public void DEBUG_CreateRandomMeshInBounds()
    {
        CreateRandomMeshInBounds();
    }

    public int3[] pos;


    [BurstCompile]
    public void CreateRandomMeshInBounds()
    {
        if (randomSpawnAmount > 0)
        {
            NativeList<int3> possiblePositions = new NativeList<int3>(spawnBounds.x * spawnBounds.y * spawnBounds.z, Allocator.Temp);

            for (int x = 0; x < spawnBounds.x + 1; x++)
            {
                for (int y = 0; y < spawnBounds.y + 1; y++)
                {
                    for (int z = 0; z < spawnBounds.z + 1; z++)
                    {
                        possiblePositions.Add(new int3(x, y, z));
                    }
                }
            }


            int calculatedAmount = Mathf.Min(randomSpawnAmount, possiblePositions.Length);

            NativeArray<int3> blockPositions = new NativeArray<int3>(calculatedAmount, Allocator.TempJob);

            for (int i = 0; i < calculatedAmount; i++)
            {
                blockPositions[i] = possiblePositions[0];
                possiblePositions.RemoveAt(0);
            }


            possiblePositions.Dispose();

            pos = blockPositions.ToArray();

            MeshCalculatorJob.CallGenerateMeshJob(int3.zero, blockPositions, atlasSize, meshFilter.mesh, GetComponent<MeshCollider>());
        }
    }
}

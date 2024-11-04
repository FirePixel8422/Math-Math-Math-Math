using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
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



    [BurstCompile]
    public void CreateRandomMeshInBounds()
    {
        if (randomSpawnAmount > 0)
        {
            NativeList<BlockPos> possiblePositions = new NativeList<BlockPos>(spawnBounds.x * spawnBounds.y * spawnBounds.z, Allocator.Temp);

            for (sbyte x = 0; x < ClampUnderMax(spawnBounds.x, 255) + 1; x++)
            {
                for (byte y = 0; y < ClampUnderMax(spawnBounds.y + 1, 255); y++)
                {
                    for (sbyte z = 0; z < ClampUnderMax(spawnBounds.z, 255) + 1; z++)
                    {
                        possiblePositions.Add(new BlockPos(x, y, z));
                    }
                }
            }


            int calculatedAmount = Mathf.Min(randomSpawnAmount, possiblePositions.Length);

            NativeArray<BlockPos> blockPositions = new NativeArray<BlockPos>(calculatedAmount, Allocator.TempJob);

            for (int i = 0; i < calculatedAmount; i++)
            {
                blockPositions[i] = possiblePositions[0];
                possiblePositions.RemoveAt(0);
            }


            possiblePositions.Dispose();

            MeshCalculatorJob.CallGenerateMeshJob(int3.zero, ref blockPositions, meshFilter.mesh, GetComponent<MeshCollider>());
        }
    }


    [BurstCompile]
    private int ClampUnderMax(int value, int max)
    {
        if (value > max)
        {
            value = max;
        }

        return value;
    }
}

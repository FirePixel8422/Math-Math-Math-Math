using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct ChunkDataComponent : IComponentData
{
    public int3 gridPos;

    public NativeArray<BlockPos> blockPositions;

    public NativeArray<BlockPos> blockPositions_Left;
    public NativeArray<BlockPos> blockPositions_Right;
    public NativeArray<BlockPos> blockPositions_Forward;
    public NativeArray<BlockPos> blockPositions_Back;


    public ChunkDataComponent(int3 _gridPos, NativeArray<BlockPos> _blockPositions, NativeArray<BlockPos> left, NativeArray<BlockPos> right, NativeArray<BlockPos> forward, NativeArray<BlockPos> back)
    {
        gridPos = _gridPos;

        blockPositions = _blockPositions;

        blockPositions_Left = left;
        blockPositions_Right = right;
        blockPositions_Forward = forward;
        blockPositions_Back = back;
    }
}

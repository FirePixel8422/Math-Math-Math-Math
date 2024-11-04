using System;
using Unity.Burst;
using Unity.Mathematics;



[BurstCompile]
[Serializable]
public struct BlockPos : IEquatable<BlockPos>
{
    public sbyte x;
    public byte y;
    public sbyte z;

    public byte dataFiller; // 1 byte for alignment, 4 byte structs are faster because they are a multitude of 2


    public BlockPos(sbyte _x, byte _y, sbyte _z, byte _dataFiller = 0)
    {
        x = _x;
        y = _y;
        z = _z;

        dataFiller = _dataFiller;
    }

    [BurstCompile]
    public bool Equals(BlockPos other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    [BurstCompile]
    public override bool Equals(object obj)
    {
        if (obj is BlockPos pos)
            return Equals(pos);
        return false;
    }

    [BurstCompile]
    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + x;
        hash = hash * 31 + y;
        hash = hash * 31 + z;
        return hash;
    }


    [BurstCompile]
    public int3 ToInt3()
    {
        return new int3(x, y, z);
    }
}




public struct DecimalOne : IEquatable<DecimalOne>
{
    private readonly int _value;


    public DecimalOne(float value)
    {
        _value = (int)(value * 10);
    }

    public float Value
    {
        get { return _value * 0.1f; }
    }


    public bool Equals(DecimalOne other)
    {
        if (Value == other.Value)
        {
            return _value == other._value;
        }
        return false;
    }
}
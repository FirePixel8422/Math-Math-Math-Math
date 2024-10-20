using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
[Obsolete]
public unsafe struct FragileNative2DArray<T> : INativeDisposable, IEquatable<FragileNative2DArray<T>> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    [NativeDisableContainerSafetyRestriction]
    private T* _buffer;

    private Allocator _allocator;
    private int _length;
    private int _width;

    public FragileNative2DArray(int length, int width, Allocator allocator)
    {
        _length = length;
        _width = width;

        _allocator = allocator;
        _buffer = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * length * width, UnsafeUtility.AlignOf<T>(), allocator);
    }

    // Indexer to access elements
    public T this[int indexX, int indexY]
    {
        get
        {
            return _buffer[indexX * _width + _width];
        }
        set
        {
            _buffer[indexX * _width + _width] = value;
        }
    }

    public int Length => _length * _width;


    // Dispose method to free allocated memory
    public void Dispose()
    {
        UnsafeUtility.Free(_buffer, _allocator);
        _buffer = null;
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        var disposeJob = new DisposeJob
        {
            Buffer = _buffer,
            Allocator = _allocator
        };
        _buffer = null;

        return disposeJob.Schedule(inputDeps);
    }

    [BurstCompile]
    private unsafe struct DisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public T* Buffer;

        public Allocator Allocator;

        public void Execute()
        {
            UnsafeUtility.Free(Buffer, Allocator);
        }
    }

    public bool Equals(FragileNative2DArray<T> other)
    {
        return _buffer == other._buffer && _length == other._length;
    }
}

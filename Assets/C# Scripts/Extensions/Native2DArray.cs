using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
[Obsolete]
public unsafe struct Native2DArray<T> : INativeDisposable, IEquatable<Native2DArray<T>> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    private T* _buffer;

    private Allocator _allocator;
    private int _length;
    private int _width;

    private AtomicSafetyHandle _safetyHandle;
    private DisposeSentinel _disposeSentinel;

    public Native2DArray(int length, int width, Allocator allocator)
    {
        _length = length;
        _width = width;

        _allocator = allocator;
        _buffer = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * length * width, UnsafeUtility.AlignOf<T>(), allocator);

        // Initialize elements to default(T)
        for (int i = 0; i < length * width; i++)
        {
            _buffer[i] = default(T);
        }

        // Create the AtomicSafetyHandle and DisposeSentinel separately
        DisposeSentinel.Create(out _safetyHandle, out _disposeSentinel, 1, allocator);

        AtomicSafetyHandle.SetAllowSecondaryVersionWriting(_safetyHandle, true);
    }

    public T this[int indexX, int indexY]
    {
        get
        {
            // Check for read access before reading
            AtomicSafetyHandle.CheckReadAndThrow(_safetyHandle);

            if (indexX < 0 || indexX >= _length || indexY < 0 || indexY >= _width)
                throw new IndexOutOfRangeException("Index out of range in 2DArray!");

            return _buffer[indexX * _width + indexY];
        }
        set
        {
            // Check for write access before writing
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);

            if (indexX < 0 || indexX >= _length || indexY < 0 || indexY >= _width)
                throw new IndexOutOfRangeException("Index out of range in 2DArray!");

            _buffer[indexX * _width + indexY] = value;
        }
    }

    public T this[int totalIndex]
    {
        get
        {
            // Check for read access before reading
            AtomicSafetyHandle.CheckReadAndThrow(_safetyHandle);

            if (totalIndex < 0 || totalIndex >= Length)
                throw new IndexOutOfRangeException("Index out of range in 2DArray!");

            return _buffer[totalIndex];
        }
        set
        {
            // Check for write access before writing
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);

            if (totalIndex < 0 || totalIndex >= Length)
                throw new IndexOutOfRangeException("Index out of range in 2DArray!");

            _buffer[totalIndex] = value;
        }
    }

    public int Length => _length * _width;

    public void Dispose()
    {
        if (_buffer != null)
        {
            UnsafeUtility.Free(_buffer, _allocator);
            _buffer = null;
        }

        // Release the atomic safety handle and dispose sentinel
        AtomicSafetyHandle.Release(_safetyHandle);
        DisposeSentinel.Dispose(ref _safetyHandle, ref _disposeSentinel);
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        var disposeJob = new DisposeJob
        {
            Buffer = _buffer,
            Allocator = _allocator
        };

        // Release atomic safety handle in the job chain
        AtomicSafetyHandle.Release(_safetyHandle);
        DisposeSentinel.Clear(ref _disposeSentinel);

        _buffer = null;

        return disposeJob.Schedule(inputDeps);
    }

    [BurstCompile]
    private unsafe struct DisposeJob : IJob
    {
        public T* Buffer;
        public Allocator Allocator;

        public void Execute()
        {
            UnsafeUtility.Free(Buffer, Allocator);
        }
    }

    [BurstCompile]
    public bool Equals(Native2DArray<T> other)
    {
        return _buffer == other._buffer && _length == other._length;
    }

    [BurstCompile]
    public override int GetHashCode()
    {
        return (int)_buffer;
    }
}

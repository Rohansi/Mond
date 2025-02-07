using System;
using System.Collections.Generic;

namespace Mond.VirtualMachine;

internal class ArrayPool<T>
{
    private readonly Stack<T[]> _arrays;
    private readonly int _maxPooled;
    private readonly int _maxSize;
    private int _taken;
    private int _returned;

    public ArrayPool(int maxPooled, int maxSize)
    {
        _arrays = new Stack<T[]>(maxPooled);
        _maxPooled = maxPooled;
        _maxSize = maxSize;

        for (var i = 0; i < maxPooled; i++)
        {
            _arrays.Push(new T[maxSize]);
        }
    }

    public ArrayPoolHandle<T> Rent(int size)
    {
        if (size == 0)
        {
            return new ArrayPoolHandle<T>(null, [], 0);
        }

        if (size > _maxSize)
        {
            // too big for us to keep in the pool
            return new ArrayPoolHandle<T>(null, new T[size], size);
        }

        if (!_arrays.TryPop(out var array))
        {
            array = new T[_maxSize];
        }

        return new ArrayPoolHandle<T>(this, array, size);
    }

    public void Return(T[] array)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (array.Length != _maxSize)
        {
            throw new ArgumentException("Array is not the correct size", nameof(array));
        }

        if (_arrays.Count >= _maxPooled)
        {
            return;
        }

        _arrays.Push(array);
    }
}

internal ref struct ArrayPoolHandle<T>(ArrayPool<T> pool, T[] array, int size) : IDisposable
{
    private T[] _array = array;
    private Span<T> _span = new(array, 0, size);

    public Span<T> Span => _span;

    public void Dispose()
    {
        if (pool == null)
        {
            return;
        }

        _span.Clear();
        _span = default;
        pool.Return(_array);
        _array = null;
    }
}

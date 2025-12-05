using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

/// <summary>
/// A structure representing a dynamic array for unmanaged types.
/// </summary>
public unsafe struct ImVector
{
    public readonly int Size;
    public readonly int Capacity;
    public readonly void* Data;

    public ImVector(int size, int capacity, void* data)
    {
        Size = size;
        Capacity = capacity;
        Data = data;
    }

    public readonly ref T Ref<T>(int index) => ref Unsafe.AsRef<T>((byte*)this.Data + (index * Unsafe.SizeOf<T>()));

    public readonly nint Address<T>(int index) => (nint)((byte*)this.Data + (index * Unsafe.SizeOf<T>()));
}

/// <summary>
/// A structure representing a dynamic array for unmanaged types.
/// </summary>
/// <typeparam name="T">The type of elements in the vector, must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImVector<T> : IEnumerable<T>
    where T : unmanaged
{
    private int size;
    private int capacity;
    private T* data;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImVector{T}"/> struct with the specified size, capacity, and data pointer.
    /// </summary>
    /// <param name="size">The initial size of the vector.</param>
    /// <param name="capacity">The initial capacity of the vector.</param>
    /// <param name="data">Pointer to the initial data.</param>
    public ImVector(int size, int capacity, T* data)
    {
        this.size = size;
        this.capacity = capacity;
        this.data = data;
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
    public T this[int index]
    {
        readonly get
        {
            if (index < 0 || index >= this.size)
                throw new IndexOutOfRangeException();
            return this.data[index];
        }
        set
        {
            if (index < 0 || index >= this.size)
                throw new IndexOutOfRangeException();
            this.data[index] = value;
        }
    }

    /// <summary>
    /// Gets a pointer to the first element of the vector.
    /// </summary>
    public readonly T* Data => this.data;

    /// <summary>
    /// Gets a pointer to the first element of the vector.
    /// </summary>
    public readonly T* Front => this.data;

    /// <summary>
    /// Gets a pointer to the last element of the vector.
    /// </summary>
    public readonly T* Back => this.size > 0 ? this.data + this.size - 1 : null;

    /// <summary>
    /// Gets or sets the capacity of the vector.
    /// </summary>
    public int Capacity
    {
        readonly get => this.capacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, this.size, nameof(Capacity));
            if (this.capacity == value)
                return;

            if (this.data == null)
            {
                this.data = (T*)ImGui.MemAlloc((nuint)(value * sizeof(T)));
            }
            else
            {
                var newSize = Math.Min(this.size, value);
                var newData = (T*)ImGui.MemAlloc((nuint)(value * sizeof(T)));
                Buffer.MemoryCopy(this.data, newData, (nuint)(value * sizeof(T)), (nuint)(newSize * sizeof(T)));
                ImGui.MemFree(this.data);
                this.data = newData;
                this.size = newSize;
            }

            this.capacity = value;

            // Clear the rest of the data
            new Span<T>(this.data + this.size, this.capacity - this.size).Clear();
        }
    }

    /// <summary>
    /// Gets the number of elements in the vector.
    /// </summary>
    public readonly int Size => this.size;

    /// <summary>
    /// Grows the capacity of the vector to at least the specified value.
    /// </summary>
    /// <param name="newCapacity">The new capacity.</param>
    public void Grow(int newCapacity)
    {
        var newCapacity2 = this.capacity > 0 ? this.capacity + (this.capacity / 2) : 8;
        this.Capacity = newCapacity2 > newCapacity ? newCapacity2 : newCapacity;
    }

    /// <summary>
    /// Ensures that the vector has at least the specified capacity.
    /// </summary>
    /// <param name="size">The minimum capacity required.</param>
    public void EnsureCapacity(int size)
    {
        if (size > this.capacity)
            Grow(size);
    }

    /// <summary>
    /// Resizes the vector to the specified size.
    /// </summary>
    /// <param name="newSize">The new size of the vector.</param>
    public void Resize(int newSize)
    {
        EnsureCapacity(newSize);
        this.size = newSize;
    }

    /// <summary>
    /// Clears all elements from the vector.
    /// </summary>
    public void Clear() => this.size = 0;

    /// <summary>
    /// Adds an element to the end of the vector.
    /// </summary>
    /// <param name="value">The value to add.</param>
    [OverloadResolutionPriority(1)]
    public void PushBack(T value)
    {
        this.EnsureCapacity(this.size + 1);
        this.data[this.size++] = value;
    }

    /// <summary>
    /// Adds an element to the end of the vector.
    /// </summary>
    /// <param name="value">The value to add.</param>
    [OverloadResolutionPriority(2)]
    public void PushBack(in T value)
    {
        EnsureCapacity(this.size + 1);
        this.data[this.size++] = value;
    }

    /// <summary>
    /// Adds an element to the front of the vector.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public void PushFront(in T value)
    {
        if (this.size == 0)
            this.PushBack(value);
        else
            this.Insert(0, value);
    }

    /// <summary>
    /// Removes the last element from the vector.
    /// </summary>
    public void PopBack()
    {
        if (this.size > 0)
        {
            this.size--;
        }
    }

    public ref T Insert(int index, in T v) {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.size, nameof(index));
        this.EnsureCapacity(this.size + 1);
        if (index < this.size)
        {
            Buffer.MemoryCopy(
                this.data + index,
                this.data + index + 1,
                (this.size - index) * sizeof(T),
                (this.size - index) * sizeof(T));
        }

        this.data[index] = v;
        this.size++;
        return ref this.data[index];
    }

    public Span<T> InsertRange(int index, ReadOnlySpan<T> v)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, this.size, nameof(index));
        this.EnsureCapacity(this.size + v.Length);
        if (index < this.size)
        {
            Buffer.MemoryCopy(
                this.data + index,
                this.data + index + v.Length,
                (this.size - index) * sizeof(T),
                (this.size - index) * sizeof(T));
        }

        var dstSpan = new Span<T>(this.data + index, v.Length);
        v.CopyTo(new(this.data + index, v.Length));
        this.size += v.Length;
        return dstSpan;
    }

    /// <summary>
    /// Frees the memory allocated for the vector.
    /// </summary>
    public void Free()
    {
        if (this.data != null)
        {
            ImGui.MemFree(this.data);
            this.data = null;
            this.size = 0;
            this.capacity = 0;
        }
    }

    public readonly ref T Ref(int index)
    {
        return ref Unsafe.AsRef<T>((byte*)Data + (index * Unsafe.SizeOf<T>()));
    }

    public readonly ref TCast Ref<TCast>(int index)
    {
        return ref Unsafe.AsRef<TCast>((byte*)Data + (index * Unsafe.SizeOf<TCast>()));
    }

    public readonly void* Address(int index)
    {
        return (byte*)Data + (index * Unsafe.SizeOf<T>());
    }

    public readonly void* Address<TCast>(int index)
    {
        return (byte*)Data + (index * Unsafe.SizeOf<TCast>());
    }

    public readonly ImVector* ToUntyped()
    {
        return (ImVector*)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
    }

    public readonly Span<T> AsSpan() => new(this.data, this.size);

    public readonly Enumerator GetEnumerator() => new(this.data, this.data + this.size);

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public struct Enumerator(T* begin, T* end) : IEnumerator<T>, IEnumerable<T>
    {
        private T* current = null;

        public readonly ref T Current => ref *this.current;

        readonly T IEnumerator<T>.Current => this.Current;

        readonly object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            var next = this.current == null ? begin : this.current + 1;
            if (next == end)
                return false;
            this.current = next;
            return true;
        }

        public void Reset() => this.current = null;

        public readonly Enumerator GetEnumerator() => new(begin, end);

        readonly void IDisposable.Dispose()
        {
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

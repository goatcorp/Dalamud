using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

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

    public ref T Ref<T>(int index)
    {
        return ref Unsafe.AsRef<T>((byte*)Data + index * Unsafe.SizeOf<T>());
    }

    public IntPtr Address<T>(int index)
    {
        return (IntPtr)((byte*)Data + index * Unsafe.SizeOf<T>());
    }
}

/// <summary>
/// A structure representing a dynamic array for unmanaged types.
/// </summary>
/// <typeparam name="T">The type of elements in the vector, must be unmanaged.</typeparam>
public unsafe struct ImVector<T> where T : unmanaged
{
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

    private int size;
    private int capacity;
    private unsafe T* data;


    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= size)
            {
                throw new IndexOutOfRangeException();
            }
            return data[index];
        }
        set
        {
            if (index < 0 || index >= size)
            {
                throw new IndexOutOfRangeException();
            }
            data[index] = value;
        }
    }

    /// <summary>
    /// Gets a pointer to the first element of the vector.
    /// </summary>
    public readonly T* Data => data;

    /// <summary>
    /// Gets a pointer to the first element of the vector.
    /// </summary>
    public readonly T* Front => data;

    /// <summary>
    /// Gets a pointer to the last element of the vector.
    /// </summary>
    public readonly T* Back => size > 0 ? data + size - 1 : null;

    /// <summary>
    /// Gets or sets the capacity of the vector.
    /// </summary>
    public int Capacity
    {
        readonly get => capacity;
        set
        {
            if (capacity == value)
            {
                return;
            }

            if (data == null)
            {
                data = (T*)ImGui.MemAlloc((nuint)(value * sizeof(T)));
            }
            else
            {
                int newSize = Math.Min(size, value);
                T* newData = (T*)ImGui.MemAlloc((nuint)(value * sizeof(T)));
                Buffer.MemoryCopy(data, newData, (nuint)(value * sizeof(T)), (nuint)(newSize * sizeof(T)));
                ImGui.MemFree(data);
                data = newData;
                size = newSize;
            }

            capacity = value;

            // Clear the rest of the data
            for (int i = size; i < capacity; i++)
            {
                data[i] = default;
            }
        }
    }

    /// <summary>
    /// Gets the number of elements in the vector.
    /// </summary>
    public readonly int Size => size;

    /// <summary>
    /// Grows the capacity of the vector to at least the specified value.
    /// </summary>
    /// <param name="newCapacity">The new capacity.</param>
    public void Grow(int newCapacity)
    {
        if (newCapacity > capacity)
        {
            Capacity = newCapacity * 2;
        }
    }

    /// <summary>
    /// Ensures that the vector has at least the specified capacity.
    /// </summary>
    /// <param name="size">The minimum capacity required.</param>
    public void EnsureCapacity(int size)
    {
        if (size > capacity)
        {
            Grow(size);
        }
    }

    /// <summary>
    /// Resizes the vector to the specified size.
    /// </summary>
    /// <param name="newSize">The new size of the vector.</param>
    public void Resize(int newSize)
    {
        EnsureCapacity(newSize);
        size = newSize;
    }

    /// <summary>
    /// Clears all elements from the vector.
    /// </summary>
    public void Clear()
    {
        size = 0;
    }

    /// <summary>
    /// Adds an element to the end of the vector.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public void PushBack(T value)
    {
        EnsureCapacity(size + 1);
        data[size++] = value;
    }

    /// <summary>
    /// Removes the last element from the vector.
    /// </summary>
    public void PopBack()
    {
        if (size > 0)
        {
            size--;
        }
    }

    /// <summary>
    /// Frees the memory allocated for the vector.
    /// </summary>
    public void Free()
    {
        if (data != null)
        {
            ImGui.MemFree(data);
            data = null;
            size = 0;
            capacity = 0;
        }
    }

    public ref T Ref(int index)
    {
        return ref Unsafe.AsRef<T>((byte*)Data + index * Unsafe.SizeOf<T>());
    }

    public ref TCast Ref<TCast>(int index)
    {
        return ref Unsafe.AsRef<TCast>((byte*)Data + index * Unsafe.SizeOf<TCast>());
    }

    public void* Address(int index)
    {
        return (byte*)Data + index * Unsafe.SizeOf<T>();
    }

    public void* Address<TCast>(int index)
    {
        return (byte*)Data + index * Unsafe.SizeOf<TCast>();
    }

    public ImVector* ToUntyped()
    {
        return (ImVector*)Unsafe.AsPointer(ref this);
    }
}

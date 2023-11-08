#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Wrapper for ImVector.
/// </summary>
/// <typeparam name="T">Contained type.</typeparam>
public unsafe class ImVectorWrapper<T> : IList<T>, IList, IReadOnlyList<T>
    where T : unmanaged
{
    private readonly ImVector* vector;
    private readonly ImGuiNativeDestroyDelegate? destroyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImVectorWrapper{T}"/> class.
    /// </summary>
    /// <param name="vector">The underlying vector.</param>
    /// <param name="destroyer">Destroy function to call on item removal.</param>
    public ImVectorWrapper(ImVector* vector, ImGuiNativeDestroyDelegate? destroyer)
    {
        this.vector = vector;
        this.destroyer = destroyer;
    }

    /// <summary>
    /// Destroy callback for items.
    /// </summary>
    /// <param name="self">Pointer to self.</param>
    public delegate void ImGuiNativeDestroyDelegate(T* self);

    /// <summary>
    /// Gets a <see cref="Span{T}"/> view of the underlying ImVector{T}.
    /// </summary>
    public Span<T> AsSpan => new(this.Data, this.Length);

    /// <summary>
    /// Gets the number of items contained inside the underlying ImVector{T}.
    /// </summary>
    public int Length
    {
        get => this.vector->Size;
        private set => *&this.vector->Size = value;
    }

    /// <summary>
    /// Gets the number of items <b>that can be</b> contained inside the underlying ImVector{T}.
    /// </summary>
    public int Capacity
    {
        get => this.vector->Capacity;
        private set => *&this.vector->Capacity = value;
    }

    /// <summary>
    /// Gets the pointer to the first item in the data inside underlying ImVector{T}.
    /// </summary>
    /// <remarks>This may be null, if <see cref="Capacity"/> is zero.</remarks>
    public T* Data
    {
        get => (T*)this.vector->Data;
        private set => *&this.vector->Data = (nint)value;
    }

    /// <inheritdoc cref="ICollection{T}.IsReadOnly"/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    int ICollection.Count => this.Length;

    /// <inheritdoc/>
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc/>
    object ICollection.SyncRoot { get; } = new();

    /// <inheritdoc/>
    int ICollection<T>.Count => this.Length;

    /// <inheritdoc/>
    int IReadOnlyCollection<T>.Count => this.Length;

    /// <inheritdoc/>
    bool IList.IsFixedSize => false;

    /// <summary>
    /// Gets the element at the specified index as a reference.
    /// </summary>
    /// <param name="index">Index of the item.</param>
    /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is out of range.</exception>
    public ref T this[int index] => ref this.Data[this.EnsureIndex(index)];

    /// <inheritdoc/>
    T IReadOnlyList<T>.this[int index] => this[index];

    /// <inheritdoc/>
    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = value is null ? default : (T)value;
    }

    /// <inheritdoc/>
    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var i in Enumerable.Range(0, this.Length))
            yield return this[i];
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(in T item)
    {
        this.EnsureCapacity(this.Length + 1);
        this.Data[this.Length++] = item;
    }

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(IEnumerable<T> items)
    {
        if (items is ICollection { Count: var count })
            this.EnsureCapacity(this.Length + count);
        foreach (var item in items)
            this.Add(item);
    }

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(Span<T> items)
    {
        this.EnsureCapacity(this.Length + items.Length);
        foreach (var item in items)
            this.Add(item);
    }

    /// <inheritdoc cref="ICollection{T}.Clear"/>
    public void Clear()
    {
        if (this.destroyer != null)
        {
            foreach (var i in Enumerable.Range(0, this.Length))
                this.destroyer(&this.Data[i]);
        }

        this.Length = 0;
    }

    /// <inheritdoc cref="ICollection{T}.Contains"/>
    public bool Contains(in T item) => this.IndexOf(in item) != -1;

    /// <summary>
    /// Size down the underlying ImVector{T}.
    /// </summary>
    /// <param name="reservation">Capacity to reserve.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    public bool Compact(int reservation) => this.SetCapacity(Math.Max(reservation, this.Length));

    /// <inheritdoc cref="ICollection{T}.CopyTo"/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arrayIndex),
                arrayIndex,
                $"{nameof(arrayIndex)} is less than 0.");
        }

        if (array.Length - arrayIndex < this.Length)
        {
            throw new ArgumentException(
                "The number of elements in the source ImVectorWrapper<T> is greater than the available space from arrayIndex to the end of the destination array.",
                nameof(array));
        }

        fixed (void* p = array)
            Buffer.MemoryCopy(this.Data, p, this.Length * sizeof(T), this.Length * sizeof(T));
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    public bool EnsureCapacity(int capacity) =>
        this.Capacity < capacity && this.SetCapacity(
            Math.Max(
                capacity,
                1 << ((sizeof(int) * 8) - BitOperations.LeadingZeroCount((uint)this.Length))));

    /// <summary>
    /// Resizes the underlying array and fills with zeroes if grown.
    /// </summary>
    /// <param name="size">New size.</param>
    public void Resize(int size)
    {
        this.EnsureCapacity(size);
        var old = this.Length;
        this.Length = size;
        if (old < size)
            this.AsSpan[old..].Clear();
    }

    /// <summary>
    /// Resizes the underlying array and fills with the <paramref name="defaultValue"/>.
    /// </summary>
    /// <param name="size">New size.</param>
    /// <param name="defaultValue">New default value.</param>
    public void Resize(int size, in T defaultValue)
    {
        this.EnsureCapacity(size);
        var old = this.Length;
        this.Length = size;
        if (old < size)
            this.AsSpan[old..].Fill(defaultValue);
    }

    /// <inheritdoc cref="ICollection{T}.Remove"/>
    public bool Remove(in T item)
    {
        var index = this.IndexOf(item);
        if (index == -1)
            return false;
        this.RemoveAt(index);
        return true;
    }

    /// <inheritdoc cref="IList{T}.IndexOf"/>
    public int IndexOf(in T item)
    {
        foreach (var i in Enumerable.Range(0, this.Length))
        {
            if (Equals(item, this.Data[i]))
                return i;
        }

        return -1;
    }

    /// <inheritdoc cref="IList{T}.Insert"/>
    public void Insert(int index, in T item)
    {
        // Note: index == this.Length is okay; we're just adding to the end then
        if (index < 0 || index > this.Length)
            throw new IndexOutOfRangeException();
        this.EnsureCapacity(this.Capacity + 1);
        var num = this.Length - index;
        Buffer.MemoryCopy(this.Data + index, this.Data + index + 1, num * sizeof(T), num * sizeof(T));
        this.Data[index] = item;
    }

    /// <inheritdoc cref="List{T}.InsertRange"/>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items is ICollection { Count: var count })
        {
            this.EnsureCapacity(this.Length + count);
            var num = this.Length - index;
            Buffer.MemoryCopy(this.Data + index, this.Data + index + count, num * sizeof(T), num * sizeof(T));
            foreach (var item in items)
                this.Data[index++] = item;
        }
        else
        {
            foreach (var item in items)
                this.Insert(index++, item);
        }
    }

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void InsertRange(int index, Span<T> items)
    {
        this.EnsureCapacity(this.Length + items.Length);
        var num = this.Length - index;
        Buffer.MemoryCopy(this.Data + index, this.Data + index + items.Length, num * sizeof(T), num * sizeof(T));
        foreach (var item in items)
            this.Data[index++] = item;
    }

    /// <inheritdoc cref="IList{T}.RemoveAt"/>
    public void RemoveAt(int index)
    {
        this.EnsureIndex(index);
        var num = this.Length - index - 1;
        this.destroyer?.Invoke(&this.Data[index]);
        Buffer.MemoryCopy(this.Data + index + 1, this.Data + index, num * sizeof(T), num * sizeof(T));
    }

    /// <summary>
    /// Sets the capacity exactly as requested.
    /// </summary>
    /// <param name="capacity">New capacity.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than <see cref="Length"/>.</exception>
    /// <exception cref="OutOfMemoryException">If memory for the requested capacity cannot be allocated.</exception>
    public bool SetCapacity(int capacity)
    {
        if (capacity < this.Length)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
        if (capacity == this.Length)
            return false;

        var oldAlloc = this.Data;
        var oldSpan = new Span<T>(oldAlloc, this.Capacity);

        var newAlloc = (T*)(capacity == 0
                                ? null
                                : ImGuiNative.igMemAlloc(checked((uint)(capacity * sizeof(T)))));
        if (newAlloc is null && capacity > 0)
            throw new OutOfMemoryException();
        var newSpan = new Span<T>(newAlloc, capacity);

        if (!oldSpan.IsEmpty && !newSpan.IsEmpty)
            oldSpan[..this.Length].CopyTo(newSpan);
#if DEBUG
        new Span<byte>(newAlloc + this.Length, sizeof(T) * (capacity - this.Length)).Fill(0xCC);
#endif

        if (oldAlloc != null)
            ImGuiNative.igMemFree(oldAlloc);

        this.Data = newAlloc;
        this.Capacity = capacity;

        return true;
    }

    /// <inheritdoc/>
    void ICollection<T>.Add(T item) => this.Add(in item);

    /// <inheritdoc/>
    bool ICollection<T>.Contains(T item) => this.Contains(in item);

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"{nameof(index)} is less than 0.");
        }

        if (array.Length - index < this.Length)
        {
            throw new ArgumentException(
                "The number of elements in the source ImVectorWrapper<T> is greater than the available space from arrayIndex to the end of the destination array.",
                nameof(array));
        }

        foreach (var i in Enumerable.Range(0, this.Length))
            array.SetValue(this.Data[i], index);
    }

    /// <inheritdoc/>
    bool ICollection<T>.Remove(T item) => this.Remove(in item);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    int IList.Add(object? value)
    {
        this.Add(value is null ? default : (T)value);
        return this.Length - 1;
    }

    /// <inheritdoc/>
    bool IList.Contains(object? value) => this.Contains(value is null ? default : (T)value);

    /// <inheritdoc/>
    int IList.IndexOf(object? value) => this.IndexOf(value is null ? default : (T)value);

    /// <inheritdoc/>
    void IList.Insert(int index, object? value) => this.Insert(index, value is null ? default : (T)value);

    /// <inheritdoc/>
    void IList.Remove(object? value) => this.Remove(value is null ? default : (T)value);

    /// <inheritdoc/>
    int IList<T>.IndexOf(T item) => this.IndexOf(in item);

    /// <inheritdoc/>
    void IList<T>.Insert(int index, T item) => this.Insert(index, in item);

    private int EnsureIndex(int i) => i >= 0 && i < this.Length ? i : throw new IndexOutOfRangeException();
}

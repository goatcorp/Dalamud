using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using JetBrains.Annotations;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Utility methods for <see cref="ImVectorWrapper{T}"/>.
/// </summary>
public static class ImVectorWrapper
{
    /// <summary>
    /// Creates a new instance of the <see cref="ImVectorWrapper{T}"/> struct, initialized with
    /// <paramref name="sourceEnumerable"/>.<br />
    /// You must call <see cref="ImVectorWrapper{T}.Dispose"/> after use.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="sourceEnumerable">The initial data.</param>
    /// <param name="destroyer">The destroyer function to call on item removal.</param>
    /// <param name="minCapacity">The minimum capacity of the new vector.</param>
    /// <returns>The new wrapped vector, that has to be disposed after use.</returns>
    public static ImVectorWrapper<T> CreateFromEnumerable<T>(
        IEnumerable<T> sourceEnumerable,
        ImVectorWrapper<T>.ImGuiNativeDestroyDelegate? destroyer = null,
        int minCapacity = 0)
        where T : unmanaged
    {
        var res = new ImVectorWrapper<T>(0, destroyer);
        try
        {
            switch (sourceEnumerable)
            {
                case T[] c:
                    res.SetCapacity(Math.Max(minCapacity, c.Length + 1));
                    res.LengthUnsafe = c.Length;
                    c.AsSpan().CopyTo(res.DataSpan);
                    break;
                case ICollection c:
                    res.SetCapacity(Math.Max(minCapacity, c.Count + 1));
                    res.AddRange(sourceEnumerable);
                    break;
                case ICollection<T> c:
                    res.SetCapacity(Math.Max(minCapacity, c.Count + 1));
                    res.AddRange(sourceEnumerable);
                    break;
                default:
                    res.SetCapacity(minCapacity);
                    res.AddRange(sourceEnumerable);
                    res.EnsureCapacity(res.LengthUnsafe + 1);
                    break;
            }

            // Null termination
            Debug.Assert(res.LengthUnsafe < res.CapacityUnsafe, "Capacity must be more than source length + 1");
            res.StorageSpan[res.LengthUnsafe] = default;

            return res;
        }
        catch
        {
            res.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ImVectorWrapper{T}"/> struct, initialized with
    /// <paramref name="sourceSpan"/>.<br />
    /// You must call <see cref="ImVectorWrapper{T}.Dispose"/> after use.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="sourceSpan">The initial data.</param>
    /// <param name="destroyer">The destroyer function to call on item removal.</param>
    /// <param name="minCapacity">The minimum capacity of the new vector.</param>
    /// <returns>The new wrapped vector, that has to be disposed after use.</returns>
    public static ImVectorWrapper<T> CreateFromSpan<T>(
        ReadOnlySpan<T> sourceSpan,
        ImVectorWrapper<T>.ImGuiNativeDestroyDelegate? destroyer = null,
        int minCapacity = 0)
        where T : unmanaged
    {
        var res = new ImVectorWrapper<T>(Math.Max(minCapacity, sourceSpan.Length + 1), destroyer);
        try
        {
            res.LengthUnsafe = sourceSpan.Length;
            sourceSpan.CopyTo(res.DataSpan);

            // Null termination
            Debug.Assert(res.LengthUnsafe < res.CapacityUnsafe, "Capacity must be more than source length + 1");
            res.StorageSpan[res.LengthUnsafe] = default;
            return res;
        }
        catch
        {
            res.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Wraps <see cref="ImFontAtlas.ConfigData"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ImFontConfig> ConfigDataWrapped(this ImFontAtlasPtr obj) =>
        obj.NativePtr is null
            ? throw new NullReferenceException()
            : new(&obj.NativePtr->ConfigData, ImGuiNative.ImFontConfig_destroy);

    /// <summary>
    /// Wraps <see cref="ImFontAtlas.Fonts"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ImFontPtr> FontsWrapped(this ImFontAtlasPtr obj) =>
        obj.NativePtr is null
            ? throw new NullReferenceException()
            : new(&obj.NativePtr->Fonts, x => ImGuiNative.ImFont_destroy(x->NativePtr));

    /// <summary>
    /// Wraps <see cref="ImFontAtlas.Textures"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ImFontAtlasTexture> TexturesWrapped(this ImFontAtlasPtr obj) =>
        obj.NativePtr is null
            ? throw new NullReferenceException()
            : new(&obj.NativePtr->Textures);

    /// <summary>
    /// Wraps <see cref="ImFont.Glyphs"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ImGuiHelpers.ImFontGlyphReal> GlyphsWrapped(this ImFontPtr obj) =>
        obj.NativePtr is null
            ? throw new NullReferenceException()
            : new(&obj.NativePtr->Glyphs);

    /// <summary>
    /// Wraps <see cref="ImFont.IndexedHotData"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ImGuiHelpers.ImFontGlyphHotDataReal> IndexedHotDataWrapped(this ImFontPtr obj)
        => obj.NativePtr is null
               ? throw new NullReferenceException()
               : new(&obj.NativePtr->IndexedHotData);

    /// <summary>
    /// Wraps <see cref="ImFont.IndexLookup"/> into a <see cref="ImVectorWrapper{T}"/>.<br />
    /// This does not need to be disposed.
    /// </summary>
    /// <param name="obj">The owner object.</param>
    /// <returns>The wrapped vector.</returns>
    public static unsafe ImVectorWrapper<ushort> IndexLookupWrapped(this ImFontPtr obj) =>
        obj.NativePtr is null
            ? throw new NullReferenceException()
            : new(&obj.NativePtr->IndexLookup);
}

/// <summary>
/// Wrapper for ImVector.
/// </summary>
/// <typeparam name="T">Contained type.</typeparam>
public unsafe struct ImVectorWrapper<T> : IList<T>, IList, IReadOnlyList<T>, IDisposable
    where T : unmanaged
{
    private ImVector* vector;
    private ImGuiNativeDestroyDelegate? destroyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImVectorWrapper{T}"/> struct.<br />
    /// If <paramref name="ownership"/> is set to true, you must call <see cref="Dispose"/> after use,
    /// and the underlying memory for <see cref="ImVector"/> must have been allocated using
    /// <see cref="ImGuiNative.igMemAlloc"/>. Otherwise, it will crash.
    /// </summary>
    /// <param name="vector">The underlying vector.</param>
    /// <param name="destroyer">The destroyer function to call on item removal.</param>
    /// <param name="ownership">Whether this wrapper owns the vector.</param>
    public ImVectorWrapper(
        [NotNull] ImVector* vector,
        ImGuiNativeDestroyDelegate? destroyer = null,
        bool ownership = false)
    {
        if (vector is null)
            throw new ArgumentException($"{nameof(vector)} cannot be null.", nameof(this.vector));

        this.vector = vector;
        this.destroyer = destroyer;
        this.HasOwnership = ownership;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImVectorWrapper{T}"/> struct.<br />
    /// You must call <see cref="Dispose"/> after use.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity.</param>
    /// <param name="destroyer">The destroyer function to call on item removal.</param>
    public ImVectorWrapper(int initialCapacity, ImGuiNativeDestroyDelegate? destroyer = null)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCapacity),
                initialCapacity,
                $"{nameof(initialCapacity)} cannot be a negative number.");
        }

        this.vector = (ImVector*)ImGuiNative.igMemAlloc((uint)sizeof(ImVector));
        if (this.vector is null)
            throw new OutOfMemoryException();
        *this.vector = default;
        this.HasOwnership = true;
        this.destroyer = destroyer;

        try
        {
            this.EnsureCapacity(initialCapacity);
        }
        catch
        {
            ImGuiNative.igMemFree(this.vector);
            this.vector = null;
            this.HasOwnership = false;
            this.destroyer = null;
            throw;
        }
    }

    /// <summary>
    /// Destroy callback for items.
    /// </summary>
    /// <param name="self">Pointer to self.</param>
    public delegate void ImGuiNativeDestroyDelegate(T* self);

    /// <summary>
    /// Gets the raw vector.
    /// </summary>
    public ImVector* RawVector => this.vector;

    /// <summary>
    /// Gets a <see cref="Span{T}"/> view of the underlying ImVector{T}, for the range of <see cref="Length"/>.
    /// </summary>
    public Span<T> DataSpan => new(this.DataUnsafe, this.LengthUnsafe);

    /// <summary>
    /// Gets a <see cref="Span{T}"/> view of the underlying ImVector{T}, for the range of <see cref="Capacity"/>.
    /// </summary>
    public Span<T> StorageSpan => new(this.DataUnsafe, this.CapacityUnsafe);

    /// <summary>
    /// Gets a value indicating whether this <see cref="ImVectorWrapper{T}"/> is disposed.
    /// </summary>
    public bool IsDisposed => this.vector is null;

    /// <summary>
    /// Gets a value indicating whether this <see cref="ImVectorWrapper{T}"/> has the ownership of the underlying
    /// <see cref="ImVector"/>.
    /// </summary>
    public bool HasOwnership { get; private set; }

    /// <summary>
    /// Gets the underlying <see cref="ImVector"/>.
    /// </summary>
    public ImVector* Vector =>
        this.vector is null ? throw new ObjectDisposedException(nameof(ImVectorWrapper<T>)) : this.vector;

    /// <summary>
    /// Gets the number of items contained inside the underlying ImVector{T}.
    /// </summary>
    public int Length => this.LengthUnsafe;

    /// <summary>
    /// Gets the number of items <b>that can be</b> contained inside the underlying ImVector{T}.
    /// </summary>
    public int Capacity => this.CapacityUnsafe;

    /// <summary>
    /// Gets the pointer to the first item in the data inside underlying ImVector{T}.
    /// </summary>
    public T* Data => this.DataUnsafe;

    /// <summary>
    /// Gets the reference to the number of items contained inside the underlying ImVector{T}.
    /// </summary>
    public ref int LengthUnsafe => ref *&this.Vector->Size;

    /// <summary>
    /// Gets the reference to the number of items <b>that can be</b> contained inside the underlying ImVector{T}.
    /// </summary>
    public ref int CapacityUnsafe => ref *&this.Vector->Capacity;

    /// <summary>
    /// Gets the reference to the pointer to the first item in the data inside underlying ImVector{T}.
    /// </summary>
    /// <remarks>This may be null, if <see cref="Capacity"/> is zero.</remarks>
    public ref T* DataUnsafe => ref *(T**)&this.Vector->Data;

    /// <inheritdoc cref="ICollection{T}.IsReadOnly"/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    int ICollection.Count => this.LengthUnsafe;

    /// <inheritdoc/>
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc/>
    object ICollection.SyncRoot { get; } = new();

    /// <inheritdoc/>
    int ICollection<T>.Count => this.LengthUnsafe;

    /// <inheritdoc/>
    int IReadOnlyCollection<T>.Count => this.LengthUnsafe;

    /// <inheritdoc/>
    bool IList.IsFixedSize => false;

    /// <summary>
    /// Gets the element at the specified index as a reference.
    /// </summary>
    /// <param name="index">Index of the item.</param>
    /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is out of range.</exception>
    public ref T this[int index] => ref this.DataUnsafe[this.EnsureIndex(index)];

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
    public void Dispose()
    {
        if (this.HasOwnership)
        {
            this.Clear();
            this.SetCapacity(0);
            Debug.Assert(this.vector->Data == 0, "SetCapacity(0) did not free the data");
            ImGuiNative.igMemFree(this.vector);
        }

        this.vector = null;
        this.HasOwnership = false;
        this.destroyer = null;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var i in Enumerable.Range(0, this.LengthUnsafe))
            yield return this[i];
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(in T item)
    {
        this.EnsureCapacityExponential(this.LengthUnsafe + 1);
        this.DataUnsafe[this.LengthUnsafe++] = item;
    }

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(IEnumerable<T> items)
    {
        if (items is ICollection { Count: var count })
            this.EnsureCapacityExponential(this.LengthUnsafe + count);

        foreach (var item in items)
            this.Add(item);
    }

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(ReadOnlySpan<T> items)
    {
        this.EnsureCapacityExponential(this.LengthUnsafe + items.Length);
        foreach (var item in items)
            this.Add(item);
    }

    /// <inheritdoc cref="ICollection{T}.Clear"/>
    public void Clear() => this.Clear(false);

    /// <summary>
    /// Clears this vector, optionally skipping destroyer invocation.
    /// </summary>
    /// <param name="skipDestroyer">Whether to skip destroyer invocation.</param>
    public void Clear(bool skipDestroyer)
    {
        if (this.destroyer != null && !skipDestroyer)
        {
            foreach (var i in Enumerable.Range(0, this.LengthUnsafe))
                this.destroyer(&this.DataUnsafe[i]);
        }

        this.LengthUnsafe = 0;
    }

    /// <inheritdoc cref="ICollection{T}.Contains"/>
    public bool Contains(in T item) => this.IndexOf(in item) != -1;

    /// <summary>
    /// Size down the underlying ImVector{T}.
    /// </summary>
    /// <param name="reservation">Capacity to reserve.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    public bool Compact(int reservation) => this.SetCapacity(Math.Max(reservation, this.LengthUnsafe));

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

        if (array.Length - arrayIndex < this.LengthUnsafe)
        {
            throw new ArgumentException(
                "The number of elements in the source ImVectorWrapper<T> is greater than the available space from arrayIndex to the end of the destination array.",
                nameof(array));
        }

        fixed (void* p = array)
            Buffer.MemoryCopy(this.DataUnsafe, p, this.LengthUnsafe * sizeof(T), this.LengthUnsafe * sizeof(T));
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.<br />
    /// On growth, the new capacity exactly matches <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    public bool EnsureCapacity(int capacity) => this.CapacityUnsafe < capacity && this.SetCapacity(capacity);

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.<br />
    /// On growth, the new capacity may exceed <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>Whether the capacity has been changed.</returns>
    public bool EnsureCapacityExponential(int capacity)
        => this.EnsureCapacity(1 << ((sizeof(int) * 8) - BitOperations.LeadingZeroCount((uint)capacity)));

    /// <summary>
    /// Resizes the underlying array and fills with zeroes if grown.
    /// </summary>
    /// <param name="size">New size.</param>
    /// <param name="defaultValue">New default value.</param>
    /// <param name="skipDestroyer">Whether to skip calling destroyer function.</param>
    public void Resize(int size, in T defaultValue = default, bool skipDestroyer = false)
    {
        this.EnsureCapacity(size);
        var old = this.LengthUnsafe;
        if (old > size && !skipDestroyer && this.destroyer is not null)
        {
            foreach (var v in this.DataSpan[size..])
                this.destroyer(&v);
        }

        this.LengthUnsafe = size;
        if (old < size)
            this.DataSpan[old..].Fill(defaultValue);
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
        foreach (var i in Enumerable.Range(0, this.LengthUnsafe))
        {
            if (Equals(item, this.DataUnsafe[i]))
                return i;
        }

        return -1;
    }

    /// <inheritdoc cref="IList{T}.Insert"/>
    public void Insert(int index, in T item)
    {
        // Note: index == this.LengthUnsafe is okay; we're just adding to the end then
        if (index < 0 || index > this.LengthUnsafe)
            throw new IndexOutOfRangeException();

        this.EnsureCapacityExponential(this.LengthUnsafe + 1);
        var num = this.LengthUnsafe - index;
        Buffer.MemoryCopy(this.DataUnsafe + index, this.DataUnsafe + index + 1, num * sizeof(T), num * sizeof(T));
        this.DataUnsafe[index] = item;
        this.LengthUnsafe += 1;
    }

    /// <inheritdoc cref="List{T}.InsertRange"/>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items is ICollection { Count: var count })
        {
            this.EnsureCapacityExponential(this.LengthUnsafe + count);
            var num = this.LengthUnsafe - index;
            Buffer.MemoryCopy(this.DataUnsafe + index, this.DataUnsafe + index + count, num * sizeof(T), num * sizeof(T));
            foreach (var item in items)
                this.DataUnsafe[index++] = item;
            this.LengthUnsafe += count;
        }
        else
        {
            foreach (var item in items)
                this.Insert(index++, item);
        }
    }

    /// <inheritdoc cref="List{T}.InsertRange"/>
    public void InsertRange(int index, ReadOnlySpan<T> items)
    {
        this.EnsureCapacityExponential(this.LengthUnsafe + items.Length);
        var num = this.LengthUnsafe - index;
        Buffer.MemoryCopy(this.DataUnsafe + index, this.DataUnsafe + index + items.Length, num * sizeof(T), num * sizeof(T));
        foreach (var item in items)
            this.DataUnsafe[index++] = item;
        this.LengthUnsafe += items.Length;
    }

    /// <summary>
    /// Removes the element at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="skipDestroyer">Whether to skip calling the destroyer function.</param>
    public void RemoveAt(int index, bool skipDestroyer = false) => this.RemoveRange(index, 1, skipDestroyer);

    /// <inheritdoc/>
    void IList<T>.RemoveAt(int index) => this.RemoveAt(index);

    /// <inheritdoc/>
    void IList.RemoveAt(int index) => this.RemoveAt(index);

    /// <summary>
    /// Removes <paramref name="count"/> elements at the given index.
    /// </summary>
    /// <param name="index">The index of the first item to remove.</param>
    /// <param name="count">Number of items to remove.</param>
    /// <param name="skipDestroyer">Whether to skip calling the destroyer function.</param>
    public void RemoveRange(int index, int count, bool skipDestroyer = false)
    {
        this.EnsureIndex(index);
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Must be positive.");
        if (count == 0)
            return;

        if (!skipDestroyer && this.destroyer is { } d)
        {
            for (var i = 0; i < count; i++)
                d(this.DataUnsafe + index + i);
        }

        var numItemsToMove = this.LengthUnsafe - index - count;
        var numBytesToMove = numItemsToMove * sizeof(T);
        Buffer.MemoryCopy(this.DataUnsafe + index + count, this.DataUnsafe + index, numBytesToMove, numBytesToMove);
        this.LengthUnsafe -= count;
    }

    /// <summary>
    /// Replaces a sequence at given offset <paramref name="index"/> of <paramref name="count"/> items with
    /// <paramref name="replacement"/>.
    /// </summary>
    /// <param name="index">The index of the first item to be replaced.</param>
    /// <param name="count">The number of items to be replaced.</param>
    /// <param name="replacement">The replacement.</param>
    /// <param name="skipDestroyer">Whether to skip calling the destroyer function.</param>
    public void ReplaceRange(int index, int count, ReadOnlySpan<T> replacement, bool skipDestroyer = false)
    {
        this.EnsureIndex(index);
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Must be positive.");
        if (count == 0)
            return;

        // Ensure the capacity first, so that we can safely destroy the items first.
        this.EnsureCapacityExponential((this.LengthUnsafe + replacement.Length) - count);

        if (!skipDestroyer && this.destroyer is { } d)
        {
            for (var i = 0; i < count; i++)
                d(this.DataUnsafe + index + i);
        }

        if (count == replacement.Length)
        {
            replacement.CopyTo(this.DataSpan[index..]);
        }
        else if (count > replacement.Length)
        {
            replacement.CopyTo(this.DataSpan[index..]);
            this.RemoveRange(index + replacement.Length, count - replacement.Length);
        }
        else
        {
            replacement[..count].CopyTo(this.DataSpan[index..]);
            this.InsertRange(index + count, replacement[count..]);
        }
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
        if (capacity < this.LengthUnsafe)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);

        if (capacity == this.LengthUnsafe)
        {
            if (capacity == 0 && this.DataUnsafe is not null)
            {
                ImGuiNative.igMemFree(this.DataUnsafe);
                this.DataUnsafe = null;
            }

            return false;
        }

        var oldAlloc = this.DataUnsafe;
        var oldSpan = new Span<T>(oldAlloc, this.CapacityUnsafe);

        var newAlloc = (T*)(capacity == 0
                                ? null
                                : ImGuiNative.igMemAlloc(checked((uint)(capacity * sizeof(T)))));

        if (newAlloc is null && capacity > 0)
            throw new OutOfMemoryException();

        var newSpan = new Span<T>(newAlloc, capacity);

        if (!oldSpan.IsEmpty && !newSpan.IsEmpty)
            oldSpan[..this.LengthUnsafe].CopyTo(newSpan);

        if (oldAlloc != null)
            ImGuiNative.igMemFree(oldAlloc);

        this.DataUnsafe = newAlloc;
        this.CapacityUnsafe = capacity;

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

        if (array.Length - index < this.LengthUnsafe)
        {
            throw new ArgumentException(
                "The number of elements in the source ImVectorWrapper<T> is greater than the available space from arrayIndex to the end of the destination array.",
                nameof(array));
        }

        foreach (var i in Enumerable.Range(0, this.LengthUnsafe))
            array.SetValue(this.DataUnsafe[i], index);
    }

    /// <inheritdoc/>
    bool ICollection<T>.Remove(T item) => this.Remove(in item);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    int IList.Add(object? value)
    {
        this.Add(value is null ? default : (T)value);
        return this.LengthUnsafe - 1;
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

    private int EnsureIndex(int i) => i >= 0 && i < this.LengthUnsafe ? i : throw new IndexOutOfRangeException();
}

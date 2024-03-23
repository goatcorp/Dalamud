using System.Collections;
using System.Collections.Generic;

namespace Dalamud.Utility.Enumeration;

/// <summary>Range enumerator for <see cref="IReadOnlyList{T}"/>.</summary>
/// <typeparam name="T">The element type.</typeparam>
public struct ListRangeEnumerator<T> : IEnumerator<T>
{
    private readonly IReadOnlyList<T> items;
    private readonly int firstIndex;
    private readonly int count;
    private int index;

    /// <summary>Initializes a new instance of the <see cref="ListRangeEnumerator{T}"/> struct.</summary>
    /// <param name="items">The list of items.</param>
    /// <param name="firstIndex">The first index.</param>
    /// <param name="count">The number of items to enumerate.</param>
    public ListRangeEnumerator(IReadOnlyList<T> items, int firstIndex, int count)
    {
        this.items = items;
        this.firstIndex = firstIndex;
        this.count = count - firstIndex;
        this.index = -1;
        if (this.firstIndex > items.Count || this.firstIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(firstIndex), firstIndex, null);
        if (this.firstIndex + count > items.Count || count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, null);
    }

    /// <summary>Initializes a new instance of the <see cref="ListRangeEnumerator{T}"/> struct.</summary>
    /// <param name="items">The list of items.</param>
    /// <param name="range">The range.</param>
    public ListRangeEnumerator(IReadOnlyList<T> items, Range range)
    {
        this.items = items;
        (this.firstIndex, this.count) = range.GetOffsetAndLength(items.Count);
        this.index = -1;
    }

    /// <inheritdoc/>
    public T? Current =>
        this.index < 0 || this.index >= this.count
            ? throw new InvalidOperationException()
            : this.items[this.firstIndex + this.index];

    /// <inheritdoc/>
    object? IEnumerator.Current => this.Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (this.index >= this.count - 1)
            return false;
        this.index++;
        return true;
    }

    /// <inheritdoc/>
    public void Reset() => this.index = -1;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}

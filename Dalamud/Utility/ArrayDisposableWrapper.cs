using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Utility;

/// <summary>
/// Wrapper for an array of T where T is an <see cref="IDisposable"/>.
/// </summary>
/// <typeparam name="T">The inner type.</typeparam>
public sealed class ArrayDisposableWrapper<T> : IReadOnlyList<T>, IDisposable where T : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDisposableWrapper{T}"/> class.
    /// </summary>
    /// <param name="backing">The underlying list.</param>
    public ArrayDisposableWrapper(T[] backing) => this.Backing = backing;

    /// <summary>
    /// Gets the underlying array.
    /// </summary>
    public T[] Backing { get; }

    /// <inheritdoc/>
    public int Count => this.Backing.Length;

    /// <inheritdoc/>
    public T this[int index] => this.Backing[index];

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var x in this.Backing)
            x.Dispose();
    }

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public IEnumerator<T> GetEnumerator() => this.Backing.AsEnumerable().GetEnumerator();

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => this.Backing.GetEnumerator();
}

/// <summary>
/// Utility functions for <see cref="ArrayDisposableWrapper{T}"/>.
/// </summary>
public static class ArrayDisposableWrapper
{
    /// <summary>
    /// Wrap an array with disposable elements with a new <see cref="ArrayDisposableWrapper{T}"/>.
    /// </summary>
    /// <param name="array">The underlying array.</param>
    /// <typeparam name="T">Type of element.</typeparam>
    /// <returns>Wrapped array.</returns>
    public static ArrayDisposableWrapper<T> WrapDisposableElements<T>(this T[] array) where T : IDisposable
        => new(array);
}

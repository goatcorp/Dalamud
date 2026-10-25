using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Compares arrays by their items, and looks them up by a span of the items without making an array.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
internal sealed class SpanSequenceComparer<T> : IEqualityComparer<T[]>, IAlternateEqualityComparer<ReadOnlySpan<T>, T[]>
    where T : unmanaged, IEquatable<T>
{
    /// <summary>The shared instance.</summary>
    public static readonly SpanSequenceComparer<T> Instance = new();

    /// <inheritdoc/>
    public bool Equals(T[]? x, T[]? y) => x.AsSpan().SequenceEqual(y);

    /// <inheritdoc/>
    public int GetHashCode(T[] obj) => this.GetHashCode(obj.AsSpan());

    /// <inheritdoc/>
    public bool Equals(ReadOnlySpan<T> alternate, T[] other) => alternate.SequenceEqual(other);

    /// <inheritdoc/>
    public int GetHashCode(ReadOnlySpan<T> alternate)
    {
        HashCode h = default;
        h.AddBytes(MemoryMarshal.AsBytes(alternate));
        return h.ToHashCode();
    }

    /// <inheritdoc/>
    public T[] Create(ReadOnlySpan<T> alternate) => alternate.ToArray();
}

using System.Collections.Immutable;
using System.Linq;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>An immutable array compared by its items.</summary>
/// <param name="array">The array. A default one is empty.</param>
/// <typeparam name="T">The type of the items.</typeparam>
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>
{
    /// <summary>Gets the array, never default.</summary>
    public ImmutableArray<T> Array => array.IsDefault ? [] : array;

    /// <inheritdoc/>
    public bool Equals(EquatableArray<T> other) => this.Array.SequenceEqual(other.Array);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hc = default(HashCode);
        foreach (var item in this.Array)
            hc.Add(item);
        return hc.ToHashCode();
    }
}

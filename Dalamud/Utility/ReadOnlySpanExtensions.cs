namespace Dalamud.Utility;

/// <summary>
/// Extension methods for <see cref="ReadOnlySpan{T}"/>.
/// </summary>
public static class ReadOnlySpanExtensions
{
    /// <summary>
    /// Returns a slice of the span containing all elements up to, but not including, the first occurrence of the default value for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements in the span, which must implement <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="span">The target read-only span to slice.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> ending before the first occurrence of <c>default(T)</c>; otherwise, the original <paramref name="span"/> if no default value is found.
    /// </returns>
    public static ReadOnlySpan<T> BeforeNull<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
    {
        var pos = span.IndexOf(default(T));
        return pos >= 0 ? span[..pos] : span;
    }
}

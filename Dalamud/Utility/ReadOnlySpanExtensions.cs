namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension methods for <see cref="ReadOnlySpan{T}"/>.
/// </summary>
public static class ReadOnlySpanExtensions
{
    extension<T>(ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// Returns a slice of the span containing all elements up to, but not including, the first occurrence of the default value for <typeparamref name="T"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="ReadOnlySpan{T}"/> ending before the first occurrence of <c>default(T)</c>; otherwise, the original <paramref name="span"/> if no default value is found.
        /// </returns>
        public ReadOnlySpan<T> BeforeNull()
        {
            var pos = span.IndexOf(default(T));
            return pos >= 0 ? span[..pos] : span;
        }
    }
}

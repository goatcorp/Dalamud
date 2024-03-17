using Dalamud.Utility.Text;

namespace Dalamud.Utility;

/// <summary>Extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>.</summary>
public static class SpanExtensions
{
    /// <summary>Creates an enumerable that can be used to iterate unicode codepoints in a span.</summary>
    /// <param name="span">The span to reinterpret.</param>
    /// <returns>The enumerable/enumerator-like ref struct.</returns>
    public static Utf8SpanEnumerator AsUtf8Enumerable(this ReadOnlySpan<byte> span) => new(span);

    /// <inheritdoc cref="AsUtf8Enumerable(ReadOnlySpan{byte})"/>
    public static Utf8SpanEnumerator AsUtf8Enumerable(this Span<byte> span) => new(span);
}

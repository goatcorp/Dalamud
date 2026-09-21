using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for <see cref="ReadOnlySeString"/>.
/// </summary>
public static class ReadOnlySeStringExtensions
{
    /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.IsTextOnly(ReadOnlySeStringSpan)"/>
    public static bool IsTextOnly(this ReadOnlySeString input)
    {
        return input.AsSpan().IsTextOnly();
    }

    /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.ContainsText(ReadOnlySeStringSpan, ReadOnlySpan{byte})"/>
    public static bool ContainsText(this ReadOnlySeString input, ReadOnlySpan<byte> needle)
    {
        return input.AsSpan().ContainsText(needle);
    }

    /// <inheritdoc cref="ReadOnlySeStringSpanExtensions.ContainsText(ReadOnlySeStringSpan, ReadOnlySpan{char})"/>
    public static bool ContainsText(this ReadOnlySeString input, ReadOnlySpan<char> needle)
    {
        return input.AsSpan().ContainsText(needle);
    }
}

using System.Buffers;
using System.Text;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for <see cref="ReadOnlySeStringSpan"/>.
/// </summary>
public static class ReadOnlySeStringSpanExtensions
{
    /// <summary>
    /// Determines whether the string is plain text, without any macros.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns><see langword="true" /> if the string contains only plain text, <see langword="false"/> otherwise.</returns>
    public static bool IsTextOnly(this ReadOnlySeStringSpan input)
    {
        foreach (var payload in input)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the string contains the specified plain text.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><see langword="true" /> if the text is found, <see langword="false"/> otherwise.</returns>
    public static bool ContainsText(this ReadOnlySeStringSpan input, ReadOnlySpan<byte> needle)
    {
        foreach (var payload in input)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                continue;

            if (payload.Body.IndexOf(needle) != -1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the string contains the specified plain text.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><see langword="true" /> if the text is found, <see langword="false"/> otherwise.</returns>
    public static bool ContainsText(this ReadOnlySeStringSpan input, ReadOnlySpan<char> needle)
    {
        if (needle.IsEmpty)
            return true;

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(needle.Length);
        if (maxByteCount <= 256)
        {
            Span<byte> byteBuffer = stackalloc byte[maxByteCount];
            var actualBytes = System.Text.Encoding.UTF8.GetBytes(needle, byteBuffer);
            return input.ContainsText(byteBuffer[..actualBytes]);
        }

        var rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var actualBytes = System.Text.Encoding.UTF8.GetBytes(needle, rented);
            return input.ContainsText(rented.AsSpan(0, actualBytes));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}

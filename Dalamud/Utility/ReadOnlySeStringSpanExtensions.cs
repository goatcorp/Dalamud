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

    /// <summary>
    /// Replaces occurrences of a specified text in a <see cref="ReadOnlySeString"/> with another text.
    /// </summary>
    /// <param name="input">The original string.</param>
    /// <param name="toFind">The text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>A new <see cref="ReadOnlySeString"/> with the replacements made.</returns>
    public static ReadOnlySeString ReplaceText(this ReadOnlySeStringSpan input, ReadOnlySpan<byte> toFind, ReadOnlySpan<byte> replacement)
    {
        if (input.IsEmpty)
            return new ReadOnlySeString(input);

        using var rssb = new RentedSeStringBuilder();

        foreach (var payload in input)
        {
            if (payload.Type == ReadOnlySePayloadType.Invalid)
                continue;

            if (payload.Type != ReadOnlySePayloadType.Text)
            {
                rssb.Append(payload);
                continue;
            }

            var index = payload.Body.IndexOf(toFind);
            if (index == -1)
            {
                rssb.Append(payload);
                continue;
            }

            var lastIndex = 0;
            while (index != -1)
            {
                rssb.Append(payload.Body[lastIndex..index]);

                if (!replacement.IsEmpty)
                {
                    rssb.Append(replacement);
                }

                lastIndex = index + toFind.Length;
                index = payload.Body[lastIndex..].IndexOf(toFind);

                if (index != -1)
                    index += lastIndex;
            }

            rssb.Append(payload.Body[lastIndex..]);
        }

        return rssb.ToReadOnlySeString();
    }
}

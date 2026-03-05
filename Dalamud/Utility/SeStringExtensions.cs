using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for SeStrings.
/// </summary>
public static class SeStringExtensions
{
    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeString"/> contains only text payloads.
    /// </summary>
    /// <param name="ross">The <see cref="ReadOnlySeString"/> to check.</param>
    /// <returns><c>true</c> if the string contains only text payloads; otherwise, <c>false</c>.</returns>
    public static bool IsTextOnly(this ReadOnlySeString ross)
    {
        return ross.AsSpan().IsTextOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeStringSpan"/> contains only text payloads.
    /// </summary>
    /// <param name="rosss">The <see cref="ReadOnlySeStringSpan"/> to check.</param>
    /// <returns><c>true</c> if the span contains only text payloads; otherwise, <c>false</c>.</returns>
    public static bool IsTextOnly(this ReadOnlySeStringSpan rosss)
    {
        foreach (var payload in rosss)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeString"/> contains the specified text.
    /// </summary>
    /// <param name="ross">The <see cref="ReadOnlySeString"/> to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this ReadOnlySeString ross, ReadOnlySpan<byte> needle)
    {
        return ross.AsSpan().ContainsText(needle);
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeStringSpan"/> contains the specified text.
    /// </summary>
    /// <param name="rosss">The <see cref="ReadOnlySeStringSpan"/> to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this ReadOnlySeStringSpan rosss, ReadOnlySpan<byte> needle)
    {
        foreach (var payload in rosss)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                continue;

            if (payload.Body.IndexOf(needle) != -1)
                return true;
        }

        return false;
    }
}

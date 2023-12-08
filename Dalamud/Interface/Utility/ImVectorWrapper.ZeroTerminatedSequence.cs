using System.Numerics;
using System.Text;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Utility methods for <see cref="ImVectorWrapper{T}"/>.
/// </summary>
public static partial class ImVectorWrapper
{
    /// <summary>
    /// Appends <paramref name="buf"/> from <paramref name="psz"/>, a zero terminated sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="buf">The target buffer.</param>
    /// <param name="psz">The pointer to the zero-terminated sequence.</param>
    public static unsafe void AppendZeroTerminatedSequence<T>(this ref ImVectorWrapper<T> buf, T* psz)
        where T : unmanaged, INumber<T>
    {
        var len = 0;
        while (psz[len] != default)
            len++;

        buf.AddRange(new Span<T>(psz, len));
    }

    /// <summary>
    /// Sets <paramref name="buf"/> from <paramref name="psz"/>, a zero terminated sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="buf">The target buffer.</param>
    /// <param name="psz">The pointer to the zero-terminated sequence.</param>
    public static unsafe void SetFromZeroTerminatedSequence<T>(this ref ImVectorWrapper<T> buf, T* psz)
        where T : unmanaged, INumber<T>
    {
        buf.Clear();
        buf.AppendZeroTerminatedSequence(psz);
    }

    /// <summary>
    /// Trims zero terminator(s).
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="buf">The buffer.</param>
    public static void TrimZeroTerminator<T>(this ref ImVectorWrapper<T> buf)
        where T : unmanaged, INumber<T>
    {
        ref var len = ref buf.LengthUnsafe;
        while (len > 0 && buf[len - 1] == default)
            len--;
    }

    /// <summary>
    /// Adds a zero terminator to the buffer, if missing.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="buf">The buffer.</param>
    public static void AddZeroTerminatorIfMissing<T>(this ref ImVectorWrapper<T> buf)
        where T : unmanaged, INumber<T>
    {
        if (buf.Length > 0 && buf[^1] == default)
            return;
        buf.Add(default);
    }

    /// <summary>
    /// Gets the codepoint at the given offset.
    /// </summary>
    /// <param name="buf">The buffer containing bytes in UTF-8.</param>
    /// <param name="offset">The offset in bytes.</param>
    /// <param name="numBytes">Number of bytes occupied by the character, invalid or not.</param>
    /// <param name="invalid">The fallback character, if no valid UTF-8 character could be found.</param>
    /// <returns>The parsed codepoint, or <paramref name="invalid"/> if it could not be parsed correctly.</returns>
    public static unsafe int Utf8GetCodepoint(
        this in ImVectorWrapper<byte> buf,
        int offset,
        out int numBytes,
        int invalid = 0xFFFD)
    {
        var cb = buf.LengthUnsafe - offset;
        if (cb <= 0)
        {
            numBytes = 0;
            return invalid;
        }

        numBytes = 1;

        var b = buf.DataUnsafe + offset;
        if ((b[0] & 0x80) == 0)
            return b[0];

        if (cb < 2 || (b[1] & 0xC0) != 0x80)
            return invalid;
        if ((b[0] & 0xE0) == 0xC0)
        {
            numBytes = 2;
            return ((b[0] & 0x1F) << 6) | (b[1] & 0x3F);
        }

        if (cb < 3 || (b[2] & 0xC0) != 0x80)
            return invalid;
        if ((b[0] & 0xF0) == 0xE0)
        {
            numBytes = 3;
            return ((b[0] & 0x0F) << 12) | ((b[1] & 0x3F) << 6) | (b[2] & 0x3F);
        }

        if (cb < 4 || (b[3] & 0xC0) != 0x80)
            return invalid;
        if ((b[0] & 0xF8) == 0xF0)
        {
            numBytes = 4;
            return ((b[0] & 0x07) << 18) | ((b[1] & 0x3F) << 12) | ((b[2] & 0x3F) << 6) | (b[3] & 0x3F);
        }

        return invalid;
    }

    /// <summary>
    /// Normalizes the given UTF-8 string.<br />
    /// Using the default values will ensure the best interop between the game, ImGui, and Windows.
    /// </summary>
    /// <param name="buf">The buffer containing bytes in UTF-8.</param>
    /// <param name="lineEnding">The replacement line ending. If empty, CR LF will be used.</param>
    /// <param name="invalidChar">The replacement invalid character. If empty, U+FFFD REPLACEMENT CHARACTER will be used.</param>
    /// <param name="normalizeLineEndings">Specify whether to normalize the line endings.</param>
    /// <param name="sanitizeInvalidCharacters">Specify whether to replace invalid characters.</param>
    /// <param name="sanitizeNonUcs2Characters">Specify whether to replace characters that requires the use of surrogate, when encoded in UTF-16.</param>
    /// <param name="sanitizeSurrogates">Specify whether to make sense out of WTF-8.</param>
    public static unsafe void Utf8Normalize(
        this ref ImVectorWrapper<byte> buf,
        ReadOnlySpan<byte> lineEnding = default,
        ReadOnlySpan<byte> invalidChar = default,
        bool normalizeLineEndings = true,
        bool sanitizeInvalidCharacters = true,
        bool sanitizeNonUcs2Characters = true,
        bool sanitizeSurrogates = true)
    {
        if (lineEnding.IsEmpty)
            lineEnding = "\r\n"u8;
        if (invalidChar.IsEmpty)
            invalidChar = "\uFFFD"u8;

        // Ensure an implicit null after the end of the string.
        buf.EnsureCapacity(buf.Length + 1);
        buf.StorageSpan[buf.Length] = 0;

        Span<char> charsBuf = stackalloc char[2];
        Span<byte> bytesBuf = stackalloc byte[4];
        for (var i = 0; i < buf.Length;)
        {
            var c1 = buf.Utf8GetCodepoint(i, out var cb, -1);
            switch (c1)
            {
                // Note that buf.Data[i + 1] is always defined. See the beginning of the function.
                case '\r' when buf.Data[i + 1] == '\n':
                    // If it's already CR LF, it passes all filters.
                    i += 2;
                    break;

                case >= 0xD800 and <= 0xDFFF when sanitizeSurrogates:
                {
                    var c2 = buf.Utf8GetCodepoint(i + cb, out var cb2);
                    if (c1 is < 0xD800 or >= 0xDC00)
                        goto case -2;
                    if (c2 is < 0xDC00 or >= 0xE000)
                        goto case -2;
                    charsBuf[0] = unchecked((char)c1);
                    charsBuf[1] = unchecked((char)c2);
                    var bytesLen = Encoding.UTF8.GetBytes(charsBuf, bytesBuf);
                    buf.ReplaceRange(i, cb + cb2, bytesBuf[..bytesLen]);
                    // Do not alter i; now that the WTF-8 has been dealt with, apply other filters.
                    break;
                }

                case -2:
                case -1 or 0xFFFE or 0xFFFF when sanitizeInvalidCharacters:
                case >= 0xD800 and <= 0xDFFF when sanitizeInvalidCharacters:
                case > char.MaxValue when sanitizeNonUcs2Characters:
                {
                    buf.ReplaceRange(i, cb, invalidChar);
                    i += invalidChar.Length;
                    break;
                }

                // See String.Manipulation.cs: IndexOfNewlineChar.
                // CR; Carriage Return
                // LF; Line Feed
                // FF; Form Feed
                // NEL; Next Line
                // LS; Line Separator
                // PS; Paragraph Separator
                case '\r' or '\n' or '\f' or '\u0085' or '\u2028' or '\u2029' when normalizeLineEndings:
                {
                    buf.ReplaceRange(i, cb, lineEnding);
                    i += lineEnding.Length;
                    break;
                }

                default:
                    i += cb;
                    break;
            }
        }
    }
}

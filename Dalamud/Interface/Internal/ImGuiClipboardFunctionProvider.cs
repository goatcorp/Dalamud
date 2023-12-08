using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Configures the ImGui clipboard behaviour to work nicely with XIV.
/// </summary>
/// <remarks>
/// <para>
/// XIV uses '\r' for line endings and will truncate all text after a '\n' character.
/// This means that copy/pasting multi-line text from ImGui to XIV will only copy the first line.
/// </para>
/// <para>
/// ImGui uses '\n' for line endings and will ignore '\r' entirely.
/// This means that copy/pasting multi-line text from XIV to ImGui will copy all the text
/// without line breaks.
/// </para>
/// <para>
/// To fix this we normalize all clipboard line endings entering/exiting ImGui to '\r\n' which
/// works for both ImGui and XIV.
/// </para>
/// </remarks>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ImGuiClipboardFunctionProvider : IServiceType, IDisposable
{
    private readonly nint clipboardUserDataOriginal;
    private readonly delegate* unmanaged<nint, byte*, void> setTextOriginal;
    private readonly delegate* unmanaged<nint, byte*> getTextOriginal;
    private GCHandle clipboardUserData;

    [ServiceManager.ServiceConstructor]
    private ImGuiClipboardFunctionProvider(InterfaceManager.InterfaceManagerWithScene imws)
    {
        // Effectively waiting for ImGui to become available.
        _ = imws;
        Debug.Assert(ImGuiHelpers.IsImGuiInitialized, "IMWS initialized but IsImGuiInitialized is false?");

        var io = ImGui.GetIO();
        this.setTextOriginal = (delegate* unmanaged<nint, byte*, void>)io.SetClipboardTextFn;
        this.getTextOriginal = (delegate* unmanaged<nint, byte*>)io.GetClipboardTextFn;
        this.clipboardUserDataOriginal = io.ClipboardUserData;
        io.SetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*, void>)(&StaticSetClipboardTextImpl);
        io.GetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*>)&StaticGetClipboardTextImpl;
        io.ClipboardUserData = GCHandle.ToIntPtr(this.clipboardUserData = GCHandle.Alloc(this));
        return;

        [UnmanagedCallersOnly]
        static void StaticSetClipboardTextImpl(nint userData, byte* text) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.SetClipboardTextImpl(text);

        [UnmanagedCallersOnly]
        static byte* StaticGetClipboardTextImpl(nint userData) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.GetClipboardTextImpl();
    }

    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "If it's null, it's crashworthy")]
    private static ImVectorWrapper<byte> ImGuiCurrentContextClipboardHandlerData =>
        new((ImVector*)(ImGui.GetCurrentContext() + 0x5520));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.clipboardUserData.IsAllocated)
            return;

        var io = ImGui.GetIO();
        io.SetClipboardTextFn = (nint)this.setTextOriginal;
        io.GetClipboardTextFn = (nint)this.getTextOriginal;
        io.ClipboardUserData = this.clipboardUserDataOriginal;

        this.clipboardUserData.Free();
    }

    private void SetClipboardTextImpl(byte* text)
    {
        var buffer = ImGuiCurrentContextClipboardHandlerData;
        Utf8Utils.SetFromNullTerminatedBytes(ref buffer, text);
        Utf8Utils.Normalize(ref buffer);
        Utf8Utils.AddNullTerminatorIfMissing(ref buffer);
        this.setTextOriginal(this.clipboardUserDataOriginal, buffer.Data);
    }

    private byte* GetClipboardTextImpl()
    {
        _ = this.getTextOriginal(this.clipboardUserDataOriginal);

        var buffer = ImGuiCurrentContextClipboardHandlerData;
        Utf8Utils.TrimNullTerminator(ref buffer);
        Utf8Utils.Normalize(ref buffer);
        Utf8Utils.AddNullTerminatorIfMissing(ref buffer);
        return buffer.Data;
    }

    private static class Utf8Utils
    {
        /// <summary>
        /// Sets <paramref name="buf"/> from <paramref name="psz"/>, a null terminated UTF-8 string.
        /// </summary>
        /// <param name="buf">The target buffer. It will not contain a null terminator.</param>
        /// <param name="psz">The pointer to the null-terminated UTF-8 string.</param>
        public static void SetFromNullTerminatedBytes(ref ImVectorWrapper<byte> buf, byte* psz)
        {
            var len = 0;
            while (psz[len] != 0)
                len++;

            buf.Clear();
            buf.AddRange(new Span<byte>(psz, len));
        }

        /// <summary>
        /// Removes the null terminator.
        /// </summary>
        /// <param name="buf">The UTF-8 string buffer.</param>
        public static void TrimNullTerminator(ref ImVectorWrapper<byte> buf)
        {
            while (buf.Length > 0 && buf[^1] == 0)
                buf.LengthUnsafe--;
        }
        
        /// <summary>
        /// Adds a null terminator to the buffer.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        public static void AddNullTerminatorIfMissing(ref ImVectorWrapper<byte> buf)
        {
            if (buf.Length > 0 && buf[^1] == 0)
                return;
            buf.Add(0);
        }

        /// <summary>
        /// Counts the number of bytes for the UTF-8 character.
        /// </summary>
        /// <param name="b">The bytes.</param>
        /// <param name="avail">Available number of bytes.</param>
        /// <returns>Number of bytes taken, or -1 if the byte was invalid.</returns>
        public static int CountBytes(byte* b, int avail)
        {
            if (avail <= 0)
                return 0;
            if ((b[0] & 0x80) == 0)
                return 1;
            if ((b[0] & 0xE0) == 0xC0 && avail >= 2)
                return (b[1] & 0xC0) == 0x80 ? 2 : -1;
            if ((b[0] & 0xF0) == 0xE0 && avail >= 3)
                return (b[1] & 0xC0) == 0x80 && (b[2] & 0xC0) == 0x80 ? 3 : -1;
            if ((b[0] & 0xF8) == 0xF0 && avail >= 4)
                return (b[1] & 0xC0) == 0x80 && (b[2] & 0xC0) == 0x80 && (b[3] & 0xC0) == 0x80 ? 4 : -1;
            return -1;
        }

        /// <summary>
        /// Gets the codepoint.
        /// </summary>
        /// <param name="b">The bytes.</param>
        /// <param name="cb">The result from <see cref="CountBytes"/>.</param>
        /// <returns>The codepoint, or \xFFFD replacement character if failed.</returns>
        public static int GetCodepoint(byte* b, int cb) => cb switch
        {
            1 => b[0],
            2 => ((b[0] & 0x1F) << 6) | (b[1] & 0x3F),
            3 => ((b[0] & 0x0F) << 12) | ((b[1] & 0x3F) << 6) | (b[2] & 0x3F),
            4 => ((b[0] & 0x07) << 18) | ((b[1] & 0x3F) << 12) | ((b[2] & 0x3F) << 6) | (b[3] & 0x3F),
            _ => 0xFFFD,
        };

        /// <summary>
        /// Replaces a sequence with another.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">Offset of the sequence to be replaced.</param>
        /// <param name="length">Length of the sequence to be replaced.</param>
        /// <param name="replacement">The replacement sequence.</param>
        /// <returns>The length of <paramref name="replacement"/>.</returns>
        public static int ReplaceSequence(
            ref ImVectorWrapper<byte> buf,
            int offset,
            int length,
            ReadOnlySpan<byte> replacement)
        {
            var i = 0;
            for (; i < replacement.Length; i++)
            {
                if (length >= i + 1)
                    buf[offset++] = replacement[i];
                else
                    buf.Insert(offset++, replacement[i]);
            }

            for (; i < length; i++)
                buf.RemoveAt(offset);

            return replacement.Length;
        }

        /// <summary>
        /// Normalize the given text for our use case.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        public static void Normalize(ref ImVectorWrapper<byte> buf)
        {
            // Ensure an implicit null after the end of the string.
            buf.EnsureCapacity(buf.Length + 1);
            buf.StorageSpan[buf.Length] = 0;

            for (var i = 0; i < buf.Length;)
            {
                var cb = CountBytes(buf.Data + i, buf.Length - i);
                var currInt = GetCodepoint(buf.Data + i, cb);
                switch (currInt)
                {
                    // Note that buf.Data[i + 1] is always defined. See the beginning of the function.
                    case '\r' when buf.Data[i + 1] == '\n': // Already CR LF?
                        i += 2;
                        continue;

                    case 0xFFFE or 0xFFFF: // Simply invalid
                    case > char.MaxValue: // ImWchar is same size with char; does not support
                    case >= 0xD800 and <= 0xDBFF: // UTF-16 surrogate; does not support
                        // Replace with \uFFFD in UTF-8: EF BF BD
                        i += ReplaceSequence(ref buf, i, cb, "\uFFFD"u8);
                        break;

                    // See String.Manipulation.cs: IndexOfNewlineChar.
                    case '\r': // CR; Carriage Return
                    case '\n': // LF; Line Feed
                    case '\f': // FF; Form Feed
                    case '\u0085': // NEL; Next Line
                    case '\u2028': // LS; Line Separator
                    case '\u2029': // PS; Paragraph Separator
                        i += ReplaceSequence(ref buf, i, cb, "\r\n"u8);
                        break;

                    default:
                        // Not a newline char.
                        i += cb;
                        break;
                }
            }
        }
    }
}

using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Utility;

/// <summary>Clipboard formats, looked up by their names.</summary>
internal static class ClipboardFormats
{
    /// <inheritdoc cref="CFSTR.CFSTR_FILECONTENTS"/>
    public static uint FileContents { get; } = ClipboardFormatFromName(CFSTR.CFSTR_FILECONTENTS);

    /// <summary>Gets the clipboard format corresponding to the PNG file format.</summary>
    public static uint Png { get; } = ClipboardFormatFromName("PNG");

    /// <inheritdoc cref="CFSTR.CFSTR_FILEDESCRIPTORW"/>
    public static uint FileDescriptorW { get; } = ClipboardFormatFromName(CFSTR.CFSTR_FILEDESCRIPTORW);

    /// <inheritdoc cref="CFSTR.CFSTR_FILEDESCRIPTORA"/>
    public static uint FileDescriptorA { get; } = ClipboardFormatFromName(CFSTR.CFSTR_FILEDESCRIPTORA);

    /// <inheritdoc cref="CFSTR.CFSTR_FILENAMEW"/>
    public static uint FileNameW { get; } = ClipboardFormatFromName(CFSTR.CFSTR_FILENAMEW);

    /// <inheritdoc cref="CFSTR.CFSTR_FILENAMEA"/>
    public static uint FileNameA { get; } = ClipboardFormatFromName(CFSTR.CFSTR_FILENAMEA);

    private static unsafe uint ClipboardFormatFromName(ReadOnlySpan<char> name)
    {
        uint cf;
        fixed (char* p = name)
            cf = RegisterClipboardFormatW(p);
        if (cf != 0)
            return cf;
        throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ??
              new InvalidOperationException($"RegisterClipboardFormatW({name}) failed.");
    }
}

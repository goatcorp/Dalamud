using System.Runtime.InteropServices;
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
internal static class ImGuiClipboardConfig
{
    private delegate void SetClipboardTextDelegate(IntPtr userData, string text);
    private delegate string GetClipboardTextDelegate();

    private static SetClipboardTextDelegate? _setTextOriginal = null;
    private static GetClipboardTextDelegate? _getTextOriginal = null;

    // These must exist as variables to prevent them from being GC'd
    private static SetClipboardTextDelegate? _setText = null;
    private static GetClipboardTextDelegate? _getText = null;

    public static void Apply()
    {
        var io = ImGui.GetIO();
        if (_setTextOriginal == null)
        {
            _setTextOriginal =
                Marshal.GetDelegateForFunctionPointer<SetClipboardTextDelegate>(io.SetClipboardTextFn);
        }

        if (_getTextOriginal == null)
        {
            _getTextOriginal =
                Marshal.GetDelegateForFunctionPointer<GetClipboardTextDelegate>(io.GetClipboardTextFn);
        }

        _setText = new SetClipboardTextDelegate(SetClipboardText);
        _getText = new GetClipboardTextDelegate(GetClipboardText);

        io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setText);
        io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getText);
    }

    public static void Unapply()
    {
        var io = ImGui.GetIO();
        if (_setTextOriginal != null)
        {
            io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setTextOriginal);
        }
        if (_getTextOriginal != null)
        {
            io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getTextOriginal);
        }
    }

    private static void SetClipboardText(IntPtr userData, string text)
    {
        _setTextOriginal!(userData, text.ReplaceLineEndings("\r\n"));
    }

    private static string GetClipboardText()
    {
        return _getTextOriginal!().ReplaceLineEndings("\r\n");
    }
}

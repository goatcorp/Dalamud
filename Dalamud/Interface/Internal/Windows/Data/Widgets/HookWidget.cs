using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying hook information.
/// </summary>
internal class HookWidget : IDataWindowWidget
{
    private Hook<MessageBoxWDelegate>? messageBoxMinHook;
    private bool hookUseMinHook;

    private delegate int MessageBoxWDelegate(
        IntPtr hWnd,
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        [MarshalAs(UnmanagedType.LPWStr)] string caption,
        MESSAGEBOX_STYLE type);

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Hook";

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "hook" };

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        try
        {
            ImGui.Checkbox("Use MinHook", ref this.hookUseMinHook);

            if (ImGui.Button("Create"))
                this.messageBoxMinHook = Hook<MessageBoxWDelegate>.FromSymbol("User32", "MessageBoxW", this.MessageBoxWDetour, this.hookUseMinHook);

            if (ImGui.Button("Enable"))
                this.messageBoxMinHook?.Enable();

            if (ImGui.Button("Disable"))
                this.messageBoxMinHook?.Disable();

            if (ImGui.Button("Call Original"))
                this.messageBoxMinHook?.Original(IntPtr.Zero, "Hello from .Original", "Hook Test", MESSAGEBOX_STYLE.MB_OK);

            if (ImGui.Button("Dispose"))
            {
                this.messageBoxMinHook?.Dispose();
                this.messageBoxMinHook = null;
            }

            if (ImGui.Button("Test"))
                _ = global::Windows.Win32.PInvoke.MessageBox(HWND.Null, "Hi", "Hello", MESSAGEBOX_STYLE.MB_OK);

            if (this.messageBoxMinHook != null)
                ImGui.Text("Enabled: " + this.messageBoxMinHook?.IsEnabled);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MinHook error caught");
        }
    }

    private int MessageBoxWDetour(IntPtr hwnd, string text, string caption, MESSAGEBOX_STYLE type)
    {
        Log.Information("[DATAHOOK] {Hwnd} {Text} {Caption} {Type}", hwnd, text, caption, type);

        var result = this.messageBoxMinHook!.Original(hwnd, "Cause Access Violation?", caption, MESSAGEBOX_STYLE.MB_YESNO);

        if (result == (int)MESSAGEBOX_RESULT.IDYES)
        {
            Marshal.ReadByte(IntPtr.Zero);
        }

        return result;
    }
}

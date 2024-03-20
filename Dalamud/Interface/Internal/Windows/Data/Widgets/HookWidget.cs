using System.Runtime.InteropServices;

using Dalamud.Hooking;
using ImGuiNET;
using PInvoke;
using Serilog;

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
        NativeFunctions.MessageBoxType type);
    
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
                this.messageBoxMinHook?.Original(IntPtr.Zero, "Hello from .Original", "Hook Test", NativeFunctions.MessageBoxType.Ok);

            if (ImGui.Button("Dispose"))
            {
                this.messageBoxMinHook?.Dispose();
                this.messageBoxMinHook = null;
            }

            if (ImGui.Button("Test"))
                _ = NativeFunctions.MessageBoxW(IntPtr.Zero, "Hi", "Hello", NativeFunctions.MessageBoxType.Ok);

            if (this.messageBoxMinHook != null)
                ImGui.Text("Enabled: " + this.messageBoxMinHook?.IsEnabled);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MinHook error caught");
        }
    }
    
    private int MessageBoxWDetour(IntPtr hwnd, string text, string caption, NativeFunctions.MessageBoxType type)
    {
        Log.Information("[DATAHOOK] {Hwnd} {Text} {Caption} {Type}", hwnd, text, caption, type);

        var result = this.messageBoxMinHook!.Original(hwnd, "Cause Access Violation?", caption, NativeFunctions.MessageBoxType.YesNo);

        if (result == (int)User32.MessageBoxResult.IDYES)
        {
            Marshal.ReadByte(IntPtr.Zero);
        }

        return result;
    }
}

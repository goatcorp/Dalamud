using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Detects the last actual ImGui window closing, so that we can turn off gamepad nav.
/// </summary>
internal class GamepadAutoExit
{
    private const long DebounceMs = 250;

    private const ImGuiWindowFlags ExcludedFlags =
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.ChildMenu;

    private bool hadWindow;
    private long? emptySince;

    /// <summary>
    /// Reset internal tracking state.
    /// </summary>
    public void Reset()
    {
        this.hadWindow = false;
        this.emptySince = null;
    }

    /// <summary>
    /// Gets whether the last navigable window has closed.
    /// </summary>
    /// <returns>True when gamepad mode should be disabled.</returns>
    public bool ShouldExit()
    {
        if (UiHideState.AnyActive)
        {
            this.Reset();
            return false;
        }

        if (HasNavWindow())
        {
            this.hadWindow = true;
            this.emptySince = null;
            return false;
        }

        if (!this.hadWindow)
        {
            return false;
        }

        var now = Environment.TickCount64;
        this.emptySince ??= now;
        if (now - this.emptySince < DebounceMs)
        {
            return false;
        }

        this.Reset();
        return true;
    }

    private static bool HasNavWindow()
    {
        var windows = ImGui.GetCurrentContext().Windows;
        for (var i = 0; i < windows.Size; i++)
        {
            if (IsNavWindow(windows[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNavWindow(ImGuiWindowPtr window)
    {
        if (!window.Active || window.IsFallbackWindow || window.RootWindow != window)
        {
            return false;
        }

        var flags = window.Flags;
        if ((flags & ExcludedFlags) != 0)
        {
            return false;
        }

        if ((flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiWindowFlags.Modal) == 0)
        {
            return false;
        }

        return (flags & ImGuiWindowFlags.NoTitleBar) == 0 || window.DockIsActive;
    }
}

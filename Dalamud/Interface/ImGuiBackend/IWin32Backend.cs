using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary><see cref="IImGuiBackend"/> with Win32 support.</summary>
internal interface IWin32Backend : IImGuiBackend
{
    /// <summary>Processes window messages.</summary>
    /// <param name="hWnd">Handle of the window.</param>
    /// <param name="msg">Type of window message.</param>
    /// <param name="wParam">wParam.</param>
    /// <param name="lParam">lParam.</param>
    /// <returns>Return value.</returns>
    public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
}

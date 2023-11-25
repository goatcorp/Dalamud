using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene;

/// <summary>
/// <see cref="IImGuiScene"/> with Win32 support.
/// </summary>
internal interface IWin32Scene : IImGuiScene
{
    /// <summary>
    /// Processes window messages.
    /// </summary>
    /// <param name="hWnd">Handle of the window.</param>
    /// <param name="msg">Type of window message.</param>
    /// <param name="wParam">wParam.</param>
    /// <param name="lParam">lParam.</param>
    /// <returns>Return value.</returns>
    public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
}

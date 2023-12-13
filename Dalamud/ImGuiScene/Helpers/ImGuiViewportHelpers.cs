using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using ImGuiNET;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Helpers for using ImGui Viewports.
/// </summary>
internal static class ImGuiViewportHelpers
{
    /// <summary>
    /// Delegate to be called when a window should be created.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void CreateWindowDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when a window should be destroyed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void DestroyWindowDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when a window should be resized.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="size">Size of the new window.</param>
    public delegate void SetWindowSizeDelegate(ImGuiViewportPtr viewport, Vector2 size);

    /// <summary>
    /// Delegate to be called when a window should be rendered.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="v">Custom user-provided argument from <see cref="ImGui.RenderPlatformWindowsDefault()"/>.</param>
    public delegate void RenderWindowDelegate(ImGuiViewportPtr viewport, nint v);

    /// <summary>
    /// Delegate to be called when buffers for the window should be swapped.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="v">Custom user-provided argument from <see cref="ImGui.RenderPlatformWindowsDefault()"/>.</param>
    public delegate void SwapBuffersDelegate(ImGuiViewportPtr viewport, nint v);

    /// <summary>
    /// Delegate to be called when the window should be showed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void ShowWindowDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when the window should be updated.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void UpdateWindowDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when the window position is queried.
    /// </summary>
    /// <param name="returnStorage">The return value storage.</param>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <returns>Same value with <paramref name="returnStorage"/>.</returns>
    public unsafe delegate Vector2* GetWindowPosDelegate(Vector2* returnStorage, ImGuiViewportPtr viewport);
    
    /// <summary>
    /// Delegate to be called when the window should be moved.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="pos">The new position.</param>
    public delegate void SetWindowPosDelegate(ImGuiViewportPtr viewport, Vector2 pos);

    /// <summary>
    /// Delegate to be called when the window size is queried.
    /// </summary>
    /// <param name="returnStorage">The return value storage.</param>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <returns>Same value with <paramref name="returnStorage"/>.</returns>
    public unsafe delegate Vector2* GetWindowSizeDelegate(Vector2* returnStorage, ImGuiViewportPtr viewport);
    
    /// <summary>
    /// Delegate to be called when the window should be given focus.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void SetWindowFocusDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when the window is focused.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <returns>Whether the window is focused.</returns>
    public delegate bool GetWindowFocusDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when whether the window is minimized is queried.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <returns>Whether the window is minimized.</returns>
    public delegate bool GetWindowMinimizedDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when the window title should be changed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="title">The new title.</param>
    public delegate void SetWindowTitleDelegate(ImGuiViewportPtr viewport, string title);

    /// <summary>
    /// Delegate to be called when the window alpha should be changed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="alpha">The new alpha.</param>
    public delegate void SetWindowAlphaDelegate(ImGuiViewportPtr viewport, float alpha);

    /// <summary>
    /// Delegate to be called when the IME input position should be changed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <param name="pos">The new position.</param>
    public delegate void SetImeInputPosDelegate(ImGuiViewportPtr viewport, Vector2 pos);

    /// <summary>
    /// Delegate to be called when the window's DPI scale value is queried.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    /// <returns>The DPI scale.</returns>
    public delegate float GetWindowDpiScaleDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Delegate to be called when viewport is changed.
    /// </summary>
    /// <param name="viewport">An instance of <see cref="ImGuiViewportPtr"/>.</param>
    public delegate void ChangedViewportDelegate(ImGuiViewportPtr viewport);

    /// <summary>
    /// Disables ImGui from disabling alpha for Viewport window backgrounds.
    /// </summary>
    public static unsafe void EnableViewportWindowBackgroundAlpha()
    {
        var offset = 0x00007FFB6ADA632C - 0x00007FFB6AD60000;
        offset += Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(x => x.ModuleName == "cimgui.dll")
                         .BaseAddress;
        var b = (byte*)offset;
        uint old;
        if (!VirtualProtect(b, 1, PAGE.PAGE_EXECUTE_READWRITE, &old))
        {
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
                  ?? throw new InvalidOperationException($"{nameof(VirtualProtect)} failed.");
        }

        *b = 0xEB;
        if (!VirtualProtect(b, 1, old, &old))
        {
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
                  ?? throw new InvalidOperationException($"{nameof(VirtualProtect)} failed.");
        }
    }
}

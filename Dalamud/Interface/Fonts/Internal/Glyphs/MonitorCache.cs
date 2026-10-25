using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Logging.Internal;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Caches per-monitor ClearType settings and display refresh rates.</summary>
/// <remarks><ul>
/// <li>Entries are read lazily and cleared on <see cref="Invalidate"/>, which the window procedure calls on
/// <c>WM_DISPLAYCHANGE</c> and <c>WM_SETTINGCHANGE</c> to avoid querying the display driver every frame.</li>
/// <li>Main thread only, except <see cref="Invalidate"/>.</li>
/// </ul></remarks>
internal static unsafe class MonitorCache
{
    private const uint DefaultRefreshHz = 60;

    private static readonly ModuleLog Log = new(typeof(MonitorCache));
    private static readonly Dictionary<nint, MonitorInfo> Monitors = [];
    private static int invalidated;

    /// <summary>Invalidates all cached monitor entries so they are re-read on next query.</summary>
    /// <remarks>Thread-safe.</remarks>
    public static void Invalidate() => Volatile.Write(ref invalidated, 1);

    /// <summary>Gets the cached settings for the monitor containing <paramref name="window"/>.</summary>
    /// <param name="window">The window, or <see langword="default"/> for the primary monitor.</param>
    /// <returns>The monitor's settings.</returns>
    public static MonitorInfo ForWindow(HWND window)
    {
        if (Volatile.Read(ref invalidated) != 0)
        {
            Volatile.Write(ref invalidated, 0);
            Monitors.Clear();
        }

        var monitor = MonitorFromWindow(window, MONITOR.MONITOR_DEFAULTTOPRIMARY);
        ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(Monitors, (nint)monitor.Value, out var exists);
        if (!exists)
            info = Read(monitor);
        return info;
    }

    /// <summary>Gets the cached settings for the monitor containing <paramref name="viewport"/>.</summary>
    /// <param name="viewport">The viewport, or null for the primary monitor.</param>
    /// <returns>The monitor's settings.</returns>
    /// <remarks>Some backends leave <see cref="ImGuiViewport.PlatformHandleRaw"/> null. Then
    /// <see cref="ImGuiViewport.PlatformHandle"/> is the window.</remarks>
    public static MonitorInfo ForViewport(ImGuiViewportPtr viewport) =>
        ForWindow(
            viewport.IsNull ? default
            : viewport.Handle->PlatformHandleRaw is not null ? (HWND)viewport.Handle->PlatformHandleRaw
            : (HWND)viewport.Handle->PlatformHandle);

    private static MonitorInfo Read(HMONITOR monitor)
    {
        var mode = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };
        var info = new MONITORINFOEXW { Base = { cbSize = (uint)sizeof(MONITORINFOEXW) } };
        var hasMode = GetMonitorInfoW(monitor, (MONITORINFO*)&info) &&
                      EnumDisplaySettingsW(&info.szDevice.e0, ENUM.ENUM_CURRENT_SETTINGS, &mode);
        var hz = hasMode && mode.dmDisplayFrequency > 1 ? mode.dmDisplayFrequency : DefaultRefreshHz;
        var orientation = hasMode ? mode.dmDisplayOrientation : DMDO_DEFAULT;
        try
        {
            return new(DirectWriteUtilities.ReadMonitorSettings(monitor, orientation), hz);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to read the ClearType settings of a monitor, drawing text in grayscale");
            return new(SubpixelSettings.Grayscale, hz);
        }
    }

    /// <summary>Cached display settings for a monitor.</summary>
    /// <param name="Text">ClearType and contrast settings.</param>
    /// <param name="RefreshHz">Display refresh rate in Hz.</param>
    public readonly record struct MonitorInfo(SubpixelSettings.MonitorSettings Text, uint RefreshHz);
}

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Logging.Internal;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>What the monitors want for text, and how often they refresh.</summary>
/// <remarks><ul>
/// <li>A monitor is read once, and again after <see cref="Invalidate"/>. The window procedure calls it on
/// <c>WM_DISPLAYCHANGE</c> and <c>WM_SETTINGCHANGE</c>, so that frames do not ask the display driver.</li>
/// <li>Main thread only, except <see cref="Invalidate"/>.</li>
/// </ul></remarks>
internal static unsafe class MonitorCache
{
    private const uint DefaultRefreshHz = 60;

    private static readonly ModuleLog Log = new(typeof(MonitorCache));
    private static readonly Dictionary<nint, MonitorInfo> Monitors = [];
    private static int invalidated;

    /// <summary>Makes every monitor be read again when next asked for.</summary>
    /// <remarks>Any thread.</remarks>
    public static void Invalidate() => Volatile.Write(ref invalidated, 1);

    /// <summary>Gets what the monitor a window is on wants.</summary>
    /// <param name="window">The window, or default for the primary monitor.</param>
    /// <returns>The monitor's settings.</returns>
    public static MonitorInfo ForWindow(HWND window)
    {
        if (Volatile.Read(ref invalidated) != 0)
            Clear();

        var monitor = MonitorFromWindow(window, MONITOR.MONITOR_DEFAULTTOPRIMARY);
        ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(Monitors, (nint)monitor.Value, out var exists);
        if (!exists)
            info = Read(monitor);
        return info;
    }

    private static void Clear()
    {
        Volatile.Write(ref invalidated, 0);
        Monitors.Clear();
    }

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

    /// <summary>What a monitor wants.</summary>
    /// <param name="Text">How it wants text drawn.</param>
    /// <param name="RefreshHz">How many times a second it shows a new image.</param>
    public readonly record struct MonitorInfo(SubpixelSettings.MonitorSettings Text, uint RefreshHz);
}

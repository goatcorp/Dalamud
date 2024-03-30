using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Helper class for retrieving Windows UI configuration.</summary>
public static class WindowsUiConfigHelper
{
    /// <summary>Gets the number of lines to scroll per mouse wheel detent.</summary>
    /// <returns>Number of lines to scroll.</returns>
    public static int GetWheelScrollLines()
    {
        int res;
        unsafe
        {
            if (!SystemParametersInfoW(SPI.SPI_GETWHEELSCROLLLINES, 0, &res, 0))
                res = 3;
        }

        return res;
    }

    /// <summary>Gets the delay between repeating keyboard inputs, in case a key is held down.</summary>
    /// <returns>The initial delay.</returns>
    public static TimeSpan GetKeyboardRepeatInitialDelay()
    {
        int res;
        unsafe
        {
            if (!SystemParametersInfoW(SPI.SPI_GETKEYBOARDDELAY, 0, &res, 0))
                res = 1;
        }

        // 0 (approximately 250 ms delay) through 3 (approximately 1 second delay)
        return TimeSpan.FromMilliseconds(250 * (res + 1));
    }

    /// <summary>Gets the interval between repeated keyboard inputs.</summary>
    /// <returns>The repeat interval.</returns>
    public static TimeSpan GetKeyboardRepeatInterval()
    {
        int res;
        unsafe
        {
            if (!SystemParametersInfoW(SPI.SPI_GETKEYBOARDSPEED, 0, &res, 0))
                res = 1;
        }

        // 0 (approximately 2.5 repetitions per second) through 31 (approximately 30 repetitions per second)
        // =>  0: 1/2.5 input/sec = 12/30
        // => 31:  1/30 input/sec
        return TimeSpan.FromSeconds(float.Lerp(1 / 2.5f, 1 / 30f, res / 31f));
    }
}

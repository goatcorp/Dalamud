using System;
using Microsoft.Win32;

namespace Dalamud.Injector;

/// <summary>
/// Utility and helper methods for various Injector-related tasks.
/// </summary>
public class Util
{
    /// <summary>
    /// Heuristically check if the current context is running on Linux/macOS/whatever via Wine.
    /// </summary>
    /// <returns>Returns true if the system can reasonably be assumed to be running via Wine.</returns>
    internal static bool IsOnWine()
    {
        // Shouldn't ever matter, this is just a guard to shut up the compiler
        if (!OperatingSystem.IsWindows())
            return true;

        if (IsEnvVarTruthy("XL_WINEONLINUX"))
            return true;

        var ntdllPtr = NativeFunctions.GetModuleHandleW("ntdll.dll");
        var wineVersionPtr = NativeFunctions.GetProcAddress(ntdllPtr, "wine_get_version");
        var wineBuildIdPtr = NativeFunctions.GetProcAddress(ntdllPtr, "wine_get_build_id");

        if (wineVersionPtr != IntPtr.Zero || wineBuildIdPtr != IntPtr.Zero)
            return true;

        return Registry.CurrentUser.OpenSubKey(@"Software\Wine") != null ||
               Registry.LocalMachine.OpenSubKey(@"Software\Wine") != null;
    }

    /// <summary>
    /// Determines if a specific environment var resolves as "truthy". Unset environment variables will return false.
    /// </summary>
    /// <param name="envVar">The environment variable to check.</param>
    /// <returns>Returns true if the specified variable is set and truthy, false otherwise.</returns>
    internal static bool IsEnvVarTruthy(string envVar)
    {
        _ = bool.TryParse(Environment.GetEnvironmentVariable(envVar), out var result);

        // will be false if conversion fails.
        return result;
    }
}

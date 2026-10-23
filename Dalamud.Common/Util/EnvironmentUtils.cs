using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Common.Util;

public static class EnvironmentUtils
{
    /// <summary>
    /// Attempts to get an environment variable using the Try pattern.
    /// </summary>
    /// <param name="variableName">The env var to get.</param>
    /// <param name="value">An output containing the env var, if present.</param>
    /// <returns>A boolean indicating whether the var was present.</returns>
    public static bool TryGetEnvironmentVariable(string variableName, [NotNullWhen(true)] out string? value)
    {
        value = Environment.GetEnvironmentVariable(variableName);
        return value != null;
    }


    /// <summary>
    /// Returns the detected platform independently from any launch args or env vars.
    /// </summary>
    /// <remarks>
    /// Be aware this should only be used for certain diagnostics and standalone unit tests.
    /// </remarks>
    /// <returns>The detected platform.</returns>
    public static OSPlatform DetectPlatformHeuristic()
    {
        var ntdll = Windows.Win32.PInvoke.GetModuleHandle("ntdll.dll");
        var wineServerCallPtr = Windows.Win32.PInvoke.GetProcAddress(ntdll, "wine_server_call");
        var wineGetHostVersionPtr = Windows.Win32.PInvoke.GetProcAddress(ntdll, "wine_get_host_version");
        var winePlatform = GetWinePlatform(wineGetHostVersionPtr);
        var isWine = wineServerCallPtr != nint.Zero;

        static unsafe string? GetWinePlatform(nint wineGetHostVersionPtr)
        {
            if (wineGetHostVersionPtr == nint.Zero) return null;

            var methodDelegate = (delegate* unmanaged[Cdecl]<out char*, out char*, void>)wineGetHostVersionPtr;
            methodDelegate(out var platformPtr, out var _);

            if (platformPtr == null) return null;

            return Marshal.PtrToStringAnsi((nint)platformPtr);
        }

        if (!isWine)
            return OSPlatform.Windows;

        if (winePlatform == "Darwin")
            return OSPlatform.OSX;

        return OSPlatform.Linux;
    }
}

using System.Runtime.InteropServices;

namespace Dalamud.Utility;

/// <summary>
/// Utilities for handling errors inside Dalamud.
/// </summary>
internal static partial class ErrorHandling
{
    /// <summary>
    /// Crash the game at this point, and show the crash handler with the supplied context.
    /// </summary>
    /// <param name="context">The context to show in the crash handler.</param>
    public static void CrashWithContext(string context)
    {
        BootVehRaiseExternalEvent(context);
    }

    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootVehRaiseExternalEventW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial void BootVehRaiseExternalEvent(string info);
}

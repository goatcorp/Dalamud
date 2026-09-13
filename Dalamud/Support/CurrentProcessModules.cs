using System.Diagnostics;
using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Support;

/// <summary>
/// Provides a cached list of loaded process modules, invalidated by notifications from Dalamud.Boot.
/// </summary>
/// <remarks>Avoids repeatedly enumerating <see cref="Process.Modules"/>.</remarks>
internal static unsafe partial class CurrentProcessModules
{
    /// <summary>Gets the cached process-module list, refreshing it after a module-change notification.</summary>
    public static ProcessModuleCollection ModuleCollection
    {
        get
        {
            ref var t = ref *GetDllChangedStorage();
            if (t != 0)
            {
                t = 0;
                field = null;
                Log.Verbose("{what}: Fetching fresh copy of current process modules.", nameof(CurrentProcessModules));
            }

            try
            {
                return field ??= Process.GetCurrentProcess().Modules;
            }
            catch (Exception e)
            {
                Log.Verbose(e, "{what}: Failed to fetch module list.", nameof(CurrentProcessModules));
                return new([]);
            }
        }
    }

    [LibraryImport("Dalamud.Boot.dll")]
    private static partial int* GetDllChangedStorage();
}

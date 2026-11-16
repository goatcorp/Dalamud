using System.Diagnostics;
using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Support;

/// <summary>Tracks the loaded process modules.</summary>
internal static unsafe partial class CurrentProcessModules
{
    private static ProcessModuleCollection? moduleCollection;

    /// <summary>Gets all the loaded modules, up to date.</summary>
    public static ProcessModuleCollection ModuleCollection
    {
        get
        {
            ref var t = ref *GetDllChangedStorage();
            if (t != 0)
            {
                t = 0;
                moduleCollection = null;
                Log.Verbose("{what}: Fetching fresh copy of current process modules.", nameof(CurrentProcessModules));
            }

            try
            {
                return moduleCollection ??= Process.GetCurrentProcess().Modules;
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

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <inheritdoc />
internal class NvPresentUnwrapper : ComHookUnwrapper
{
    /// <inheritdoc/>
    protected override unsafe bool IsRelevantComObject<T>(T* obj)
    {
        if (!IsValidReadableMemoryAddress((nint)obj, sizeof(nint)))
            return false;

        try
        {
            var vtbl = (nint**)Marshal.ReadIntPtr((nint)obj);
            if (!IsValidReadableMemoryAddress((nint)vtbl, sizeof(nint) * 3))
                return false;

            // Enumerate the loaded modules once and reuse the snapshot for every vtable pointer
            // check, rather than calling Process.GetCurrentProcess().Modules per pointer (which also
            // leaks an undisposed Process). The module set is stable for the duration of this check.
            using var process = Process.GetCurrentProcess();
            var modules = process.Modules;

            for (var i = 0; i < 3; i++)
            {
                var pfn = Marshal.ReadIntPtr((nint)(vtbl + i));
                if (!IsValidExecutableMemoryAddress(pfn, 1))
                    return false;
                if (!BelongsInNvPresentDll(modules, pfn))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BelongsInNvPresentDll(ProcessModuleCollection modules, nint ptr)
    {
        foreach (ProcessModule processModule in modules)
        {
            if (ptr < processModule.BaseAddress ||
                ptr >= processModule.BaseAddress + processModule.ModuleMemorySize ||
                !processModule.ModuleName.Contains("NvPresent", StringComparison.OrdinalIgnoreCase))
                continue;

            return true;
        }

        return false;
    }
}

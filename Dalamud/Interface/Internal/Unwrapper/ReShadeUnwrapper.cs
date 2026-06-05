using System.Diagnostics;
using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <inheritdoc />
internal unsafe class ReShadeUnwrapper : ComHookUnwrapper
{
    /// <inheritdoc/>
    protected override bool IsRelevantComObject<T>(T* obj)
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
                if (!BelongsInReShadeDll(modules, pfn))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BelongsInReShadeDll(ProcessModuleCollection modules, nint ptr)
    {
        foreach (ProcessModule processModule in modules)
        {
            if (ptr < processModule.BaseAddress ||
                ptr >= processModule.BaseAddress + processModule.ModuleMemorySize ||
                !HasProcExported(processModule, "ReShadeRegisterAddon"u8) ||
                !HasProcExported(processModule, "ReShadeUnregisterAddon"u8) ||
                !HasProcExported(processModule, "ReShadeRegisterEvent"u8) ||
                !HasProcExported(processModule, "ReShadeUnregisterEvent"u8))
                continue;

            return true;
        }

        return false;

        static bool HasProcExported(ProcessModule m, ReadOnlySpan<byte> name)
        {
            fixed (byte* p = name)
                return GetProcAddress((HMODULE)m.BaseAddress, (sbyte*)p) != null;
        }
    }
}

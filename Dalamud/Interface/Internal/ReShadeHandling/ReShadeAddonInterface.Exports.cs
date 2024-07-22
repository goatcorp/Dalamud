using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.ReShadeHandling;

/// <summary>ReShade interface.</summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal sealed unsafe partial class ReShadeAddonInterface
{
    private static readonly ExportsStruct Exports;

    static ReShadeAddonInterface()
    {
        foreach (var m in Process.GetCurrentProcess().Modules.Cast<ProcessModule>())
        {
            ExportsStruct e;
            if (!GetProcAddressInto(m, nameof(e.ReShadeRegisterAddon), &e.ReShadeRegisterAddon) ||
                !GetProcAddressInto(m, nameof(e.ReShadeUnregisterAddon), &e.ReShadeUnregisterAddon) ||
                !GetProcAddressInto(m, nameof(e.ReShadeRegisterEvent), &e.ReShadeRegisterEvent) ||
                !GetProcAddressInto(m, nameof(e.ReShadeUnregisterEvent), &e.ReShadeUnregisterEvent))
                continue;

            ReShadeModule = m;
            Exports = e;
            return;
        }

        return;

        bool GetProcAddressInto(ProcessModule m, ReadOnlySpan<char> name, void* res)
        {
            Span<byte> name8 = stackalloc byte[Encoding.UTF8.GetByteCount(name) + 1];
            name8[Encoding.UTF8.GetBytes(name, name8)] = 0;
            *(nint*)res = GetProcAddress((HMODULE)m.BaseAddress, (sbyte*)Unsafe.AsPointer(ref name8[0]));
            return *(nint*)res != 0;
        }
    }

    /// <summary>Gets the active ReShade module.</summary>
    public static ProcessModule? ReShadeModule { get; private set; }

    private struct ExportsStruct
    {
        public delegate* unmanaged<HMODULE, uint, bool> ReShadeRegisterAddon;
        public delegate* unmanaged<HMODULE, void> ReShadeUnregisterAddon;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeRegisterEvent;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeUnregisterEvent;
    }
}

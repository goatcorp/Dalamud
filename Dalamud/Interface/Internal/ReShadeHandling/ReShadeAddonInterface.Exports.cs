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

            fixed (void* pwszFile = m.FileName)
            fixed (Guid* pguid = &WINTRUST_ACTION_GENERIC_VERIFY_V2)
            {
                var wtfi = new WINTRUST_FILE_INFO
                {
                    cbStruct = (uint)sizeof(WINTRUST_FILE_INFO),
                    pcwszFilePath = (ushort*)pwszFile,
                    hFile = default,
                    pgKnownSubject = null,
                };
                var wtd = new WINTRUST_DATA
                {
                    cbStruct = (uint)sizeof(WINTRUST_DATA),
                    pPolicyCallbackData = null,
                    pSIPClientData = null,
                    dwUIChoice = WTD.WTD_UI_NONE,
                    fdwRevocationChecks = WTD.WTD_REVOKE_NONE,
                    dwUnionChoice = WTD.WTD_STATEACTION_VERIFY,
                    hWVTStateData = default,
                    pwszURLReference = null,
                    dwUIContext = 0,
                    pFile = &wtfi,
                };
                ReShadeHasSignature = WinVerifyTrust(default, pguid, &wtd) != TRUST.TRUST_E_NOSIGNATURE;
            }

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

    /// <summary>Gets a value indicating whether the loaded ReShade has signatures.</summary>
    /// <remarks>ReShade without addon support is signed, but may not pass signature verification.</remarks>
    public static bool ReShadeHasSignature { get; private set; }

    private struct ExportsStruct
    {
        public delegate* unmanaged<HMODULE, uint, bool> ReShadeRegisterAddon;
        public delegate* unmanaged<HMODULE, void> ReShadeUnregisterAddon;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeRegisterEvent;
        public delegate* unmanaged<AddonEvent, void*, void> ReShadeUnregisterEvent;
    }
}

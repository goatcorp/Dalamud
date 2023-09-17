using System.Diagnostics;
using System.Linq;

using Serilog;

namespace Dalamud.Game.Internal.DXGI;

/// <summary>
/// The address resolver for native D3D11 methods to facilitate displaying the Dalamud UI.
/// </summary>
[Obsolete("This has been deprecated in favor of the VTable resolver.")]
internal sealed class SwapChainSigResolver : BaseAddressResolver, ISwapChainAddressResolver
{
    /// <inheritdoc/>
    public IntPtr Present { get; set; }

    /// <inheritdoc/>
    public IntPtr ResizeBuffers { get; set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(SigScanner sig)
    {
        var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(m => m.ModuleName == "dxgi.dll");

        Log.Debug($"Found DXGI: 0x{module.BaseAddress.ToInt64():X}");

        var scanner = new SigScanner(module);

        // This(code after the function head - offset of it) was picked to avoid running into issues with other hooks being installed into this function.
        this.Present = scanner.ScanModule("41 8B F0 8B FA 89 54 24 ?? 48 8B D9 48 89 4D ?? C6 44 24 ?? 00") - 0x37;

        this.ResizeBuffers = scanner.ScanModule("48 8B C4 55 41 54 41 55 41 56 41 57 48 8D 68 B1 48 81 EC ?? ?? ?? ?? 48 C7 45 ?? ?? ?? ?? ?? 48 89 58 10 48 89 70 18 48 89 78 20 45 8B F9 45 8B E0 44 8B EA 48 8B F9 8B 45 7F 89 44 24 30 8B 75 77 89 74 24 28 44 89 4C 24");
    }
}

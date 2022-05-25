using System;
using Dalamud.Game;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Fixes;

/// <summary>
/// This fix is for the following issue:
/// Null reference in the game's WndProc function when certain window messages arrive
/// before an object on the game's input manager is initialized.
/// </summary>
internal class WndProcNullRefFix : IGameFix, IDisposable
{
    private AsmHook? wndProcHook;

    /// <inheritdoc/>
    public void Apply()
    {
        var sigScanner = Service<SigScanner>.Get();

        if (!sigScanner.TryScanText("E8 ?? ?? ?? ?? 48 83 38 00 74 14", out var patchAddress))
        {
            Log.Error("Failed to find WndProc patch address");
            return;
        }

        Log.Information($"Applying WndProcNullRefFix at {patchAddress:X}");

        var patchAsm = new byte[]
        {
            0x48, 0x85, 0xc0, // test rax, rax
            0x74, 0x15, // jz +0x1A
        };

        this.wndProcHook = new AsmHook(patchAddress, patchAsm, "WndProcNullRefFix");
        this.wndProcHook.Enable();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.wndProcHook?.Dispose();
    }
}

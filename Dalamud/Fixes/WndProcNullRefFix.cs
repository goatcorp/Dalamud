using System;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Memory;
using Serilog;

namespace Dalamud.Fixes;

/// <summary>
/// This fix is for the following issue:
/// Null reference in the game's WndProc function when certain window messages arrive
/// before an object on the game's input manager is initialized.
/// </summary>
internal class WndProcNullRefFix : IGameFix, IDisposable
{
    private Hook<WndProcDelegate>? wndProcHook;

    private IntPtr object1Address;
    private IntPtr object2Address;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <inheritdoc/>
    public void Apply()
    {
        var sigScanner = Service<SigScanner>.Get();

        if (!sigScanner.TryScanText("40 55 53 41 54 41 56 48 8D 6C 24 ??", out var patchAddress))
        {
            Log.Error("Failed to find WndProc address");
            return;
        }

        if (!sigScanner.TryGetStaticAddressFromSig("74 1F E8 ?? ?? ?? ?? 48 83 38 00 ", out this.object1Address))
        {
            Log.Error("Failed to find object1 address");
            return;
        }

        if (!sigScanner.TryGetStaticAddressFromSig("E8 ?? ?? ?? ?? 48 83 38 00 74 14", out this.object2Address, 0x7))
        {
            Log.Error("Failed to find object2 address");
            return;
        }

        Log.Information($"Applying WndProcNullRefFix at {patchAddress:X} with o1 {this.object1Address:X}, o2 {this.object2Address:X}");

        this.wndProcHook = new Hook<WndProcDelegate>(patchAddress, this.WndProcDetour, true);
        Log.Information("Set up hook");
        this.wndProcHook.Enable();
        Log.Information("Enabled hook");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.wndProcHook?.Dispose();
    }

    private IntPtr WndProcDetour(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == 0x219 && wParam.ToInt64() == 7 && (MemoryHelper.Read<IntPtr>(this.object1Address) == IntPtr.Zero || MemoryHelper.Read<IntPtr>(this.object2Address) == IntPtr.Zero))
        {
            Log.Information("Filtered WM_DEVICE_CHANGE message");
            return IntPtr.Zero;
        }

        return this.wndProcHook!.Original(hWnd, msg, wParam, lParam);
    }
}

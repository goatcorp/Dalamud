using System.Threading;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Internal;

/// <summary>Helper for dealing with swap chains.</summary>
internal static unsafe class SwapChainHelper
{
    /// <summary>Gets the game's active instance of IDXGISwapChain that is initialized.</summary>
    /// <value>Address of the game's instance of IDXGISwapChain, or <c>null</c> if not available (yet.)</value>
    public static IDXGISwapChain* GameDeviceSwapChain
    {
        get
        {
            var kernelDev = Device.Instance();
            if (kernelDev == null)
                return null;

            var swapChain = kernelDev->SwapChain;
            if (swapChain == null)
                return null;

            // BackBuffer should be something from IDXGISwapChain->GetBuffer, which means that IDXGISwapChain itself
            // must have been fully initialized.
            if (swapChain->BackBuffer == null)
                return null;

            return (IDXGISwapChain*)swapChain->DXGISwapChain;
        }
    }

    /// <summary>Gets the vtable of <see cref="GameDeviceSwapChain"/>.</summary>
    public static IDXGISwapChain.Vtbl<IDXGISwapChain>* GameDeviceSwapChainVtbl
    {
        get
        {
            var s = GameDeviceSwapChain;
            return (IDXGISwapChain.Vtbl<IDXGISwapChain>*)(s is null ? null : s->lpVtbl);
        }
    }

    /// <summary>Wait for the game to have finished initializing the IDXGISwapChain.</summary>
    public static void BusyWaitForGameDeviceSwapChain()
    {
        while (GameDeviceSwapChain is null)
            Thread.Yield();
    }
}

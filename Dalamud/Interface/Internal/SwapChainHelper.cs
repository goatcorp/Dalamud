using System.Threading;

using Dalamud.Interface.Internal.ReShadeHandling;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Helper for dealing with swap chains.</summary>
internal static unsafe class SwapChainHelper
{
    private static IDXGISwapChain* foundGameDeviceSwapChain;

    /// <summary>Describes how to hook <see cref="IDXGISwapChain"/> methods.</summary>
    public enum HookMode
    {
        /// <summary>Hooks by rewriting the native bytecode.</summary>
        ByteCode,

        /// <summary>Hooks by providing an alternative vtable.</summary>
        VTable,
    }

    /// <summary>Gets the game's active instance of IDXGISwapChain that is initialized.</summary>
    /// <value>Address of the game's instance of IDXGISwapChain, or <c>null</c> if not available (yet.)</value>
    public static IDXGISwapChain* GameDeviceSwapChain
    {
        get
        {
            if (foundGameDeviceSwapChain is not null)
                return foundGameDeviceSwapChain;

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

            return foundGameDeviceSwapChain = (IDXGISwapChain*)swapChain->DXGISwapChain;
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

    /// <inheritdoc cref="IsGameDeviceSwapChain{T}"/>
    public static bool IsGameDeviceSwapChain(nint punk) => IsGameDeviceSwapChain((IUnknown*)punk);

    /// <summary>Determines if the given instance of IUnknown is the game device's swap chain.</summary>
    /// <param name="punk">Object to check.</param>
    /// <typeparam name="T">Type of the object to check.</typeparam>
    /// <returns><c>true</c> if the object is the game's swap chain.</returns>
    public static bool IsGameDeviceSwapChain<T>(T* punk) where T : unmanaged, IUnknown.Interface
    {
        using var psc = default(ComPtr<IDXGISwapChain>);
        fixed (Guid* piid = &IID.IID_IDXGISwapChain)
        {
            if (punk->QueryInterface(piid, (void**)psc.GetAddressOf()).FAILED)
                return false;
        }

        return IsGameDeviceSwapChain(psc.Get());
    }

    /// <inheritdoc cref="IsGameDeviceSwapChain{T}"/>
    public static bool IsGameDeviceSwapChain(IDXGISwapChain* punk)
    {
        DXGI_SWAP_CHAIN_DESC desc1;
        if (punk->GetDesc(&desc1).FAILED)
            return false;
        
        DXGI_SWAP_CHAIN_DESC desc2;
        if (GameDeviceSwapChain->GetDesc(&desc2).FAILED)
            return false;

        return desc1.OutputWindow == desc2.OutputWindow;
    }

    /// <summary>Wait for the game to have finished initializing the IDXGISwapChain.</summary>
    public static void BusyWaitForGameDeviceSwapChain()
    {
        while (GameDeviceSwapChain is null)
            Thread.Yield();
    }

    /// <summary>
    /// Make <see cref="GameDeviceSwapChain"/> store address of unwrapped swap chain, if it was wrapped with ReShade.
    /// </summary>
    /// <returns><c>true</c> if it was wrapped with ReShade.</returns>
    public static bool UnwrapReShade()
    {
        using var swapChain = new ComPtr<IDXGISwapChain>(GameDeviceSwapChain);
        if (!ReShadeUnwrapper.Unwrap(&swapChain))
            return false;

        foundGameDeviceSwapChain = swapChain.Get();
        return true;
    }
}

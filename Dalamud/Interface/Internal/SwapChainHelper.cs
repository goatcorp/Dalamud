using System.Threading;

using Dalamud.Interface.Internal.Unwrapper;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Helper for dealing with swap chains.</summary>
internal static unsafe class SwapChainHelper
{
    private static IDXGISwapChain* foundGameDisplaySwapChain;

    /// <summary>Describes how to hook <see cref="IDXGISwapChain"/> methods.</summary>
    public enum HookMode
    {
        /// <summary>Hooks by rewriting the native bytecode.</summary>
        ByteCode,

        /// <summary>Hooks by providing an alternative vtable.</summary>
        VTable,
    }

    /// <summary>Gets the effective <see cref="IDXGISwapChain"/> that presents to the game window.</summary>
    /// <value>The display swap chain, or <c>null</c> until the game initializes its back buffer.</value>
    /// <remarks>
    /// After unwrapping graphics middleware, the cached display pointer may differ from
    /// <see cref="SwapChain.DXGISwapChain"/>.
    /// This pointer is borrowed; callers must not release it. Unwrapping replaces only the cached pointer,
    /// not the game's swap-chain field.
    /// </remarks>
    public static IDXGISwapChain* GameDisplaySwapChain
    {
        get
        {
            if (foundGameDisplaySwapChain is not null)
                return foundGameDisplaySwapChain;

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

            return foundGameDisplaySwapChain = (IDXGISwapChain*)swapChain->DXGISwapChain;
        }
    }

    /// <summary>Gets the vtable of <see cref="GameDisplaySwapChain"/>.</summary>
    public static IDXGISwapChain.Vtbl<IDXGISwapChain>* GameDeviceSwapChainVtbl
    {
        get
        {
            var s = GameDisplaySwapChain;
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
        if (GameDisplaySwapChain->GetDesc(&desc2).FAILED)
            return false;

        return desc1.OutputWindow == desc2.OutputWindow;
    }

    /// <summary>Wait for the game to have finished initializing the IDXGISwapChain.</summary>
    public static void BusyWaitForGameDeviceSwapChain()
    {
        while (GameDisplaySwapChain is null)
            Thread.Yield();
    }

    /// <summary>
    /// Replaces <see cref="GameDisplaySwapChain"/> with the underlying swap chain after removing ReShade wrappers.
    /// </summary>
    /// <returns><c>true</c> if at least one ReShade wrapper was removed.</returns>
    public static bool UnwrapReShade()
    {
        using var swapChain = new ComPtr<IDXGISwapChain>(GameDisplaySwapChain);
        var reshadeUnwrapper = new ReShadeUnwrapper();
        if (!reshadeUnwrapper.Unwrap(&swapChain))
            return false;

        foundGameDisplaySwapChain = swapChain.Get();
        return true;
    }

    /// <summary>
    /// Replaces <see cref="GameDisplaySwapChain"/> with the underlying swap chain after removing NvPresent wrappers.
    /// </summary>
    /// <returns><c>true</c> if an NvPresent wrapper was removed.</returns>
    public static bool UnwrapNvPresent()
    {
        using var swapChain = new ComPtr<IDXGISwapChain>(GameDisplaySwapChain);
        var reshadeUnwrapper = new NvPresentUnwrapper();
        if (!reshadeUnwrapper.Unwrap(&swapChain))
            return false;

        foundGameDisplaySwapChain = swapChain.Get();
        return true;
    }
}

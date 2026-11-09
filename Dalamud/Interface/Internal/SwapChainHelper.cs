using System.Collections.Generic;
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
    private static ComPtr<IDXGISwapChain> ownedGameDisplaySwapChain;

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
    /// This pointer is borrowed; callers must not release it. The cache retains an unwrapped interface until
    /// presentation hooks and rendering resources are released. Unwrapping changes only the cache, not the game's field.
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

            // An initialized back buffer indicates that the swap chain is ready for use.
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
    /// <param name="visited">Wrappers already removed in this wrapper-discovery transaction.</param>
    /// <returns><c>true</c> if at least one ReShade wrapper was removed.</returns>
    public static bool UnwrapReShade(HashSet<nint>? visited = null) => Unwrap(new ReShadeUnwrapper(), visited);

    /// <summary>
    /// Replaces <see cref="GameDisplaySwapChain"/> with the underlying swap chain after removing NvPresent wrappers.
    /// </summary>
    /// <param name="visited">Wrappers already removed in this wrapper-discovery transaction.</param>
    /// <returns><c>true</c> if an NvPresent wrapper was removed.</returns>
    public static bool UnwrapNvPresent(HashSet<nint>? visited = null) => Unwrap(new NvPresentUnwrapper(), visited);

    /// <summary>Clears the cache and releases its reference after presentation hooks and rendering resources are released.</summary>
    public static void ResetGameDisplaySwapChain()
    {
        foundGameDisplaySwapChain = null;
        ownedGameDisplaySwapChain.Dispose();
        ownedGameDisplaySwapChain = default;
    }

    /// <summary>Unwraps the cached chain while retaining ownership of any discovered interface.</summary>
    /// <param name="unwrapper">The wrapper-specific discovery strategy.</param>
    /// <param name="visited">Wrappers already removed in this wrapper-discovery transaction.</param>
    /// <returns>Whether the cached interface changed.</returns>
    private static bool Unwrap(ComHookUnwrapper unwrapper, HashSet<nint>? visited = null)
    {
        using var swapChain = new ComPtr<IDXGISwapChain>(GameDisplaySwapChain);
        if (!unwrapper.Unwrap(&swapChain, visited))
            return false;

        // QueryInterface may return a tear-off interface whose lifetime requires retaining this reference.
        foundGameDisplaySwapChain = swapChain.Get();
        swapChain.Swap(ref ownedGameDisplaySwapChain);
        return true;
    }
}

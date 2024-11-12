using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal unsafe partial class InterfaceManager
{
    // NOTE: Do not use HRESULT as return value type. It appears that .NET marshaller thinks HRESULT needs to be still
    // treated as a type that does not fit into RAX.

    /// <summary>Delegate for <c>DXGISwapChain::on_present(UINT flags, const DXGI_PRESENT_PARAMETERS *params)</c> in
    /// <c>dxgi_swapchain.cpp</c>.</summary>
    /// <param name="swapChain">Pointer to an instance of <c>DXGISwapChain</c>, which happens to be an
    /// <see cref="IDXGISwapChain"/>.</param>
    /// <param name="flags">An integer value that contains swap-chain presentation options. These options are defined by
    /// the <c>DXGI_PRESENT</c> constants.</param>
    /// <param name="presentParams">Optional; DXGI present parameters.</param>
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void ReShadeDxgiSwapChainPresentDelegate(
        ReShadeDxgiSwapChain* swapChain,
        uint flags,
        DXGI_PRESENT_PARAMETERS* presentParams);

    /// <summary>Delegate for <see cref="IDXGISwapChain.Present"/>.
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present">Microsoft
    /// Learn</a>.</summary>
    /// <param name="swapChain">Pointer to an instance of <see cref="IDXGISwapChain"/>.</param>
    /// <param name="syncInterval">An integer that specifies how to synchronize presentation of a frame with the
    /// vertical blank.</param>
    /// <param name="flags">An integer value that contains swap-chain presentation options. These options are defined by
    /// the <c>DXGI_PRESENT</c> constants.</param>
    /// <returns>A <see cref="HRESULT"/> representing the result of the operation.</returns>
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int DxgiSwapChainPresentDelegate(IDXGISwapChain* swapChain, uint syncInterval, uint flags);

    /// <summary>Detour function for <see cref="IDXGISwapChain.ResizeBuffers"/>.
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-resizebuffers">
    /// Microsoft Learn</a>.</summary>
    /// <param name="swapChain">Pointer to an instance of <see cref="IDXGISwapChain"/>.</param>
    /// <param name="bufferCount">The number of buffers in the swap chain (including all back and front buffers).
    /// This number can be different from the number of buffers with which you created the swap chain. This number
    /// can't be greater than <see cref="DXGI.DXGI_MAX_SWAP_CHAIN_BUFFERS"/>. Set this number to zero to preserve the
    /// existing number of buffers in the swap chain. You can't specify less than two buffers for the flip presentation
    /// model.</param>
    /// <param name="width">The new width of the back buffer. If you specify zero, DXGI will use the width of the client
    /// area of the target window. You can't specify the width as zero if you called the
    /// <see cref="IDXGIFactory2.CreateSwapChainForComposition"/> method to create the swap chain for a composition
    /// surface.</param>
    /// <param name="height">The new height of the back buffer. If you specify zero, DXGI will use the height of the
    /// client area of the target window. You can't specify the height as zero if you called the
    /// <see cref="IDXGIFactory2.CreateSwapChainForComposition"/> method to create the swap chain for a composition
    /// surface.</param>
    /// <param name="newFormat">A DXGI_FORMAT-typed value for the new format of the back buffer. Set this value to
    /// <see cref="DXGI_FORMAT.DXGI_FORMAT_UNKNOWN"/> to preserve the existing format of the back buffer. The flip
    /// presentation model supports a more restricted set of formats than the bit-block transfer (bitblt) model.</param>
    /// <param name="swapChainFlags">A combination of <see cref="DXGI_SWAP_CHAIN_FLAG"/>-typed values that are combined
    /// by using a bitwise OR operation. The resulting value specifies options for swap-chain behavior.</param>
    /// <returns>A <see cref="HRESULT"/> representing the result of the operation.</returns>
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int ResizeBuffersDelegate(
        IDXGISwapChain* swapChain,
        uint bufferCount,
        uint width,
        uint height,
        DXGI_FORMAT newFormat,
        uint swapChainFlags);

    private void ReShadeDxgiSwapChainOnPresentDetour(
        ReShadeDxgiSwapChain* swapChain,
        uint flags,
        DXGI_PRESENT_PARAMETERS* presentParams)
    {
        Debug.Assert(
            this.reShadeDxgiSwapChainPresentHook is not null,
            "this.reShadeDxgiSwapChainPresentHook is not null");

        if (this.RenderDalamudCheckAndInitialize(swapChain->AsIDxgiSwapChain(), flags) is { } activeScene)
            this.RenderDalamudDraw(activeScene);

        this.reShadeDxgiSwapChainPresentHook!.Original(swapChain, flags, presentParams);

        // Upstream call to system IDXGISwapChain::Present will be called by ReShade.
    }

    private int DxgiSwapChainPresentDetour(IDXGISwapChain* swapChain, uint syncInterval, uint flags)
    {
        Debug.Assert(this.dxgiSwapChainPresentHook is not null, "this.dxgiSwapChainPresentHook is not null");

        if (this.RenderDalamudCheckAndInitialize(swapChain, flags) is { } activeScene)
            this.RenderDalamudDraw(activeScene);

        return this.dxgiSwapChainPresentHook!.Original(swapChain, syncInterval, flags);
    }

    private int AsHookDxgiSwapChainResizeBuffersDetour(
        IDXGISwapChain* swapChain,
        uint bufferCount,
        uint width,
        uint height,
        DXGI_FORMAT newFormat,
        uint swapChainFlags)
    {
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

#if DEBUG
        Log.Verbose(
            $"Calling resizebuffers swap@{(nint)swapChain:X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

        this.ResizeBuffers?.InvokeSafely();

        this.scene?.OnPreResize();

        var ret = this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        if (ret == DXGI.DXGI_ERROR_INVALID_CALL)
            Log.Error("invalid call to resizeBuffers");

        this.scene?.OnPostResize((int)width, (int)height);

        return ret;
    }

    /// <summary>Represents <c>DXGISwapChain</c> in ReShade.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ReShadeDxgiSwapChain
    {
        // DXGISwapChain only implements IDXGISwapChain4. The only vtable should be that.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDXGISwapChain* AsIDxgiSwapChain() => (IDXGISwapChain*)Unsafe.AsPointer(ref this);
    }
}

using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal unsafe partial class InterfaceManager
{
    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        this.backend?.OnPreResize();
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        DXGI_SWAP_CHAIN_DESC desc;
        if (swapChainNative->GetDesc(&desc).FAILED)
            return;

        this.backend?.OnPostResize((int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
    }

    private void ReShadeAddonInterfaceOnPresent(
        ref ReShadeAddonInterface.ApiObject runtime,
        ref ReShadeAddonInterface.ApiObject swapChain,
        ReadOnlySpan<RECT> sourceRect,
        ReadOnlySpan<RECT> destRect,
        ReadOnlySpan<RECT> dirtyRects)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();

        if (this.RenderDalamudCheckAndInitialize(swapChainNative, 0) is { } activebackend)
            this.RenderDalamudDraw(activebackend);
    }

    private void ReShadeAddonInterfaceOnReShadeOverlay(ref ReShadeAddonInterface.ApiObject runtime)
    {
        var swapChainNative = runtime.GetNative<IDXGISwapChain>();

        if (this.RenderDalamudCheckAndInitialize(swapChainNative, 0) is { } activebackend)
            this.RenderDalamudDraw(activebackend);
    }

    private int AsReShadeAddonDxgiSwapChainResizeBuffersDetour(
        IDXGISwapChain* swapChain,
        uint bufferCount,
        uint width,
        uint height,
        DXGI_FORMAT newFormat,
        uint swapChainFlags)
    {
        // Hooked vtbl instead of registering ReShade event. This check is correct.
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        this.ResizeBuffers?.InvokeSafely();
        return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
    }
}

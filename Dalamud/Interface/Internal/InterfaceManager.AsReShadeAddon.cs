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
    // ReShade splits a resize across destroy and init callbacks; retain ownership across that callback boundary.
    private bool reShadeResizeEntered;

    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        // OnInitSwapChain releases this exclusion after ReShade has recreated the target.
        this.backend?.EnterResize();
        this.reShadeResizeEntered = true;

        // Drain deferred render cleanup while no render pass is active.
        this.RetireResourcesForResize();

        this.backend?.OnPreResize();
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        // Keep all validation inside the try so every path balances the exclusion acquired by the destroy callback.
        try
        {
            var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
            if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
                return;

            DXGI_SWAP_CHAIN_DESC desc;
            if (swapChainNative->GetDesc(&desc).FAILED)
                return;

            this.backend?.OnPostResize((int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
        }
        finally
        {
            // Balance OnDestroySwapChain even when target validation or GetDesc fails.
            if (this.reShadeResizeEntered)
            {
                this.reShadeResizeEntered = false;
                this.backend?.ExitResize();
            }
        }
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

        // Exclude frame capture and worker-thread rendering for the complete back-buffer reallocation.
        this.backend?.EnterResize();
        try
        {
            this.RetireResourcesForResize();
            this.ResizeBuffers?.InvokeSafely();
            return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        }
        finally
        {
            this.backend?.ExitResize();
        }
    }
}

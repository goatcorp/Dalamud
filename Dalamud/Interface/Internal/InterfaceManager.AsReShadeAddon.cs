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
    // Records an enter from the destroy callback for release during init. Callbacks must pair on the same thread;
    // this flag does not track the swap-chain identity or count overlapping resize acquisitions.
    private bool reShadeResizeEntered;

    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        // The corresponding init callback must release resize exclusion on this thread.
        this.backend?.EnterResize();
        this.reShadeResizeEntered = true;

        // Drain deferred render cleanup while no render pass is active.
        this.RetireResourcesForResize();

        this.backend?.OnPreResize();
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        // Run the release check even if target validation or GetDesc returns early.
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
            // Release the recorded destroy-callback acquisition; same-thread pairing is required.
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
        // This is a DXGI detour, so identify the game target from the native swap-chain interface.
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        // Request snapshot exclusion across reallocation. Destroy/init callbacks may also enter resize;
        // the backend does not count nested acquisitions, so their exits can release this acquisition.
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

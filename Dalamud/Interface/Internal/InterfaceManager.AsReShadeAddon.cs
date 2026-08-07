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
    // The ReShade-addon resize path is split across two asymmetric callbacks (OnDestroySwapChain ->
    // OnInitSwapChain). This tracks whether we entered the backend's resize-exclusive section in
    // OnDestroySwapChain so the matching ExitResize in OnInitSwapChain is balanced exactly once, even when a
    // callback early-outs.
    private bool reShadeResizeEntered;

    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.backend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        // Enter the resize-exclusive section so no pacer-thread render composites to the swap chain while its
        // back buffers are reallocated. ExitResize is balanced in OnInitSwapChain via reShadeResizeEntered.
        this.backend?.EnterResize();
        this.reShadeResizeEntered = true;

        // Retire anything sized for the old swap chain while the write lock is held (no render pass active).
        this.RetireResourcesForResize();

        this.backend?.OnPreResize();
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        // IMPORTANT: This callback is responsible for balancing the EnterResize() taken in
        // OnDestroySwapChain. We must NOT early-return before the finally below, or the backend's resize
        // write lock would be held forever and Step()/Render() would early-return on resizeInProgress for the
        // rest of the session - freezing the rendered image while the game keeps running (audio/input still
        // work). So the whole body, including the attached-target check, runs inside the try/finally.
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
            // Balance the EnterResize from OnDestroySwapChain, even on the attached-target/GetDesc early-outs.
            // ExitResize() is itself defensive (no-ops if the section is not actually held), so calling it here
            // is always safe.
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

        // Take the backend's resize write lock for the whole reallocation window, same as the DXGI path, so no
        // pacer-thread render composites to the swap chain while its back buffers are reallocated.
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

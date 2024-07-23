using System.Diagnostics;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal partial class InterfaceManager
{
    private unsafe void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeHandling.ReShadeAddonInterface.ApiObject swapchain)
    {
        var swapChain = swapchain.GetNative<IDXGISwapChain>();
        if (this.scene?.SwapChain.NativePointer != (nint)swapChain)
            return;

        this.scene?.OnPreResize();
    }

    private unsafe void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeHandling.ReShadeAddonInterface.ApiObject swapchain)
    {
        var swapChain = swapchain.GetNative<IDXGISwapChain>();
        if (this.scene?.SwapChain.NativePointer != (nint)swapChain)
            return;

        DXGI_SWAP_CHAIN_DESC desc;
        if (swapChain->GetDesc(&desc).FAILED)
            return;

        this.scene?.OnPostResize((int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
    }

    private void ReShadeAddonInterfaceOnReShadeOverlay(ref ReShadeHandling.ReShadeAddonInterface.ApiObject runtime)
    {
        var swapChain = runtime.GetNative();

        if (this.scene == null)
            this.InitScene(swapChain);

        if (this.scene?.SwapChain.NativePointer != swapChain)
            return;

        Debug.Assert(this.dalamudAtlas is not null, "this.dalamudAtlas is not null");

        if (!this.dalamudAtlas!.HasBuiltAtlas)
        {
            if (this.dalamudAtlas.BuildTask.Exception != null)
            {
                // TODO: Can we do something more user-friendly here? Unload instead?
                Log.Error(this.dalamudAtlas.BuildTask.Exception, "Failed to initialize Dalamud base fonts");
                Util.Fatal("Failed to initialize Dalamud base fonts.\nPlease report this error.", "Dalamud");
            }

            return;
        }

        this.CumulativePresentCalls++;
        this.IsMainThreadInPresent = true;

        while (this.runBeforeImGuiRender.TryDequeue(out var action))
            action.InvokeSafely();

        RenderImGui(this.scene!);
        this.PostImGuiRender();
        this.IsMainThreadInPresent = false;
    }

    private nint AsReShadeAddonResizeBuffersDetour(
        nint swapChain,
        uint bufferCount,
        uint width,
        uint height,
        uint newFormat,
        uint swapChainFlags)
    {
        // Hooked vtbl instead of registering ReShade event. This check is correct.
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        this.ResizeBuffers?.InvokeSafely();
        return this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
    }
}

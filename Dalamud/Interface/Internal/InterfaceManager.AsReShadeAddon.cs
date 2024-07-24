using System.Diagnostics;

using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal partial class InterfaceManager
{
    private unsafe void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.scene?.SwapChain.NativePointer != (nint)swapChainNative)
            return;

        this.scene?.OnPreResize();
    }

    private unsafe void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        if (this.scene?.SwapChain.NativePointer != (nint)swapChainNative)
            return;

        DXGI_SWAP_CHAIN_DESC desc;
        if (swapChainNative->GetDesc(&desc).FAILED)
            return;

        this.scene?.OnPostResize((int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
    }

    private void ReShadeAddonInterfaceOnPresent(
        ref ReShadeAddonInterface.ApiObject runtime,
        ref ReShadeAddonInterface.ApiObject swapChain,
        ReadOnlySpan<RECT> sourceRect,
        ReadOnlySpan<RECT> destRect,
        ReadOnlySpan<RECT> dirtyRects)
    {
        var swapChainNative = swapChain.GetNative();

        if (this.scene == null)
            this.InitScene(swapChainNative);

        if (this.scene?.SwapChain.NativePointer != swapChainNative)
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

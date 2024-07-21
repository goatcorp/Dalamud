using System.Diagnostics;

using Dalamud.Utility;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal partial class InterfaceManager
{
    private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
    {
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.dxgiPresentHook!.Original(swapChain, syncInterval, presentFlags);

        Debug.Assert(this.dxgiPresentHook is not null, "How did PresentDetour get called when presentHook is null?");
        Debug.Assert(this.dalamudAtlas is not null, "dalamudAtlas should have been set already");

        if (this.scene == null)
            this.InitScene(swapChain);

        Debug.Assert(this.scene is not null, "InitScene did not set the scene field, but did not throw an exception.");

        if (!this.dalamudAtlas!.HasBuiltAtlas)
        {
            if (this.dalamudAtlas.BuildTask.Exception != null)
            {
                // TODO: Can we do something more user-friendly here? Unload instead?
                Log.Error(this.dalamudAtlas.BuildTask.Exception, "Failed to initialize Dalamud base fonts");
                Util.Fatal("Failed to initialize Dalamud base fonts.\nPlease report this error.", "Dalamud");
            }

            return this.dxgiPresentHook!.Original(swapChain, syncInterval, presentFlags);
        }

        this.CumulativePresentCalls++;
        this.IsMainThreadInPresent = true;

        while (this.runBeforeImGuiRender.TryDequeue(out var action))
            action.InvokeSafely();

        RenderImGui(this.scene!);
        this.PostImGuiRender();
        this.IsMainThreadInPresent = false;

        return this.dxgiPresentHook!.Original(swapChain, syncInterval, presentFlags);
    }

    private IntPtr AsHookResizeBuffersDetour(
        IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
    {
        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

#if DEBUG
        Log.Verbose(
            $"Calling resizebuffers swap@{swapChain.ToInt64():X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

        this.ResizeBuffers?.InvokeSafely();

        this.scene?.OnPreResize();

        var ret = this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        if (ret.ToInt64() == 0x887A0001)
        {
            Log.Error("invalid call to resizeBuffers");
        }

        this.scene?.OnPostResize((int)width, (int)height);

        return ret;
    }
}

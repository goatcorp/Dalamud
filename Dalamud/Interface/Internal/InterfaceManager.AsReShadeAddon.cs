using System.Collections.Generic;
using System.Threading;

using Dalamud.Interface.ImGuiBackend;
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
    // Serialize lifecycle bookkeeping and resource updates; no lock spans separate callbacks.
    // Filter secondary targets first: Step emits their callbacks under snapshot write exclusion.
    private readonly Lock reShadeResizeLock = new();
    private readonly Dictionary<nint, ReShadeResizeState> reShadeResizes = new();

    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        using var callback = this.callbackLifetime.Enter();
        if (!callback.CanUseBackend)
            return;

        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        var activeBackend = this.backend;
        if (activeBackend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        using (this.reShadeResizeLock.EnterScope())
        {
            var identity = (nint)swapChain.AsPointer();
            if (!this.reShadeResizes.TryGetValue(identity, out var state))
                this.reShadeResizes.Add(identity, state = new());

            // Pair each destroy with its own backend scope; an unmatched destroy keeps rendering suspended.
            state.AwaitingInit.Push((activeBackend, activeBackend.BeginResize()));
            activeBackend.OnPreResize();
        }
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        using var callback = this.callbackLifetime.Enter();
        if (!callback.CanUseBackend)
            return;

        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        var activeBackend = this.backend;
        if (activeBackend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        using (this.reShadeResizeLock.EnterScope())
        {
            var identity = (nint)swapChain.AsPointer();
            if (!this.reShadeResizes.TryGetValue(identity, out var state))
                return;

            (IWin32Backend Backend, IDisposable Scope) pending;
            if (state.AwaitingInit.TryPeek(out pending))
            {
                if (!ReferenceEquals(pending.Backend, activeBackend))
                    return;

                // Record receipt before resource work, which may fail and require a later init to recover.
                state.Recovery.Add(pending);
                state.AwaitingInit.Pop();
            }
            else
            {
                if (state.Recovery.Count == 0)
                    return;

                pending = state.Recovery[^1];
                if (!ReferenceEquals(pending.Backend, activeBackend))
                    return;
            }

            DXGI_SWAP_CHAIN_DESC desc;
            if (swapChainNative->GetDesc(&desc).FAILED)
                return;

            // Successful restoration releases this backend's recovery scopes; outer scopes remain pending.
            pending.Backend.OnPostResize((int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
            for (var i = state.Recovery.Count - 1; i >= 0; i--)
            {
                var recovery = state.Recovery[i];
                if (!ReferenceEquals(recovery.Backend, activeBackend))
                    continue;

                recovery.Scope.Dispose();
                state.Recovery.RemoveAt(i);
            }

            if (state.AwaitingInit.Count == 0 && state.Recovery.Count == 0)
                this.reShadeResizes.Remove(identity);
        }
    }

    /// <summary>Releases outstanding resize scopes after interface callbacks have drained.</summary>
    private void ClearReShadeResizeScopes()
    {
        using (this.reShadeResizeLock.EnterScope())
        {
            foreach (var state in this.reShadeResizes.Values)
            {
                while (state.AwaitingInit.TryPeek(out var pending))
                {
                    pending.Scope.Dispose();
                    state.AwaitingInit.Pop();
                }

                while (state.Recovery.Count != 0)
                {
                    state.Recovery[^1].Scope.Dispose();
                    state.Recovery.RemoveAt(state.Recovery.Count - 1);
                }
            }

            this.reShadeResizes.Clear();
        }
    }

    private void ReShadeAddonInterfaceOnPresent(
        ref ReShadeAddonInterface.ApiObject runtime,
        ref ReShadeAddonInterface.ApiObject swapChain,
        ReadOnlySpan<RECT> sourceRect,
        ReadOnlySpan<RECT> destRect,
        ReadOnlySpan<RECT> dirtyRects)
    {
        using var callback = this.callbackLifetime.Enter();
        if (!callback.CanUseBackend)
            return;

        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();

        this.RenderDalamudDraw(swapChainNative, 0);
    }

    private void ReShadeAddonInterfaceOnReShadeOverlay(ref ReShadeAddonInterface.ApiObject runtime)
    {
        using var callback = this.callbackLifetime.Enter();
        if (!callback.CanUseBackend)
            return;

        var swapChainNative = runtime.GetNative<IDXGISwapChain>();

        this.RenderDalamudDraw(swapChainNative, 0);
    }

    private int AsReShadeAddonDxgiSwapChainResizeBuffersDetour(
        IDXGISwapChain* swapChain,
        uint bufferCount,
        uint width,
        uint height,
        DXGI_FORMAT newFormat,
        uint swapChainFlags)
    {
        using var callback = this.callbackLifetime.Enter();
        var original = callback.IsTracked ? this.dxgiSwapChainResizeBuffersHook!.Original : this.dxgiSwapChainResizeBuffersFallback!;

        // This is a DXGI detour, so identify the game target from the native swap-chain interface.
        if (!callback.CanUseBackend || !SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        using var resize = this.backend?.BeginResize();
        this.ResizeBuffers?.InvokeSafely();
        return original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
    }

    /// <summary>Tracks resize callbacks for one ReShade swap-chain identity.</summary>
    private sealed class ReShadeResizeState
    {
        /// <summary>Gets destroy scopes whose matching init callbacks have not arrived.</summary>
        public Stack<(IWin32Backend Backend, IDisposable Scope)> AwaitingInit { get; } = new();

        /// <summary>Gets received init scopes retained until resource restoration succeeds.</summary>
        public List<(IWin32Backend Backend, IDisposable Scope)> Recovery { get; } = new();
    }
}

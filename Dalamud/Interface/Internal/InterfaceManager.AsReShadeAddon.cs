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
    // Only short callback transactions take this lock. No thread-owned lock spans destroy/init callbacks.
    // Filter secondary targets before taking it: Step can emit their callbacks while holding the snapshot writer.
    private readonly Lock reShadeResizeLock = new();
    private readonly Dictionary<nint, ReShadeResizeState> reShadeResizes = new();

    private void ReShadeAddonInterfaceOnDestroySwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
        var swapChainNative = swapChain.GetNative<IDXGISwapChain>();
        var activeBackend = this.backend;
        if (activeBackend?.IsAttachedToPresentationTarget((nint)swapChainNative) is not true)
            return;

        using (this.reShadeResizeLock.EnterScope())
        {
            var identity = (nint)swapChain.AsPointer();
            if (!this.reShadeResizes.TryGetValue(identity, out var state))
                this.reShadeResizes.Add(identity, state = new());

            // Retain the exact backend and acquisition. Nested callbacks and the DXGI detour own other scopes.
            // A destroy without an init leaves the target suspended, without stranding a thread-owned writer.
            state.AwaitingInit.Push((activeBackend, activeBackend.BeginResize()));
            activeBackend.OnPreResize();
        }
    }

    private void ReShadeAddonInterfaceOnInitSwapChain(ref ReShadeAddonInterface.ApiObject swapChain)
    {
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

                // This native init has arrived even if our descriptor query or resource update fails.
                // Keep its suspension for recovery, separately from genuine outer callbacks still awaiting init.
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

            // A successful native lifecycle may follow an earlier init we rejected. Restoring this backend
            // also recovers those completed callbacks, without releasing outer callbacks or detour scopes.
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

        using var resize = this.backend?.BeginResize();
        this.ResizeBuffers?.InvokeSafely();
        return this.dxgiSwapChainResizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
    }

    private sealed class ReShadeResizeState
    {
        public Stack<(IWin32Backend Backend, IDisposable Scope)> AwaitingInit { get; } = new();

        public List<(IWin32Backend Backend, IDisposable Scope)> Recovery { get; } = new();
    }
}

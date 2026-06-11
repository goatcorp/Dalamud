using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.ImGuiBackend.Delegates;
using Dalamud.Interface.ImGuiBackend.Helpers;
using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;
using Dalamud.Utility;

using Serilog;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>
/// Backend for ImGui, using <see cref="Dx11Renderer"/> and <see cref="Win32InputHandler"/>.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal sealed unsafe class Dx11Win32Backend : IWin32Backend
{
    private readonly Dx11Renderer imguiRenderer;
    private readonly Win32InputHandler imguiInput;

    // When using nvidia smooth motion, Render() through our present hook is called multiple times per Step() to render interpolated frames.
    // We need to decouple creating our draw datas from actually rendering them, so we deep-copy the draw data so that we can render it while
    // Dalamud and plugin code is potentially mutating the next frame's draw data for the next Render() call.
    // Otherwise, we would be updating ImGui multiple times per game-tick which causes large problems with plugin (and our own) code,
    // which relies on executing in-step with the game tick.
    //
    // Lock-acquisition order / contract for drawDataLock:
    //   - Render() is serialized by renderLock, then takes the READ lock to render the stable snapshot.
    //   - Step() takes the WRITE lock for the (short) draw-data copy.
    //   - A swap-chain resize takes the WRITE lock for the whole resize window via EnterResize()/ExitResize(),
    //     guaranteeing no pacer-thread Render() is compositing while the swap chain's back buffers are reallocated.
    //   - resizeInProgress is checked lock-free as a fast-path skip in Step()/Render(); correctness is still
    //     guaranteed by the write lock, the flag just avoids queuing work behind the resize writer.
    //   - EnterResize()/ExitResize() must be paired on the SAME thread and must not be nested with Step()/Render()
    //     on that thread (the lock is NoRecursion).
    //
    // The D3D11 immediate context and the renderer's dynamic buffers are not safe for concurrent use. Smooth
    // Motion can invoke our present hook from multiple threads, so renderLock must be acquired before the read
    // lock. This keeps render calls waiting outside drawDataLock instead of making them active readers that block
    // Step() or a swap-chain resize.
    private readonly object renderLock = new();
    private readonly ReaderWriterLockSlim drawDataLock = new(LockRecursionPolicy.NoRecursion);
    private readonly DrawDataSnapshot snapshot = new();

    // Deep copies of every secondary (multi-viewport / pop-out) window's draw data captured under the write lock
    // during Step(), so the pacer thread can render and present them in Render() without ever touching the live,
    // single-buffered ImGui platform-IO viewport state (which ImGui.RenderPlatformWindowsDefault() would walk and
    // which the game-present thread mutates each Step() via NewFrame()/UpdatePlatformWindows()).
    private readonly ViewportSnapshot viewportSnapshots = new();

    // Set true for the whole swap-chain resize window (see EnterResize/ExitResize). Checked lock-free as a
    // fast-path skip in Step()/Render().
    private volatile bool resizeInProgress;

    // Managed thread id holding the resize-exclusive section (0 = none). Guards EnterResize()/ExitResize()
    // against unbalanced calls from the asymmetric resize detours, which would otherwise either self-deadlock
    // the NoRecursion write lock (double-enter) or throw / wedge the lock forever (exit-without-enter).
    private int resizeOwnerThreadId;

    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11Multithread> deviceMultithread;
    private bool restoreMultithreadProtection;

    // 0 = RenderPlatformWindowsDefault() still needs to be called for the current step, 1 = already done
    private int platformWindowsRenderedForStep = 1;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx11Win32Backend"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain"/>. The reference is copied.</param>
    /// <param name="enableMultithreadProtection">
    /// Whether to enable D3D11 runtime serialization for Smooth Motion's cross-thread immediate-context access.
    /// </param>
    public Dx11Win32Backend(IDXGISwapChain* swapChain, bool enableMultithreadProtection)
    {
        try
        {
            this.swapChainPossiblyWrapped = new(swapChain);
            this.swapChain = new(swapChain);
            fixed (ComPtr<IDXGISwapChain>* ppSwapChain = &this.swapChain)
                ReShadePeeler.PeelSwapChain(ppSwapChain);

            fixed (Guid* guid = &IID.IID_ID3D11Device)
            fixed (ID3D11Device** pp = &this.device.GetPinnableReference())
                this.swapChain.Get()->GetDevice(guid, (void**)pp).ThrowOnError();

            fixed (ID3D11DeviceContext** pp = &this.deviceContext.GetPinnableReference())
                this.device.Get()->GetImmediateContext(pp);

            if (enableMultithreadProtection)
            {
                // Smooth Motion uses its own capture and pacing threads while Dalamud renders through the game's
                // immediate context. Protect the shared context at the D3D11 runtime level so calls from the game,
                // NVIDIA and Dalamud cannot race each other. A private managed lock only protects Dalamud calls.
                fixed (Guid* guid = &IID.IID_ID3D11Multithread)
                fixed (ID3D11Multithread** pp = &this.deviceMultithread.GetPinnableReference())
                {
                    if (this.deviceContext.Get()->QueryInterface(guid, (void**)pp).SUCCEEDED)
                    {
                        var alreadyProtected = this.deviceMultithread.Get()->SetMultithreadProtected(BOOL.TRUE);
                        this.restoreMultithreadProtection = !alreadyProtected;
                        Log.Information(
                            "D3D11 multithread protection enabled for Smooth Motion (was already protected: {Already}).",
                            (bool)alreadyProtected);
                    }
                    else
                    {
                        Log.Warning(
                            "D3D11 multithread protection NOT enabled for Smooth Motion: ID3D11Multithread unavailable.");
                    }
                }
            }

            using var buffer = default(ComPtr<ID3D11Resource>);
            fixed (Guid* guid = &IID.IID_ID3D11Resource)
                this.swapChain.Get()->GetBuffer(0, guid, (void**)buffer.GetAddressOf()).ThrowOnError();

            var desc = default(DXGI_SWAP_CHAIN_DESC);
            this.swapChain.Get()->GetDesc(&desc).ThrowOnError();
            this.targetWidth = (int)desc.BufferDesc.Width;
            this.targetHeight = (int)desc.BufferDesc.Height;
            this.WindowHandle = desc.OutputWindow;

            var ctx = ImGui.CreateContext();
            ImGuizmo.SetImGuiContext(ctx);
            ImPlot.SetImGuiContext(ctx);
            ImPlot.CreateContext();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            this.imguiRenderer = new(this.SwapChain, this.Device, this.DeviceContext);
            this.imguiInput = new(this.WindowHandle);
        }
        catch
        {
            this.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Dx11Win32Backend"/> class.
    /// </summary>
    ~Dx11Win32Backend() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public event ImGuiBuildUiDelegate? BuildUi;

    /// <inheritdoc/>
    public event ImGuiNewInputFrameDelegate? NewInputFrame;

    /// <inheritdoc/>
    public event ImGuiNewRenderFrameDelegate? NewRenderFrame;

    /// <inheritdoc/>
    public event Action? PostCopy;

    /// <inheritdoc/>
    public bool UpdateCursor
    {
        get => this.imguiInput.UpdateCursor;
        set => this.imguiInput.UpdateCursor = value;
    }

    /// <inheritdoc/>
    public string? IniPath
    {
        get => this.imguiInput.IniPath;
        set => this.imguiInput.IniPath = value;
    }

    /// <inheritdoc/>
    public IImGuiInputHandler InputHandler => this.imguiInput;

    /// <inheritdoc/>
    public IImGuiRenderer Renderer => this.imguiRenderer;

    /// <inheritdoc/>
    public bool IsResizeInProgress => this.resizeInProgress;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="IDXGISwapChain"/>.
    /// </summary>
    public IDXGISwapChain* SwapChain => this.swapChain;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D11Device"/>.
    /// </summary>
    public ID3D11Device* Device => this.device;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D11Device"/>, in <see cref="nint"/>.
    /// </summary>
    public nint DeviceHandle => (nint)this.device.Get();

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D11DeviceContext"/>.
    /// </summary>
    public ID3D11DeviceContext* DeviceContext => this.deviceContext;

    /// <summary>
    /// Gets the window handle.
    /// </summary>
    public HWND WindowHandle { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam) =>
        this.imguiInput.ProcessWndProcW(hWnd, msg, wParam, lParam);

    /// <inheritdoc/>
    public void Step()
    {
        // Skip frame construction/snapshotting while a swap-chain resize holds (or is about to hold) the write
        // lock. This also avoids blocking the game-present thread behind a resize.
        if (this.resizeInProgress)
            return;

        this.imguiRenderer.OnNewFrame();
        this.NewRenderFrame?.Invoke();
        this.imguiInput.NewFrame(this.targetWidth, this.targetHeight);
        this.NewInputFrame?.Invoke();

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        // BuildUi (dalamud and plugin draw logic) can run outside the lock and doesn't need to block rendering
        this.BuildUi?.Invoke();

        ImGui.Render();

        // Snapshot the draw data under the write lock and signal that we want to render our viewports.
        // UpdatePlatformWindows() is moved INSIDE the write lock: it only lays out viewports and
        // creates/destroys/resizes their secondary windows (no GPU work), so it is safe on the game-present thread,
        // and holding the write lock across it guarantees a viewport create/destroy cannot race an in-flight
        // pacer-thread Render() (which holds the read lock). This is what lets us safely snapshot the live
        // per-viewport draw data immediately afterwards.
        this.drawDataLock.EnterWriteLock();
        try
        {
            ImGui.UpdatePlatformWindows();

            this.snapshot.CopyFrom(ImGui.GetDrawData().Handle);

            // Capture a stable, owned copy of EVERY viewport's draw data so the pacer thread never has to walk the
            // live ImGui platform-IO viewport list (which the next Step() mutates). Entry 0 is the main viewport.
            this.viewportSnapshots.BeginCapture();
            this.viewportSnapshots.Capture(ImGui.GetDrawData().Handle, nint.Zero, isMainViewport: true);

            var viewports = ImGui.GetPlatformIO().Viewports;
            for (var i = 1; i < viewports.Size; i++)
            {
                var viewport = viewports[i];

                // Skip viewports we don't own a renderer-side handle for (not yet created / being torn down).
                var rendererUserData = (nint)viewport.RendererUserData;
                if (rendererUserData == nint.Zero)
                    continue;

                this.viewportSnapshots.Capture(viewport.DrawData.Handle, rendererUserData, isMainViewport: false);
            }

            // PostCopy fires while the write lock is held (guaranteeing no render pass is active with the resources from the previous frame),
            // giving InterfaceManager a chance to retire the previous frame's resources
            this.PostCopy?.Invoke();

            // Signal that external viewports need to be rendered for this step. We can't do that here, it causes driver crashes
            // (nvidia code is rendering from other threads)
            Volatile.Write(ref this.platformWindowsRenderedForStep, 0);
        }
        finally
        {
            this.drawDataLock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Render()
    {
        // Fast-path skip while a resize is in progress; the write lock already guarantees correctness, this just
        // avoids queuing readers behind the resize writer.
        if (this.resizeInProgress)
            return;

        lock (this.renderLock)
        {
            // Resize may have started while this render call was waiting for another render to finish.
            if (this.resizeInProgress)
                return;

            // Prevent Step() from mutating the draw data copy.
            this.drawDataLock.EnterReadLock();
            try
            {
                // Render the main viewport (entry 0) through the existing main-viewport composite path.
                this.imguiRenderer.RenderDrawData(new ImDrawDataPtr(this.snapshot.Handle));

                // Render the secondary (multi-viewport / pop-out) windows exactly once per Step(). We deliberately do
                // NOT call ImGui.RenderPlatformWindowsDefault() here: that walks the live, single-buffered
                // ImGui.GetPlatformIO().Viewports list and reads each viewport's live DrawData, which the game-present
                // thread mutates in the next Step() (NewFrame()/UpdatePlatformWindows()) - enumerating/rendering that
                // while it is being mutated throws on the pacer thread. Instead we iterate the stable, owned snapshots
                // captured under the write lock in Step(), mirroring what RenderPlatformWindowsDefault() does per
                // viewport (RendererRenderWindow + RendererSwapBuffers) but against isolated copies.
                if (Interlocked.CompareExchange(ref this.platformWindowsRenderedForStep, 1, 0) == 0)
                {
                    for (var i = 1; i < this.viewportSnapshots.Count; i++)
                    {
                        var entry = this.viewportSnapshots[i];
                        this.imguiRenderer.RenderViewportSnapshot(
                            entry.RendererUserData,
                            new ImDrawDataPtr(entry.DrawData.Handle));
                    }
                }
            }
            finally
            {
                this.drawDataLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    public void EnterResize()
    {
        // Defensive: the asymmetric resize detours (especially the ReShade OnDestroy/OnInitSwapChain split) can
        // theoretically call EnterResize() twice without an intervening ExitResize(). Because drawDataLock is
        // NoRecursion, a second EnterWriteLock() on the SAME thread would self-deadlock and freeze the rendered
        // image forever (Step()/Render() keep early-returning on resizeInProgress). Ignore the nested enter.
        var currentThreadId = Environment.CurrentManagedThreadId;
        if (this.resizeOwnerThreadId == currentThreadId)
        {
            Log.Warning(
                "EnterResize() called re-entrantly on thread {ThreadId}; ignoring the nested enter to avoid a self-deadlock.",
                currentThreadId);
            return;
        }

        // Set the flag BEFORE taking the lock so an in-flight Step() that just missed the lock will early-out
        // next time, rather than racing to snapshot against the swap-chain reallocation.
        this.resizeInProgress = true;

        // Blocks until all current read-lock Render() passes drain, then prevents Step()/Render() for the
        // duration of the resize window.
        this.drawDataLock.EnterWriteLock();
        this.resizeOwnerThreadId = currentThreadId;

        // Drop any captured secondary-viewport snapshots: they are sized for the OLD swap chain and must not be
        // re-presented after the resize. The next Step() will recapture against the new swap chain.
        this.viewportSnapshots.BeginCapture();
    }

    /// <inheritdoc/>
    public void ExitResize()
    {
        // Defensive: only release the write lock if THIS backend believes the section is actually held. An
        // unbalanced ExitResize() (e.g. a detour that exits without ever entering) would otherwise throw
        // SynchronizationLockException, and a missing ExitResize() would leave the lock wedged forever. Clearing
        // state unconditionally keeps Step()/Render() from being frozen if the section was already released.
        if (this.resizeOwnerThreadId == 0)
        {
            Log.Warning("ExitResize() called without a matching EnterResize(); ignoring.");
            this.resizeInProgress = false;
            return;
        }

        this.resizeOwnerThreadId = 0;
        this.drawDataLock.ExitWriteLock();
        this.resizeInProgress = false;
    }

    /// <inheritdoc/>
    public void OnPreResize() => this.imguiRenderer.OnPreResize();

    /// <inheritdoc/>
    public void OnPostResize(int newWidth, int newHeight)
    {
        this.imguiRenderer.OnPostResize(newWidth, newHeight);
        this.targetWidth = newWidth;
        this.targetHeight = newHeight;
    }

    /// <inheritdoc/>
    public void InvalidateFonts() => this.imguiRenderer.RebuildFontTexture();

    /// <inheritdoc/>
    public bool IsImGuiCursor(nint cursorHandle) => this.imguiInput.IsImGuiCursor(cursorHandle);

    /// <inheritdoc/>
    public bool IsAttachedToPresentationTarget(nint targetHandle) =>
        AreIUnknownEqual(this.swapChain.Get(), (IUnknown*)targetHandle)
        || AreIUnknownEqual(this.swapChainPossiblyWrapped.Get(), (IUnknown*)targetHandle);

    /// <inheritdoc/>
    public bool IsMainViewportFullScreen()
    {
        BOOL fullscreen;
        this.swapChain.Get()->GetFullscreenState(&fullscreen, null);
        return fullscreen;
    }

    private static bool AreIUnknownEqual<T1, T2>(T1* punk1, T2* punk2)
        where T1 : unmanaged, IUnknown.Interface
        where T2 : unmanaged, IUnknown.Interface
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)
        // For any given COM object (also known as a COM component), a specific query for the IUnknown interface on any
        // of the object's interfaces must always return the same pointer value.

        if (punk1 is null || punk2 is null)
            return false;

        fixed (Guid* iid = &IID.IID_IUnknown)
        {
            using var u1 = default(ComPtr<IUnknown>);
            if (punk1->QueryInterface(iid, (void**)u1.GetAddressOf()).FAILED)
                return false;

            using var u2 = default(ComPtr<IUnknown>);
            if (punk2->QueryInterface(iid, (void**)u2.GetAddressOf()).FAILED)
                return false;

            return u1.Get() == u2.Get();
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.device.IsEmpty())
            return;

        this.imguiRenderer.Dispose();
        this.imguiInput.Dispose();

        this.snapshot.Dispose();
        this.viewportSnapshots.Dispose();

        ImPlot.DestroyContext();
        ImGui.DestroyContext();

        if (this.restoreMultithreadProtection && !this.deviceMultithread.IsEmpty())
            this.deviceMultithread.Get()->SetMultithreadProtected(BOOL.FALSE);
        this.deviceMultithread.Dispose();

        this.swapChain.Dispose();
        this.deviceContext.Dispose();
        this.device.Dispose();
        this.swapChainPossiblyWrapped.Dispose();
    }
}

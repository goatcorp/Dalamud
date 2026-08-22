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

    // Smooth Motion may call Render() several times, from several threads, for one game-thread Step(). Snapshots
    // keep those renders independent from the live ImGui state that plugins mutate during the following Step().
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
    private readonly Lock renderLock = new();
    private readonly ReaderWriterLockSlim drawDataLock = new(LockRecursionPolicy.NoRecursion);
    private readonly DrawDataSnapshot snapshot = new();

    // Secondary viewports also need snapshots because ImGui's platform viewport list is single-buffered and may
    // be mutated by the next Step() while an NVIDIA pacing thread renders the current frame.
    private readonly ViewportSnapshot viewportSnapshots = new();

    // Lock-free early-out for a resize; drawDataLock remains the synchronization mechanism.
    private volatile bool resizeInProgress;

    // Tracks resize ownership so asymmetric ReShade callbacks cannot recursively enter or incorrectly release
    // the non-recursive write lock.
    private int resizeOwnerThreadId;

    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11Multithread> deviceMultithread;
    private bool restoreMultithreadProtection;

    // Secondary swap chains are presented once per Step, even when the main snapshot is composited repeatedly.
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
        // Avoid constructing a frame that cannot be captured while resize owns the snapshot write lock.
        if (this.resizeInProgress)
            return;

        this.imguiRenderer.OnNewFrame();
        this.NewRenderFrame?.Invoke();
        this.imguiInput.NewFrame(this.targetWidth, this.targetHeight);
        this.NewInputFrame?.Invoke();

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        // Plugin UI does not touch snapshot storage, so it need not block an in-flight render.
        this.BuildUi?.Invoke();

        ImGui.Render();

        // Keep platform-window mutation and capture in one write transaction so pacing-thread renders cannot
        // observe a viewport being created, resized, or destroyed.
        this.drawDataLock.EnterWriteLock();
        try
        {
            ImGui.UpdatePlatformWindows();

            this.snapshot.CopyFrom(ImGui.GetDrawData().Handle);

            // Entry 0 mirrors the main snapshot; later entries are secondary platform windows.
            this.viewportSnapshots.BeginCapture();
            this.viewportSnapshots.Capture(ImGui.GetDrawData().Handle, nint.Zero, isMainViewport: true);

            var viewports = ImGui.GetPlatformIO().Viewports;
            for (var i = 1; i < viewports.Size; i++)
            {
                var viewport = viewports[i];

                // A missing renderer handle means the platform window is not ready to present.
                var rendererUserData = (nint)viewport.RendererUserData;
                if (rendererUserData == nint.Zero)
                    continue;

                this.viewportSnapshots.Capture(viewport.DrawData.Handle, rendererUserData, isMainViewport: false);
            }

            // Retire resources only after readers of the previous snapshot have drained.
            this.PostCopy?.Invoke();

            // Defer secondary presentation to Render(); presenting it here can race NVIDIA's worker threads.
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

            // Hold the snapshot stable for the complete main and secondary render pass.
            this.drawDataLock.EnterReadLock();
            try
            {
                // The main snapshot may be composited once for every generated frame.
                this.imguiRenderer.RenderDrawData(new ImDrawDataPtr(this.snapshot.Handle));

                // Secondary windows should not advance at Smooth Motion's generated-frame rate. Present their stable
                // snapshots once instead of walking ImGui's live platform viewport list from a pacing thread.
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
        // ReShade splits resize across destroy/init callbacks; ignore a duplicate enter rather than deadlocking
        // this non-recursive lock and permanently suppressing Step()/Render().
        var currentThreadId = Environment.CurrentManagedThreadId;
        if (this.resizeOwnerThreadId == currentThreadId)
        {
            Log.Warning(
                "EnterResize() called re-entrantly on thread {ThreadId}; ignoring the nested enter to avoid a self-deadlock.",
                currentThreadId);
            return;
        }

        // Publish intent before waiting so new Step()/Render() calls do not queue behind the resize writer.
        this.resizeInProgress = true;

        // Wait for active snapshot readers, then exclude frame capture and rendering until ExitResize().
        this.drawDataLock.EnterWriteLock();
        this.resizeOwnerThreadId = currentThreadId;

        // Do not present viewport snapshots captured against the old swap-chain buffers.
        this.viewportSnapshots.BeginCapture();
    }

    /// <inheritdoc/>
    public void ExitResize()
    {
        // An unmatched ReShade init callback must not throw or leave rendering disabled.
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

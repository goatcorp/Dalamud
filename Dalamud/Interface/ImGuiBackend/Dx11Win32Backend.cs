using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.ImGuiBackend.Delegates;
using Dalamud.Interface.ImGuiBackend.Helpers;
using Dalamud.Interface.ImGuiBackend.InputHandler;
using Dalamud.Interface.ImGuiBackend.Renderers;
using Dalamud.Interface.Internal;
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
    private readonly bool enableMultithreadProtection;

    // Snapshot synchronization: presentation threads may replay the main snapshot multiple times per Step.
    // Graphics operations take the native device lock first, matching callbacks that already hold it.
    // Render and viewport reads then take renderLock -> snapshot read; Step takes snapshot write for
    // platform updates, main capture, secondary presentation, and retirement. Serializing readers first
    // avoids blocking pending writers.
    // Resize scopes suspend replay/capture without retaining a lock across reallocation. Begin/end take
    // snapshot write alone; resource changes take device -> snapshot write. All readers recheck suspension.
    // The snapshot lock is non-recursive: do not begin/end resize scopes or re-enter graphics while holding it.
    private readonly Lock renderLock = new();
    private readonly ReaderWriterLockSlim drawDataLock = new(LockRecursionPolicy.NoRecursion);
    private readonly DrawDataSnapshot snapshot = new();

    // Owned native contexts and initialized companion bindings.
    private ImGuiContextPtr imguiContext;
    private ImPlotContextPtr implotContext;
    private bool imguizmoContextSet;
    private bool implotImGuiContextSet;
    private bool disposed;

    // Protected by drawDataLock; replay is allowed only after a complete capture.
    private bool hasSnapshot;

    // Detects resize during UI construction, which runs outside drawDataLock.
    private long resizeGeneration;

    // Counts pending and active resize scopes, regardless of their acquiring or completing thread.
    private int resizeRequests;

    // Retain the input interface for presentation-target identity checks, even when drawing uses a peeled chain.
    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    // Separately retained and ReShade-peeled interface used to acquire drawing resources.
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11Multithread> deviceMultithread;
    private bool restoreMultithreadProtection;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx11Win32Backend"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain"/>. The reference is copied.</param>
    /// <param name="enableMultithreadProtection">Whether NvPresent participation requires automatic context protection.</param>
    public Dx11Win32Backend(IDXGISwapChain* swapChain, bool enableMultithreadProtection)
    {
        this.enableMultithreadProtection = enableMultithreadProtection;
        var previousImGuiContext = default(ImGuiContextPtr);
        var previousImPlotContext = default(ImPlotContextPtr);
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

            // Native lock ordering applies in every mode, independently of automatic context protection.
            fixed (Guid* guid = &IID.IID_ID3D11Multithread)
            fixed (ID3D11Multithread** pp = &this.deviceMultithread.GetPinnableReference())
            {
                if (this.deviceContext.Get()->QueryInterface(guid, (void**)pp).FAILED)
                    this.deviceMultithread.Reset();
            }

            if (enableMultithreadProtection && !this.deviceMultithread.IsEmpty())
            {
                using (new DeviceLock(this))
                {
                    Log.Information(
                        "D3D11 multithread protection enabled for NvPresent (was already protected: {Already}).",
                        !this.restoreMultithreadProtection);
                }
            }
            else if (enableMultithreadProtection)
            {
                Log.Warning(
                    "D3D11 multithread protection NOT enabled for NvPresent: ID3D11Multithread unavailable.");
            }

            using var buffer = default(ComPtr<ID3D11Resource>);
            fixed (Guid* guid = &IID.IID_ID3D11Resource)
                this.swapChain.Get()->GetBuffer(0, guid, (void**)buffer.GetAddressOf()).ThrowOnError();

            var desc = default(DXGI_SWAP_CHAIN_DESC);
            this.swapChain.Get()->GetDesc(&desc).ThrowOnError();
            this.targetWidth = (int)desc.BufferDesc.Width;
            this.targetHeight = (int)desc.BufferDesc.Height;
            this.WindowHandle = desc.OutputWindow;

            // Successful construction leaves these contexts current; rollback restores the previous ones.
            previousImGuiContext = ImGui.GetCurrentContext();
            this.imguiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(this.imguiContext);
            ImGuizmo.SetImGuiContext(this.imguiContext);
            this.imguizmoContextSet = true;
            ImPlot.SetImGuiContext(this.imguiContext);
            this.implotImGuiContextSet = true;
            previousImPlotContext = ImPlot.GetCurrentContext();
            this.implotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(this.implotContext);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            this.imguiRenderer = new(this.SwapChain, this.Device, this.DeviceContext);
            this.imguiInput = new(this.WindowHandle);
        }
        catch
        {
            var restoreImGuiContext = this.imguiContext.Handle is not null;
            var restoreImPlotContext = this.implotContext.Handle is not null;
            try
            {
                this.Dispose();
            }
            catch (Exception exception)
            {
                // Preserve the initialization exception if rollback also fails.
                Log.Error(exception, "Error cleaning up a partially initialized ImGui backend.");
            }
            finally
            {
                // Suppress finalizer retries even if synchronous rollback failed.
                GC.SuppressFinalize(this);
                if (restoreImPlotContext)
                    ImPlot.SetCurrentContext(previousImPlotContext);
                if (restoreImGuiContext)
                    this.SetCurrentImGuiContext(previousImGuiContext);
            }

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
    public bool IsResizeInProgress => Volatile.Read(ref this.resizeRequests) != 0;

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
        ThreadSafety.AssertMainThread();

        // Sample before callbacks to detect resize even if it finishes during UI construction.
        var generation = Volatile.Read(ref this.resizeGeneration);

        if (this.IsResizeInProgress)
            return;

        this.imguiRenderer.OnNewFrame();
        this.NewRenderFrame?.Invoke();
        this.imguiInput.NewFrame(this.targetWidth, this.targetHeight);
        this.NewInputFrame?.Invoke();

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        // Build live UI before acquiring snapshot write access. Resources referenced by the previous snapshot
        // must remain valid until retirement, even if UI construction requests their disposal.
        this.BuildUi?.Invoke();

        ImGui.Render();

        // Complete platform updates, main capture, and secondary presentation under write exclusion.
        // Native factory hooks may acquire the device lock, so enter it first to preserve lock ordering.
        using var deviceLock = new DeviceLock(this);
        this.drawDataLock.EnterWriteLock();
        try
        {
            // Platform-window mutation or a partial copy can invalidate the previous snapshot even if Step fails.
            this.hasSnapshot = false;

            // ImGui requires platform completion before the next NewFrame, even if resize discards this capture.
            ImGui.UpdatePlatformWindows();

            // Discard construction overlapping resize without publishing or advancing resource retirement.
            if (this.IsResizeInProgress || generation != Volatile.Read(ref this.resizeGeneration))
                return;

            this.snapshot.CopyFrom(ImGui.GetDrawData().Handle);

            // Present may synchronously message the secondary HWND, so draw and present on its owning thread
            // before the kernel Present handoff. Write exclusion keeps live viewport data and resources stable.
            var viewports = ImGui.GetPlatformIO().Viewports;
            for (var i = 1; i < viewports.Size; i++)
            {
                var viewport = viewports[i];

                if ((viewport.Flags & ImGuiViewportFlags.Minimized) != 0)
                    continue;

                var rendererUserData = (nint)viewport.RendererUserData;
                if (rendererUserData == nint.Zero)
                    continue;

                this.imguiRenderer.RenderViewport(rendererUserData, viewport.DrawData);
            }

            // Retire previous-frame resources after replay readers drain and live secondary drawing completes.
            this.PostCopy?.Invoke();

            // Publish only after main capture, secondary presentation, and retirement succeed.
            this.hasSnapshot = true;
        }
        finally
        {
            this.drawDataLock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Render()
    {
        // Skip work while resize is announced, including the interval between resource callbacks.
        if (this.IsResizeInProgress)
            return;

        using (new DeviceLock(this))
        using (this.renderLock.EnterScope())
        {
            // Resize may have been announced while waiting for the graphics locks.
            if (this.IsResizeInProgress)
                return;

            // Hold the main snapshot stable for the complete render pass.
            this.drawDataLock.EnterReadLock();
            try
            {
                // Resize invalidates the captured presentation target; wait for a complete, fresh capture.
                if (this.IsResizeInProgress || !this.hasSnapshot)
                    return;

                // The main snapshot may be composited once for every generated frame.
                this.imguiRenderer.RenderDrawData(new ImDrawDataPtr(this.snapshot.Handle));
            }
            finally
            {
                this.drawDataLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    public bool TryReadViewportBackBuffer(uint viewportId, Action<nint> reader)
    {
        if (this.IsResizeInProgress)
            return false;

        using (new DeviceLock(this))
        using (this.renderLock.EnterScope())
        {
            this.drawDataLock.EnterReadLock();
            try
            {
                if (this.IsResizeInProgress)
                    return false;

                // The surrounding presentation/framework transaction excludes live ImGui platform mutations.
                var viewports = ImGui.GetPlatformIO().Viewports;
                for (var i = 0; i < viewports.Size; i++)
                {
                    var viewport = viewports[i];
                    if (viewport.ID != viewportId)
                        continue;

                    using var texture = i == 0
                                            ? GetMainViewportBackBuffer()
                                            : this.imguiRenderer.GetViewportBackBuffer((nint)viewport.RendererUserData);
                    reader((nint)texture.Get());
                    return true;
                }

                throw new ArgumentOutOfRangeException(nameof(viewportId), viewportId, "Could not find a viewport with the given ID.");
            }
            finally
            {
                this.drawDataLock.ExitReadLock();
            }
        }

        static ComPtr<ID3D11Texture2D> GetMainViewportBackBuffer()
        {
            // Middleware can make the effective display chain differ from the raw game swap chain.
            var texture = default(ComPtr<ID3D11Texture2D>);
            try
            {
                fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
                {
                    SwapChainHelper.GameDisplaySwapChain->GetBuffer(0, piid, (void**)texture.GetAddressOf())
                        .ThrowOnError();
                }

                return texture;
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public IDisposable BeginResize()
    {
        var scope = new ResizeScope(this);
        // Announce before draining readers. Every pending or active scope must finish before capture resumes.
        Interlocked.Increment(ref this.resizeRequests);
        try
        {
            this.drawDataLock.EnterWriteLock();
            try
            {
                // Deferred resources survive resize because an already-started Step can still use them.
                this.hasSnapshot = false;
                Interlocked.Increment(ref this.resizeGeneration);
            }
            finally
            {
                this.drawDataLock.ExitWriteLock();
            }
        }
        catch
        {
            Interlocked.Decrement(ref this.resizeRequests);
            throw;
        }

        return scope;
    }

    /// <inheritdoc/>
    public void OnPreResize()
    {
        using var deviceLock = new DeviceLock(this);
        this.drawDataLock.EnterWriteLock();
        try
        {
            this.imguiRenderer.OnPreResize();
        }
        finally
        {
            this.drawDataLock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void OnPostResize(int newWidth, int newHeight)
    {
        using var deviceLock = new DeviceLock(this);
        this.drawDataLock.EnterWriteLock();
        try
        {
            this.imguiRenderer.OnPostResize(newWidth, newHeight);
            this.targetWidth = newWidth;
            this.targetHeight = newHeight;
        }
        finally
        {
            this.drawDataLock.ExitWriteLock();
        }
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

    private void EndResize()
    {
        this.drawDataLock.EnterWriteLock();
        try
        {
            // Reject construction that began anywhere within this scope, even if no capture was attempted then.
            Interlocked.Increment(ref this.resizeGeneration);
            Interlocked.Decrement(ref this.resizeRequests);
        }
        finally
        {
            this.drawDataLock.ExitWriteLock();
        }
    }

    private void ReleaseUnmanagedResources()
    {
        // Cleanup is idempotent even when construction fails before device acquisition.
        if (this.disposed)
            return;
        this.disposed = true;

        try
        {
            this.ReleaseImGuiResources();
        }
        finally
        {
            // Release the snapshot and device references even if ImGui teardown fails.
            // The snapshot may be unassigned if a field initializer threw.
            this.snapshot?.Dispose();

            // Disable protection only if this backend enabled it from a previously disabled state.
            if (this.enableMultithreadProtection && this.restoreMultithreadProtection && !this.deviceMultithread.IsEmpty())
                this.deviceMultithread.Get()->SetMultithreadProtected(BOOL.FALSE);
            this.deviceMultithread.Dispose();

            this.swapChain.Dispose();
            this.deviceContext.Dispose();
            this.device.Dispose();
            this.swapChainPossiblyWrapped.Dispose();
        }
    }

    private void ReleaseImGuiResources()
    {
        // Early construction failure can leave native bindings unloaded and another context current.
        if (this.imguiContext.Handle is null)
            return;

        var context = this.imguiContext;
        var previousContext = ImGui.GetCurrentContext();
        // Renderer and input teardown require the owned context to remain current and alive.
        this.SetCurrentImGuiContext(context);
        try
        {
            try
            {
                this.imguiRenderer?.Dispose();
            }
            finally
            {
                this.imguiInput?.Dispose();
            }
        }
        finally
        {
            try
            {
                if (this.implotContext.Handle is not null)
                    ImPlot.DestroyContext(this.implotContext);
            }
            finally
            {
                this.implotContext = default;
                ImGui.DestroyContext(context);
                this.imguiContext = default;
                this.SetCurrentImGuiContext(previousContext != context ? previousContext : default);
            }
        }
    }

    private void SetCurrentImGuiContext(ImGuiContextPtr context)
    {
        ImGui.SetCurrentContext(context);
        // Companion DLLs have separate ImGui globals; touch only successfully initialized bindings.
        if (this.imguizmoContextSet)
            ImGuizmo.SetImGuiContext(context);
        if (this.implotImGuiContextSet)
            ImPlot.SetImGuiContext(context);
    }

    private readonly ref struct DeviceLock
    {
        private readonly ID3D11Multithread* multithread;

        public DeviceLock(Dx11Win32Backend backend)
        {
            this.multithread = backend.deviceMultithread.Get();
            if (this.multithread is null)
                return;

            this.multithread->Enter();

            // ReShade may restore temporary protection after backend initialization. Reassert the NvPresent
            // policy before context use and remember disabled-to-enabled transitions for disposal.
            if (backend.enableMultithreadProtection && !this.multithread->SetMultithreadProtected(BOOL.TRUE))
                backend.restoreMultithreadProtection = true;
        }

        public void Dispose()
        {
            if (this.multithread is not null)
                this.multithread->Leave();
        }
    }

    private sealed class ResizeScope(Dx11Win32Backend backend) : IDisposable
    {
        private readonly Lock disposeLock = new();
        private Dx11Win32Backend? backend = backend;

        public void Dispose()
        {
            using (this.disposeLock.EnterScope())
            {
                // Clear only after release succeeds, so an invalid recursive call does not lose its ownership.
                this.backend?.EndResize();
                this.backend = null;
            }
        }
    }
}

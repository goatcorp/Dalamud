using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx11Win32Backend"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain"/>. The reference is copied.</param>
    public Dx11Win32Backend(IDXGISwapChain* swapChain)
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
    public void Render()
    {
        this.imguiRenderer.OnNewFrame();
        this.NewRenderFrame?.Invoke();
        this.imguiInput.NewFrame(this.targetWidth, this.targetHeight);
        this.NewInputFrame?.Invoke();

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        this.BuildUi?.Invoke();

        ImGui.Render();

        this.imguiRenderer.RenderDrawData(ImGui.GetDrawData());

        ImGui.UpdatePlatformWindows();
        ImGui.RenderPlatformWindowsDefault();
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

        this.imguiRenderer?.Dispose();
        this.imguiInput?.Dispose();

        ImPlot.DestroyContext();
        ImGui.DestroyContext();

        this.swapChain.Dispose();
        this.deviceContext.Dispose();
        this.device.Dispose();
        this.swapChainPossiblyWrapped.Dispose();
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using ImGuiNET;

using ImGuizmoNET;

using ImPlotNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Backend for ImGui, using <see cref="Dx11Renderer"/> and <see cref="Win32InputHandler"/>.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal sealed unsafe class Dx11Win32Scene : IWin32Scene
{
    private readonly Dx11Renderer imguiRenderer;
    private readonly Win32InputHandler imguiInput;
    private readonly WicEasy wicEasy;

    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx11Win32Scene"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain"/>. The reference is copied.</param>
    public Dx11Win32Scene(IDXGISwapChain* swapChain)
    {
        this.wicEasy = new();
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
    /// Finalizes an instance of the <see cref="Dx11Win32Scene"/> class.
    /// </summary>
    ~Dx11Win32Scene() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public event IImGuiScene.BuildUiDelegate? BuildUi;

    /// <inheritdoc/>
    public event IImGuiScene.NewInputFrameDelegate? NewInputFrame;

    /// <inheritdoc/>
    public event IImGuiScene.NewRenderFrameDelegate? NewRenderFrame;

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
        this.wicEasy.Dispose();
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
    public bool SupportsTextureFormat(int format) =>
        this.SupportsTextureFormat((DXGI_FORMAT)format);

    /// <inheritdoc/>
    public bool SupportsTextureFormatForRenderTarget(int format) =>
        this.SupportsTextureFormat(
            (DXGI_FORMAT)format,
            D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D |
            D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_RENDER_TARGET);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2D(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool allowRenderTarget,
        [CallerMemberName] string debugName = "") =>
        this.imguiRenderer.CreateTexture2D(
            data,
            specs,
            cpuRead,
            cpuWrite,
            allowRenderTarget,
            debugName);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTextureFromImGuiViewport(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.imguiRenderer.CreateTextureFromImGuiViewport(args, ownerPlugin, debugName, cancellationToken);

    /// <inheritdoc/>
    public IDalamudTextureWrap WrapFromTextureResource(nint handle) =>
        this.imguiRenderer.WrapFromTextureResource(handle);

    /// <inheritdoc/>
    public RawImageSpecification GetTextureSpecification(IDalamudTextureWrap texture) =>
        this.imguiRenderer.GetTextureSpecification(texture);

    /// <inheritdoc/>
    public IntPtr GetTextureResource(IDalamudTextureWrap texture) => this.imguiRenderer.GetTextureResource(texture);

    /// <inheritdoc/>
    public void DrawTextureToTexture(
        IDalamudTextureWrap target,
        Vector2 targetUv0,
        Vector2 targetUv1,
        IDalamudTextureWrap source,
        Vector2 sourceUv0,
        Vector2 sourceUv1,
        bool copyAlphaOnly = false) =>
        this.imguiRenderer.DrawTextureToTexture(
            target,
            targetUv0,
            targetUv1,
            source,
            sourceUv0,
            sourceUv1,
            copyAlphaOnly);

    /// <inheritdoc cref="Dx11Renderer.CreateTexturePipeline"/>
    public ITexturePipelineWrap CreateTexturePipeline(ReadOnlySpan<byte> ps, in D3D11_SAMPLER_DESC samplerDesc) =>
        this.imguiRenderer.CreateTexturePipeline(ps, samplerDesc);

    /// <inheritdoc/>
    public void SetTexturePipeline(IDalamudTextureWrap textureHandle, ITexturePipelineWrap? pipelineHandle) =>
        this.imguiRenderer.SetTexturePipeline(textureHandle, pipelineHandle);

    /// <inheritdoc/>
    public ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap textureHandle) =>
        this.imguiRenderer.GetTexturePipeline(textureHandle);

    /// <inheritdoc/>
    public byte[] GetTextureData(IDalamudTextureWrap texture, out RawImageSpecification specification) =>
        this.imguiRenderer.GetTextureData(texture, out specification);

    /// <inheritdoc/>
    public bool IsImGuiCursor(nint cursorHandle) => this.imguiInput.IsImGuiCursor(cursorHandle);

    /// <inheritdoc/>
    public bool IsAttachedToPresentationTarget(nint targetHandle) =>
        this.swapChain.Get() == (void*)targetHandle
        || this.swapChainPossiblyWrapped.Get() == (void*)targetHandle;

    /// <inheritdoc/>
    public bool IsMainViewportFullScreen()
    {
        BOOL fullscreen;
        this.swapChain.Get()->GetFullscreenState(&fullscreen, null);
        return fullscreen;
    }

    /// <summary>
    /// Determines whether the current D3D11 Device supports the given DXGI format.
    /// </summary>
    /// <param name="dxgiFormat">DXGI format to check.</param>
    /// <param name="formatSupport">Formats to test. All formats must be supported, if multiple are specified.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsTextureFormat(
        DXGI_FORMAT dxgiFormat,
        D3D11_FORMAT_SUPPORT formatSupport = D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D)
    {
        var flags = 0u;
        if (this.Device->CheckFormatSupport(dxgiFormat, &flags).FAILED)
            return false;
        return (flags & (uint)formatSupport) == (uint)formatSupport;
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.device.IsEmpty())
            return;

        this.imguiRenderer.Dispose();
        this.imguiInput.Dispose();

        ImGui.DestroyContext();

        this.swapChain.Dispose();
        this.deviceContext.Dispose();
        this.device.Dispose();
        this.swapChainPossiblyWrapped.Dispose();
    }
}

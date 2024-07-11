using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Renderer for testing DX12 implementation onto DX11 render target.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe class Dx12OnDx11Win32Scene : IWin32Scene
{
    private readonly Dx12Win32Scene scene12;
    private readonly ComPtr<ID3D11ShaderResourceView>[] shaderResourceViewsD3D11;
    private readonly D3D11RectangleDrawer drawsOneSquare;
    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device1> device11;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11RenderTargetView> rtv;
    private ComPtr<ID3D12Device> device12;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx12OnDx11Win32Scene"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain"/>.</param>
    public Dx12OnDx11Win32Scene(IDXGISwapChain* swapChain)
    {
        try
        {
            this.swapChainPossiblyWrapped = new(swapChain);
            this.swapChain = new(swapChain);
            fixed (ComPtr<IDXGISwapChain>* ppSwapChain = &this.swapChain)
                ReShadePeeler.PeelSwapChain(ppSwapChain);

            fixed (Guid* guid = &IID.IID_ID3D11Device1)
            fixed (ID3D11Device1** pp = &this.device11.GetPinnableReference())
                this.swapChain.Get()->GetDevice(guid, (void**)pp).ThrowOnError();

            fixed (ID3D11DeviceContext** pp = &this.deviceContext.GetPinnableReference())
                this.device11.Get()->GetImmediateContext(pp);

            using var buffer = default(ComPtr<ID3D11Resource>);
            fixed (Guid* guid = &IID.IID_ID3D11Resource)
                this.swapChain.Get()->GetBuffer(0, guid, (void**)buffer.GetAddressOf()).ThrowOnError();

            fixed (ID3D11RenderTargetView** pp = &this.rtv.GetPinnableReference())
                this.device11.Get()->CreateRenderTargetView(buffer.Get(), null, pp).ThrowOnError();

            var desc = default(DXGI_SWAP_CHAIN_DESC);
            this.swapChain.Get()->GetDesc(&desc).ThrowOnError();
            this.targetWidth = (int)desc.BufferDesc.Width;
            this.targetHeight = (int)desc.BufferDesc.Height;
            this.WindowHandle = desc.OutputWindow;

#if DEBUG
            fixed (Guid* piid = &IID.IID_ID3D12Debug)
            {
                using var debug = default(ComPtr<ID3D12Debug>);
                DirectX.D3D12GetDebugInterface(piid, (void**)debug.GetAddressOf());
                debug.Get()->EnableDebugLayer();
            }
#endif

            fixed (Guid* piid = &IID.IID_ID3D12Device)
            fixed (ID3D12Device** ppDevice = &this.device12.GetPinnableReference())
            {
                DirectX.D3D12CreateDevice(
                    null,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                    piid,
                    (void**)ppDevice).ThrowOnError();
            }

            this.drawsOneSquare = new();
            this.drawsOneSquare.Setup(this.device11.Get());
            this.scene12 = new(this.device12, this.WindowHandle, this.targetWidth, this.targetHeight);
            this.shaderResourceViewsD3D11 = new ComPtr<ID3D11ShaderResourceView>[this.scene12.Renderer.NumBackBuffers];
        }
        catch
        {
            this.drawsOneSquare?.Dispose();
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Dx12OnDx11Win32Scene"/> class.
    /// </summary>
    ~Dx12OnDx11Win32Scene() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public event IImGuiScene.BuildUiDelegate? BuildUi
    {
        add => this.scene12.BuildUi += value;
        remove => this.scene12.BuildUi -= value;
    }

    /// <inheritdoc/>
    public event IImGuiScene.NewInputFrameDelegate? NewInputFrame
    {
        add => this.scene12.NewInputFrame += value;
        remove => this.scene12.NewInputFrame -= value;
    }

    /// <inheritdoc/>
    public event IImGuiScene.NewRenderFrameDelegate? NewRenderFrame
    {
        add => this.scene12.NewRenderFrame += value;
        remove => this.scene12.NewRenderFrame -= value;
    }

    /// <inheritdoc/>
    public bool UpdateCursor
    {
        get => this.scene12.UpdateCursor;
        set => this.scene12.UpdateCursor = value;
    }

    /// <inheritdoc/>
    public string? IniPath
    {
        get => this.scene12.IniPath;
        set => this.scene12.IniPath = value;
    }

    /// <summary>
    /// Gets the pointer to an instance of <see cref="IDXGISwapChain"/>.
    /// </summary>
    public IDXGISwapChain* SwapChain => this.swapChain;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D11Device"/>.
    /// </summary>
    public ID3D11Device1* Device => this.device11;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D11Device"/>, in <see cref="nint"/>.
    /// </summary>
    public nint DeviceHandle => (nint)this.device11.Get();

    /// <summary>
    /// Gets the window handle.
    /// </summary>
    public HWND WindowHandle { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.drawsOneSquare.Dispose();
        this.ReleaseUnmanagedResources();
        this.scene12.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam) =>
        this.scene12.ProcessWndProcW(hWnd, msg, wParam, lParam);

    /// <inheritdoc/>
    public void Render()
    {
        this.scene12.Render();

        this.scene12.Renderer.GetCurrentMainViewportRenderTarget(out var resource, out var backBufferIndex);
        ref var srv = ref this.shaderResourceViewsD3D11[backBufferIndex];
        if (srv.IsEmpty())
        {
            using var sharedHandle = Win32Handle.CreateSharedHandle(this.device12, resource);
            using var tex = default(ComPtr<ID3D11Texture2D>);
            fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
                this.device11.Get()->OpenSharedResource1(sharedHandle, piid, (void**)tex.GetAddressOf()).ThrowOnError();

            D3D11_TEXTURE2D_DESC desc;
            tex.Get()->GetDesc(&desc);

            using var srvTemp = default(ComPtr<ID3D11ShaderResourceView>);
            var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                tex,
                D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
            this.device11.Get()->CreateShaderResourceView((ID3D11Resource*)tex.Get(), &srvDesc, srvTemp.GetAddressOf())
                .ThrowOnError();
            srv.Swap(&srvTemp);
        }

        var prtv = this.rtv.Get();
        this.deviceContext.Get()->OMSetRenderTargets(1, &prtv, null);
        this.drawsOneSquare.Draw(
            this.deviceContext,
            this.rtv.Get(),
            Vector2.Zero,
            Vector2.One,
            srv,
            Vector2.Zero,
            Vector2.One,
            false);
        this.deviceContext.Get()->OMSetRenderTargets(0, null, null);
    }

    /// <inheritdoc/>
    public void OnPreResize()
    {
        this.rtv.Reset();
        foreach (ref var s in this.shaderResourceViewsD3D11.AsSpan())
            s.Reset();
        this.scene12.OnPreResize();
    }

    /// <inheritdoc/>
    public void OnPostResize(int newWidth, int newHeight)
    {
        using var buffer = default(ComPtr<ID3D11Resource>);
        fixed (Guid* guid = &IID.IID_ID3D11Resource)
            this.SwapChain->GetBuffer(0, guid, (void**)buffer.ReleaseAndGetAddressOf()).ThrowOnError();

        using var rtvTemp = default(ComPtr<ID3D11RenderTargetView>);
        this.Device->CreateRenderTargetView(buffer.Get(), null, rtvTemp.GetAddressOf()).ThrowOnError();
        this.rtv.Swap(&rtvTemp);

        this.targetWidth = newWidth;
        this.targetHeight = newHeight;
        this.scene12.OnPostResize(newWidth, newHeight);
    }

    /// <inheritdoc/>
    public void InvalidateFonts() => this.scene12.InvalidateFonts();

    /// <inheritdoc/>
    public bool IsImGuiCursor(nint cursorHandle) => this.scene12.IsImGuiCursor(cursorHandle);

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

    /// <inheritdoc/>
    public bool SupportsTextureFormat(int format) => this.scene12.SupportsTextureFormat(format);

    /// <inheritdoc/>
    public bool SupportsTextureFormatForRenderTarget(int format) =>
        this.scene12.SupportsTextureFormatForRenderTarget(format);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2D(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool allowRenderTarget,
        [CallerMemberName] string debugName = "") =>
        this.scene12.CreateTexture2D(
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
        this.scene12.CreateTextureFromImGuiViewport(args, ownerPlugin, debugName, cancellationToken);

    /// <inheritdoc/>
    public IDalamudTextureWrap WrapFromTextureResource(nint handle) =>
        this.scene12.WrapFromTextureResource(handle);

    /// <inheritdoc/>
    public RawImageSpecification GetTextureSpecification(IDalamudTextureWrap texture) =>
        this.scene12.GetTextureSpecification(texture);

    /// <inheritdoc/>
    public byte[] GetTextureData(IDalamudTextureWrap texture, out RawImageSpecification specification) =>
        this.scene12.GetTextureData(texture, out specification);

    /// <inheritdoc/>
    public IntPtr GetTextureResource(IDalamudTextureWrap texture) => this.scene12.GetTextureResource(texture);

    /// <inheritdoc/>
    public void DrawTextureToTexture(
        IDalamudTextureWrap target,
        Vector2 targetUv0,
        Vector2 targetUv1,
        IDalamudTextureWrap source,
        Vector2 sourceUv0,
        Vector2 sourceUv1,
        bool copyAlphaOnly = false) =>
        this.scene12.DrawTextureToTexture(target, targetUv0, targetUv1, source, sourceUv0, sourceUv1, copyAlphaOnly);

    /// <inheritdoc cref="Dx12Win32Scene.CreateTexturePipeline"/>
    public ITexturePipelineWrap CreateTexturePipeline(
        ReadOnlySpan<byte> ps,
        in D3D12_STATIC_SAMPLER_DESC samplerDesc,
        [CallerMemberName] string debugName = "")
        => this.scene12.CreateTexturePipeline(ps, samplerDesc, debugName);

    /// <inheritdoc/>
    public void SetTexturePipeline(IDalamudTextureWrap textureHandle, ITexturePipelineWrap? pipelineHandle) =>
        this.scene12.SetTexturePipeline(textureHandle, pipelineHandle);

    /// <inheritdoc/>
    public ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap textureHandle) =>
        this.scene12.GetTexturePipeline(textureHandle);

    private void ReleaseUnmanagedResources()
    {
        this.swapChain.Reset();
        this.device11.Reset();
        this.deviceContext.Reset();
        this.rtv.Reset();
        foreach (ref var s in this.shaderResourceViewsD3D11.AsSpan())
            s.Reset();
        this.device12.Reset();
        this.drawsOneSquare.Dispose();
        this.swapChainPossiblyWrapped.Dispose();
    }
}

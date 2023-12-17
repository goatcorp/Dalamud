using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Internal;
using Dalamud.Utility;

using ImGuiNET;

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
    private ComPtr<IDXGISwapChain> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain> swapChain;
    private ComPtr<ID3D11Device1> device11;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11RenderTargetView> rtv;
    private ComPtr<ID3D12Device> device12;
    private DrawsOneSquare drawsOneSquare;

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
                this.swapChain.Get()->GetDevice(guid, (void**)pp).ThrowHr();

            fixed (ID3D11DeviceContext** pp = &this.deviceContext.GetPinnableReference())
                this.device11.Get()->GetImmediateContext(pp);

            using var buffer = default(ComPtr<ID3D11Resource>);
            fixed (Guid* guid = &IID.IID_ID3D11Resource)
                this.swapChain.Get()->GetBuffer(0, guid, (void**)buffer.GetAddressOf()).ThrowHr();

            fixed (ID3D11RenderTargetView** pp = &this.rtv.GetPinnableReference())
                this.device11.Get()->CreateRenderTargetView(buffer.Get(), null, pp).ThrowHr();

            var desc = default(DXGI_SWAP_CHAIN_DESC);
            this.swapChain.Get()->GetDesc(&desc).ThrowHr();
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
                    (void**)ppDevice).ThrowHr();
            }

            this.drawsOneSquare.Setup(this.device11);
            this.scene12 = new(this.device12, this.WindowHandle, this.targetWidth, this.targetHeight);
            this.shaderResourceViewsD3D11 = new ComPtr<ID3D11ShaderResourceView>[this.scene12.Renderer.NumBackBuffers];
        }
        catch
        {
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
                this.device11.Get()->OpenSharedResource1(sharedHandle, piid, (void**)tex.GetAddressOf()).ThrowHr();

            D3D11_TEXTURE2D_DESC desc;
            tex.Get()->GetDesc(&desc);

            using var srvTemp = default(ComPtr<ID3D11ShaderResourceView>);
            var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                tex,
                D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
            this.device11.Get()->CreateShaderResourceView((ID3D11Resource*)tex.Get(), &srvDesc, srvTemp.GetAddressOf())
                .ThrowHr();
            srv.Swap(&srvTemp);
        }

        var prtv = this.rtv.Get();
        this.deviceContext.Get()->OMSetRenderTargets(1, &prtv, null);
        this.drawsOneSquare.Draw(this.deviceContext, srv, this.targetWidth, this.targetHeight);
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
            this.SwapChain->GetBuffer(0, guid, (void**)buffer.ReleaseAndGetAddressOf()).ThrowHr();

        using var rtvTemp = default(ComPtr<ID3D11RenderTargetView>);
        this.Device->CreateRenderTargetView(buffer.Get(), null, rtvTemp.GetAddressOf()).ThrowHr();
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
    public IDalamudTextureWrap CreateTexture2DFromFile(string path, [CallerMemberName] string debugName = "") =>
        this.scene12.CreateTexture2DFromFile(path, debugName);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2DFromBytes(
        ReadOnlySpan<byte> data,
        [CallerMemberName] string debugName = "") =>
        this.scene12.CreateTexture2DFromBytes(data, debugName);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2DFromRaw(
        ReadOnlySpan<byte> data,
        int pitch,
        int width,
        int height,
        int format,
        [CallerMemberName] string debugName = "") =>
        this.scene12.CreateTexture2DFromRaw(data, pitch, width, height, format, debugName);

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

    private struct DrawsOneSquare : IDisposable
    {
        private ComPtr<ID3D11SamplerState> sampler;
        private ComPtr<ID3D11VertexShader> vertexShader;
        private ComPtr<ID3D11PixelShader> pixelShader;
        private ComPtr<ID3D11InputLayout> inputLayout;
        private ComPtr<ID3D11Buffer> vertexConstantBuffer;
        private ComPtr<ID3D11BlendState> blendState;
        private ComPtr<ID3D11RasterizerState> rasterizerState;
        private ComPtr<ID3D11Buffer> vertexBuffer;
        private ComPtr<ID3D11Buffer> indexBuffer;

        public void Setup(ID3D11Device1* device)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Create the vertex shader
            if (this.vertexShader.IsEmpty() || this.inputLayout.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream("imgui-vertex.hlsl.bytes")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);
                fixed (byte* pArray = array)
                fixed (ID3D11VertexShader** ppShader = &this.vertexShader.GetPinnableReference())
                fixed (ID3D11InputLayout** ppInputLayout = &this.inputLayout.GetPinnableReference())
                fixed (void* pszPosition = "POSITION"u8)
                fixed (void* pszTexCoord = "TEXCOORD"u8)
                fixed (void* pszColor = "COLOR"u8)
                {
                    device->CreateVertexShader(pArray, (nuint)stream.Length, null, ppShader).ThrowHr();

                    var ied = stackalloc D3D11_INPUT_ELEMENT_DESC[]
                    {
                        new()
                        {
                            SemanticName = (sbyte*)pszPosition,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                            AlignedByteOffset = uint.MaxValue,
                        },
                        new()
                        {
                            SemanticName = (sbyte*)pszTexCoord,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                            AlignedByteOffset = uint.MaxValue,
                        },
                        new()
                        {
                            SemanticName = (sbyte*)pszColor,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                            AlignedByteOffset = uint.MaxValue,
                        },
                    };
                    device->CreateInputLayout(ied, 3, pArray, (nuint)stream.Length, ppInputLayout).ThrowHr();
                }

                ArrayPool<byte>.Shared.Return(array);
            }

            // Create the constant buffer
            if (this.vertexConstantBuffer.IsEmpty())
            {
                var bufferDesc = new D3D11_BUFFER_DESC(
                    (uint)sizeof(Matrix4x4),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
                var data = Matrix4x4.Identity;
                var subr = new D3D11_SUBRESOURCE_DATA { pSysMem = &data };
                fixed (ID3D11Buffer** ppBuffer = &this.vertexConstantBuffer.GetPinnableReference())
                    device->CreateBuffer(&bufferDesc, &subr, ppBuffer).ThrowHr();
            }

            // Create the pixel shader
            if (this.pixelShader.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream("imgui-frag.hlsl.bytes")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);
                fixed (byte* pArray = array)
                fixed (ID3D11PixelShader** ppShader = &this.pixelShader.GetPinnableReference())
                    device->CreatePixelShader(pArray, (nuint)stream.Length, null, ppShader).ThrowHr();

                ArrayPool<byte>.Shared.Return(array);
            }

            // Create the blending setup
            if (this.blendState.IsEmpty())
            {
                var blendStateDesc = new D3D11_BLEND_DESC
                {
                    RenderTarget =
                    {
                        e0 =
                        {
                            BlendEnable = true,
                            SrcBlend = D3D11_BLEND.D3D11_BLEND_SRC_ALPHA,
                            DestBlend = D3D11_BLEND.D3D11_BLEND_INV_SRC_ALPHA,
                            BlendOp = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                            SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_INV_SRC_ALPHA,
                            DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_ZERO,
                            BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                            RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALL,
                        },
                    },
                };
                fixed (ID3D11BlendState** ppBlendState = &this.blendState.GetPinnableReference())
                    device->CreateBlendState(&blendStateDesc, ppBlendState).ThrowHr();
            }

            // Create the rasterizer state
            if (this.rasterizerState.IsEmpty())
            {
                var rasterizerDesc = new D3D11_RASTERIZER_DESC
                {
                    FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
                    CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
                };
                fixed (ID3D11RasterizerState** ppRasterizerState = &this.rasterizerState.GetPinnableReference())
                    device->CreateRasterizerState(&rasterizerDesc, ppRasterizerState).ThrowHr();
            }

            // Create the font sampler
            if (this.sampler.IsEmpty())
            {
                var samplerDesc = new D3D11_SAMPLER_DESC(
                    D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
                    D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    0f,
                    0,
                    D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
                    null,
                    0,
                    0);
                fixed (ID3D11SamplerState** ppSampler = &this.sampler.GetPinnableReference())
                    device->CreateSamplerState(&samplerDesc, ppSampler).ThrowHr();
            }

            if (this.vertexBuffer.IsEmpty())
            {
                var data = stackalloc ImDrawVert[]
                {
                    new() { col = uint.MaxValue, pos = new(-1, 1), uv = new(0, 0) },
                    new() { col = uint.MaxValue, pos = new(-1, -1), uv = new(0, 1) },
                    new() { col = uint.MaxValue, pos = new(1, 1), uv = new(1, 0) },
                    new() { col = uint.MaxValue, pos = new(1, -1), uv = new(1, 1) },
                };
                var desc = new D3D11_BUFFER_DESC(
                    (uint)(sizeof(ImDrawVert) * 4),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
                var subr = new D3D11_SUBRESOURCE_DATA { pSysMem = data };
                var buffer = default(ID3D11Buffer*);
                device->CreateBuffer(&desc, &subr, &buffer).ThrowHr();
                this.vertexBuffer.Attach(buffer);
            }

            if (this.indexBuffer.IsEmpty())
            {
                var data = stackalloc ushort[] { 0, 1, 2, 1, 2, 3 };
                var desc = new D3D11_BUFFER_DESC(
                    sizeof(ushort) * 6,
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
                var subr = new D3D11_SUBRESOURCE_DATA { pSysMem = data };
                var buffer = default(ID3D11Buffer*);
                device->CreateBuffer(&desc, &subr, &buffer).ThrowHr();
                this.indexBuffer.Attach(buffer);
            }
        }

        public void Draw(ID3D11DeviceContext* ctx, ID3D11ShaderResourceView* srv, int width, int height)
        {
            ctx->IASetInputLayout(this.inputLayout);
            var buffer = this.vertexBuffer.Get();
            var stride = (uint)sizeof(ImDrawVert);
            var offset = 0u;
            ctx->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
            ctx->IASetIndexBuffer(this.indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
            ctx->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

            var viewport = new D3D11_VIEWPORT(0, 0, width, height);
            ctx->RSSetState(this.rasterizerState);
            ctx->RSSetViewports(1, &viewport);

            var blendColor = default(Vector4);
            ctx->OMSetBlendState(this.blendState, (float*)&blendColor, 0xffffffff);
            ctx->OMSetDepthStencilState(null, 0);

            ctx->VSSetShader(this.vertexShader.Get(), null, 0);
            buffer = this.vertexConstantBuffer.Get();
            ctx->VSSetConstantBuffers(0, 1, &buffer);

            ctx->PSSetShader(this.pixelShader, null, 0);
            var simp = this.sampler.Get();
            ctx->PSSetSamplers(0, 1, &simp);
            ctx->PSSetShaderResources(0, 1, &srv);
            ctx->GSSetShader(null, null, 0);
            ctx->HSSetShader(null, null, 0);
            ctx->DSSetShader(null, null, 0);
            ctx->CSSetShader(null, null, 0);
            ctx->DrawIndexed(6, 0, 0);
        }

        public void Dispose()
        {
            this.sampler.Reset();
            this.vertexShader.Reset();
            this.pixelShader.Reset();
            this.inputLayout.Reset();
            this.vertexConstantBuffer.Reset();
            this.blendState.Reset();
            this.rasterizerState.Reset();
            this.vertexBuffer.Reset();
            this.indexBuffer.Reset();
        }
    }
}

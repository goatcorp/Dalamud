using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using ImGuiNET;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private DrawsOneSquare? drawsOneSquare;

    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat) =>
        this.IsDxgiFormatSupportedForCreateFromExistingTextureAsync((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync"/>
    public bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(DXGI_FORMAT dxgiFormat)
    {
        if (this.interfaceManager.Scene is not { } scene)
        {
            _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
            scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
        }

        switch (dxgiFormat)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format
            // Video formats requiring use of another DXGI_FORMAT when using with CreateRenderTarget
            case DXGI_FORMAT.DXGI_FORMAT_AYUV:
            case DXGI_FORMAT.DXGI_FORMAT_NV12:
            case DXGI_FORMAT.DXGI_FORMAT_P010:
            case DXGI_FORMAT.DXGI_FORMAT_P016:
            case DXGI_FORMAT.DXGI_FORMAT_NV11:
                return false;
        }

        var format = (Format)dxgiFormat;
        var support = scene.Device.CheckFormatSupport(format);
        const FormatSupport required =
            FormatSupport.RenderTarget |
            FormatSupport.Texture2D;
        return (support & required) == required;
    }

    /// <inheritdoc/>
    Task<IDalamudTextureWrap> ITextureProvider.CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        Vector2 uv0,
        Vector2 uv1,
        int dxgiFormat,
        CancellationToken cancellationToken) =>
        this.CreateFromExistingTextureAsync(wrap, uv0, uv1, (DXGI_FORMAT)dxgiFormat, cancellationToken);

    /// <inheritdoc cref="ITextureProvider.CreateFromExistingTextureAsync"/>
    public Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        Vector2 uv0,
        Vector2 uv1,
        DXGI_FORMAT format,
        CancellationToken cancellationToken = default)
    {
        var wrapCopy = wrap.CreateWrapSharingLowLevelResource();
        return this.textureLoadThrottler.LoadTextureAsync(
                       new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
                       ct =>
                       {
                           var tcs = new TaskCompletionSource<IDalamudTextureWrap>();
                           this.interfaceManager.RunBeforePresent(
                               () =>
                               {
                                   try
                                   {
                                       ct.ThrowIfCancellationRequested();
                                       unsafe
                                       {
                                           using var tex = default(ComPtr<ID3D11Texture2D>);
                                           tex.Attach(
                                               this.NoThrottleCreateFromExistingTextureCore(
                                                   wrapCopy,
                                                   uv0,
                                                   uv1,
                                                   format,
                                                   false));

                                           using var device = default(ComPtr<ID3D11Device>);
                                           tex.Get()->GetDevice(device.GetAddressOf());

                                           using var srv = default(ComPtr<ID3D11ShaderResourceView>);
                                           var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                                               tex,
                                               D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
                                           device.Get()->CreateShaderResourceView(
                                                   (ID3D11Resource*)tex.Get(),
                                                   &srvDesc,
                                                   srv.GetAddressOf())
                                               .ThrowOnError();

                                           var desc = default(D3D11_TEXTURE2D_DESC);
                                           tex.Get()->GetDesc(&desc);

                                           tcs.SetResult(
                                               new UnknownTextureWrap(
                                                   (IUnknown*)srv.Get(),
                                                   (int)desc.Width,
                                                   (int)desc.Height,
                                                   true));
                                       }
                                   }
                                   catch (Exception e)
                                   {
                                       tcs.SetException(e);
                                   }
                               });

                           return tcs.Task;
                       },
                       cancellationToken)
                   .ContinueWith(
                       r =>
                       {
                           wrapCopy.Dispose();
                           return r;
                       },
                       default(CancellationToken))
                   .Unwrap();
    }

    private unsafe ID3D11Texture2D* NoThrottleCreateFromExistingTextureCore(
        IDalamudTextureWrap wrap,
        Vector2 uv0,
        Vector2 uv1,
        DXGI_FORMAT format,
        bool enableCpuRead)
    {
        ThreadSafety.AssertMainThread();

        using var resUnk = new ComPtr<IUnknown>((IUnknown*)wrap.ImGuiHandle);

        using var texSrv = default(ComPtr<ID3D11ShaderResourceView>);
        resUnk.As(&texSrv).ThrowOnError();

        using var device = default(ComPtr<ID3D11Device>);
        texSrv.Get()->GetDevice(device.GetAddressOf());

        using var deviceContext = default(ComPtr<ID3D11DeviceContext>);
        device.Get()->GetImmediateContext(deviceContext.GetAddressOf());

        using var tex2D = default(ComPtr<ID3D11Texture2D>);
        using (var texRes = default(ComPtr<ID3D11Resource>))
        {
            texSrv.Get()->GetResource(texRes.GetAddressOf());
            texRes.As(&tex2D).ThrowOnError();
        }

        var texDesc = default(D3D11_TEXTURE2D_DESC);
        tex2D.Get()->GetDesc(&texDesc);

        using var tex2DCopyTemp = default(ComPtr<ID3D11Texture2D>);
        var tex2DCopyTempDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = checked((uint)MathF.Round((uv1.X - uv0.X) * wrap.Width)),
            Height = checked((uint)MathF.Round((uv1.Y - uv0.Y) * wrap.Height)),
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDesc = new(1, 0),
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
            CPUAccessFlags = 0u,
            MiscFlags = 0u,
        };
        device.Get()->CreateTexture2D(&tex2DCopyTempDesc, null, tex2DCopyTemp.GetAddressOf()).ThrowOnError();

        using (var rtvCopyTemp = default(ComPtr<ID3D11RenderTargetView>))
        {
            var rtvCopyTempDesc = new D3D11_RENDER_TARGET_VIEW_DESC(
                tex2DCopyTemp,
                D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
            device.Get()->CreateRenderTargetView(
                (ID3D11Resource*)tex2DCopyTemp.Get(),
                &rtvCopyTempDesc,
                rtvCopyTemp.GetAddressOf()).ThrowOnError();

            this.drawsOneSquare ??= new();
            this.drawsOneSquare.Setup(device.Get());

            deviceContext.Get()->OMSetRenderTargets(1u, rtvCopyTemp.GetAddressOf(), null);
            this.drawsOneSquare.Draw(
                deviceContext.Get(),
                texSrv.Get(),
                (int)tex2DCopyTempDesc.Width,
                (int)tex2DCopyTempDesc.Height,
                uv0,
                uv1);
            deviceContext.Get()->OMSetRenderTargets(0, null, null);
        }

        if (!enableCpuRead)
        {
            tex2DCopyTemp.Get()->AddRef();
            return tex2DCopyTemp.Get();
        }

        using var tex2DTarget = default(ComPtr<ID3D11Texture2D>);
        var tex2DTargetDesc = tex2DCopyTempDesc with
        {
            Usage = D3D11_USAGE.D3D11_USAGE_DYNAMIC,
            BindFlags = 0u,
            CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
        };
        device.Get()->CreateTexture2D(&tex2DTargetDesc, null, tex2DTarget.GetAddressOf()).ThrowOnError();

        deviceContext.Get()->CopyResource((ID3D11Resource*)tex2DTarget.Get(), (ID3D11Resource*)tex2DCopyTemp.Get());

        tex2DTarget.Get()->AddRef();
        return tex2DTarget.Get();
    }

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    private sealed unsafe class DrawsOneSquare : IDisposable
    {
        private ComPtr<ID3D11SamplerState> sampler;
        private ComPtr<ID3D11VertexShader> vertexShader;
        private ComPtr<ID3D11PixelShader> pixelShader;
        private ComPtr<ID3D11InputLayout> inputLayout;
        private ComPtr<ID3D11Buffer> vertexConstantBuffer;
        private ComPtr<ID3D11BlendState> blendState;
        private ComPtr<ID3D11RasterizerState> rasterizerState;
        private ComPtr<ID3D11Buffer> vertexBufferFill;
        private ComPtr<ID3D11Buffer> vertexBufferMutable;
        private ComPtr<ID3D11Buffer> indexBuffer;

        ~DrawsOneSquare() => this.Dispose();

        public void Dispose()
        {
            this.sampler.Reset();
            this.vertexShader.Reset();
            this.pixelShader.Reset();
            this.inputLayout.Reset();
            this.vertexConstantBuffer.Reset();
            this.blendState.Reset();
            this.rasterizerState.Reset();
            this.vertexBufferFill.Reset();
            this.vertexBufferMutable.Reset();
            this.indexBuffer.Reset();
        }

        public void Setup<T>(T* device) where T : unmanaged, ID3D11Device.Interface
        {
            var assembly = typeof(ImGuiScene.ImGui_Impl_DX11).Assembly;

            // Create the vertex shader
            if (this.vertexShader.IsEmpty() || this.inputLayout.IsEmpty())
            {
                this.vertexShader.Reset();
                this.inputLayout.Reset();

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
                    device->CreateVertexShader(pArray, (nuint)stream.Length, null, ppShader).ThrowOnError();

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
                    device->CreateInputLayout(ied, 3, pArray, (nuint)stream.Length, ppInputLayout).ThrowOnError();
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
                    device->CreateBuffer(&bufferDesc, &subr, ppBuffer).ThrowOnError();
            }

            // Create the pixel shader
            if (this.pixelShader.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream("imgui-frag.hlsl.bytes")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);
                fixed (byte* pArray = array)
                fixed (ID3D11PixelShader** ppShader = &this.pixelShader.GetPinnableReference())
                    device->CreatePixelShader(pArray, (nuint)stream.Length, null, ppShader).ThrowOnError();

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
                            SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_INV_DEST_ALPHA,
                            DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_ONE,
                            BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                            RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALL,
                        },
                    },
                };
                fixed (ID3D11BlendState** ppBlendState = &this.blendState.GetPinnableReference())
                    device->CreateBlendState(&blendStateDesc, ppBlendState).ThrowOnError();
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
                    device->CreateRasterizerState(&rasterizerDesc, ppRasterizerState).ThrowOnError();
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
                    device->CreateSamplerState(&samplerDesc, ppSampler).ThrowOnError();
            }

            if (this.vertexBufferFill.IsEmpty())
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
                device->CreateBuffer(&desc, &subr, &buffer).ThrowOnError();
                this.vertexBufferFill.Attach(buffer);
            }

            if (this.vertexBufferMutable.IsEmpty())
            {
                var desc = new D3D11_BUFFER_DESC(
                    (uint)(sizeof(ImDrawVert) * 4),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                    (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
                var buffer = default(ID3D11Buffer*);
                device->CreateBuffer(&desc, null, &buffer).ThrowOnError();
                this.vertexBufferMutable.Attach(buffer);
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
                device->CreateBuffer(&desc, &subr, &buffer).ThrowOnError();
                this.indexBuffer.Attach(buffer);
            }
        }

        public void Draw(
            ID3D11DeviceContext* ctx,
            ID3D11ShaderResourceView* srv,
            int width,
            int height,
            Vector2 uv0,
            Vector2 uv1)
        {
            ID3D11Buffer* buffer;
            if (uv0 == Vector2.Zero && uv1 == Vector2.One)
            {
                buffer = this.vertexBufferFill.Get();
            }
            else
            {
                buffer = this.vertexBufferMutable.Get();
                var mapped = default(D3D11_MAPPED_SUBRESOURCE);
                ctx->Map((ID3D11Resource*)buffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0u, &mapped).ThrowOnError();
                _ = new Span<ImDrawVert>(mapped.pData, 4)
                {
                    [0] = new() { col = uint.MaxValue, pos = new(-1, 1), uv = uv0 },
                    [1] = new() { col = uint.MaxValue, pos = new(-1, -1), uv = new(uv0.X, uv1.Y) },
                    [2] = new() { col = uint.MaxValue, pos = new(1, 1), uv = new(uv1.X, uv0.Y) },
                    [3] = new() { col = uint.MaxValue, pos = new(1, -1), uv = uv1 },
                };
                ctx->Unmap((ID3D11Resource*)buffer, 0u);
            }

            var stride = (uint)sizeof(ImDrawVert);
            var offset = 0u;

            ctx->IASetInputLayout(this.inputLayout);
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
    }
}

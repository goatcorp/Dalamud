using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Storage.Assets;
using Dalamud.Utility;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private SimpleDrawerImpl? simpleDrawer;

    /// <summary>A class for drawing simple stuff.</summary>
    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    internal sealed unsafe class SimpleDrawerImpl : IDisposable
    {
        private ComPtr<ID3D11SamplerState> sampler;
        private ComPtr<ID3D11VertexShader> vertexShader;
        private ComPtr<ID3D11PixelShader> pixelShader;
        private ComPtr<ID3D11InputLayout> inputLayout;
        private ComPtr<ID3D11Buffer> vertexConstantBuffer;
        private ComPtr<ID3D11BlendState> blendState;
        private ComPtr<ID3D11BlendState> blendStateForStrippingAlpha;
        private ComPtr<ID3D11RasterizerState> rasterizerState;
        private ComPtr<ID3D11Buffer> vertexBufferFill;
        private ComPtr<ID3D11Buffer> vertexBufferMutable;
        private ComPtr<ID3D11Buffer> indexBuffer;

        /// <summary>Finalizes an instance of the <see cref="SimpleDrawerImpl"/> class.</summary>
        ~SimpleDrawerImpl() => this.Dispose();

        /// <inheritdoc/>
        public void Dispose()
        {
            this.sampler.Reset();
            this.vertexShader.Reset();
            this.pixelShader.Reset();
            this.inputLayout.Reset();
            this.vertexConstantBuffer.Reset();
            this.blendState.Reset();
            this.blendStateForStrippingAlpha.Reset();
            this.rasterizerState.Reset();
            this.vertexBufferFill.Reset();
            this.vertexBufferMutable.Reset();
            this.indexBuffer.Reset();
            GC.SuppressFinalize(this);
        }

        /// <summary>Sets up this instance of <see cref="SimpleDrawerImpl"/>.</summary>
        /// <param name="device">The device.</param>
        public void Setup(ID3D11Device* device)
        {
            var assembly = typeof(SimpleDrawerImpl).Assembly;

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

            if (this.blendStateForStrippingAlpha.IsEmpty())
            {
                var blendStateDesc = new D3D11_BLEND_DESC
                {
                    RenderTarget =
                    {
                        e0 =
                        {
                            BlendEnable = true,
                            SrcBlend = D3D11_BLEND.D3D11_BLEND_ZERO,
                            DestBlend = D3D11_BLEND.D3D11_BLEND_ONE,
                            BlendOp = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                            SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_ONE,
                            DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_ZERO,
                            BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                            RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALPHA,
                        },
                    },
                };
                fixed (ID3D11BlendState** ppBlendState = &this.blendStateForStrippingAlpha.GetPinnableReference())
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
                    new() { Col = uint.MaxValue, Pos = new(-1, 1), Uv = new(0, 0) },
                    new() { Col = uint.MaxValue, Pos = new(-1, -1), Uv = new(0, 1) },
                    new() { Col = uint.MaxValue, Pos = new(1, 1), Uv = new(1, 0) },
                    new() { Col = uint.MaxValue, Pos = new(1, -1), Uv = new(1, 1) },
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

        /// <summary>Draws the given shader resource view to the current render target.</summary>
        /// <param name="ctx">An instance of <see cref="ID3D11DeviceContext"/>.</param>
        /// <param name="srv">The shader resource view.</param>
        /// <param name="uv0">The left top coordinates relative to the size of the source texture.</param>
        /// <param name="uv1">The right bottom coordinates relative to the size of the source texture.</param>
        /// <remarks>This function does not throw.</remarks>
        public void Draw(
            ID3D11DeviceContext* ctx,
            ID3D11ShaderResourceView* srv,
            Vector2 uv0,
            Vector2 uv1)
        {
            using var rtv = default(ComPtr<ID3D11RenderTargetView>);
            ctx->OMGetRenderTargets(1, rtv.GetAddressOf(), null);
            if (rtv.IsEmpty())
                return;

            using var rtvRes = default(ComPtr<ID3D11Resource>);
            rtv.Get()->GetResource(rtvRes.GetAddressOf());

            using var rtvTex = default(ComPtr<ID3D11Texture2D>);
            if (rtvRes.As(&rtvTex).FAILED)
                return;

            D3D11_TEXTURE2D_DESC texDesc;
            rtvTex.Get()->GetDesc(&texDesc);

            ID3D11Buffer* buffer;
            if (uv0 == Vector2.Zero && uv1 == Vector2.One)
            {
                buffer = this.vertexBufferFill.Get();
            }
            else
            {
                buffer = this.vertexBufferMutable.Get();
                var mapped = default(D3D11_MAPPED_SUBRESOURCE);
                if (ctx->Map((ID3D11Resource*)buffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0u, &mapped).FAILED)
                    return;
                _ = new Span<ImDrawVert>(mapped.pData, 4)
                {
                    [0] = new() { Col = uint.MaxValue, Pos = new(-1, 1), Uv = uv0 },
                    [1] = new() { Col = uint.MaxValue, Pos = new(-1, -1), Uv = new(uv0.X, uv1.Y) },
                    [2] = new() { Col = uint.MaxValue, Pos = new(1, 1), Uv = new(uv1.X, uv0.Y) },
                    [3] = new() { Col = uint.MaxValue, Pos = new(1, -1), Uv = uv1 },
                };
                ctx->Unmap((ID3D11Resource*)buffer, 0u);
            }

            var stride = (uint)sizeof(ImDrawVert);
            var offset = 0u;

            ctx->IASetInputLayout(this.inputLayout);
            ctx->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
            ctx->IASetIndexBuffer(this.indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
            ctx->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

            var viewport = new D3D11_VIEWPORT(0, 0, texDesc.Width, texDesc.Height);
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

            var ppn = default(ID3D11ShaderResourceView*);
            ctx->PSSetShaderResources(0, 1, &ppn);
        }

        /// <summary>Fills alpha channel to 1.0 from the current render target.</summary>
        /// <param name="ctx">An instance of <see cref="ID3D11DeviceContext"/>.</param>
        /// <remarks>This function does not throw.</remarks>
        public void StripAlpha(ID3D11DeviceContext* ctx)
        {
            using var rtv = default(ComPtr<ID3D11RenderTargetView>);
            ctx->OMGetRenderTargets(1, rtv.GetAddressOf(), null);
            if (rtv.IsEmpty())
                return;

            using var rtvRes = default(ComPtr<ID3D11Resource>);
            rtv.Get()->GetResource(rtvRes.GetAddressOf());

            using var rtvTex = default(ComPtr<ID3D11Texture2D>);
            if (rtvRes.As(&rtvTex).FAILED)
                return;

            D3D11_TEXTURE2D_DESC texDesc;
            rtvTex.Get()->GetDesc(&texDesc);

            var buffer = this.vertexBufferFill.Get();
            var stride = (uint)sizeof(ImDrawVert);
            var offset = 0u;

            ctx->IASetInputLayout(this.inputLayout);
            ctx->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
            ctx->IASetIndexBuffer(this.indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
            ctx->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

            var viewport = new D3D11_VIEWPORT(0, 0, texDesc.Width, texDesc.Height);
            ctx->RSSetState(this.rasterizerState);
            ctx->RSSetViewports(1, &viewport);

            var blendColor = default(Vector4);
            ctx->OMSetBlendState(this.blendStateForStrippingAlpha, (float*)&blendColor, 0xffffffff);
            ctx->OMSetDepthStencilState(null, 0);

            ctx->VSSetShader(this.vertexShader.Get(), null, 0);
            buffer = this.vertexConstantBuffer.Get();
            ctx->VSSetConstantBuffers(0, 1, &buffer);

            ctx->PSSetShader(this.pixelShader, null, 0);
            var simp = this.sampler.Get();
            ctx->PSSetSamplers(0, 1, &simp);
            var ppn = (ID3D11ShaderResourceView*)Service<DalamudAssetManager>.Get().White4X4.Handle.Handle;
            ctx->PSSetShaderResources(0, 1, &ppn);

            ctx->GSSetShader(null, null, 0);
            ctx->HSSetShader(null, null, 0);
            ctx->DSSetShader(null, null, 0);
            ctx->CSSetShader(null, null, 0);
            ctx->DrawIndexed(6, 0, 0);

            ppn = default;
            ctx->PSSetShaderResources(0, 1, &ppn);
        }
    }
}

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <inheritdoc cref="IDrawListTextureWrap"/>
internal sealed unsafe partial class DrawListTextureWrap
{
    /// <summary>The renderer.</summary>
    [ServiceManager.EarlyLoadedService]
    internal sealed class Renderer : IInternalDisposableService
    {
        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11DeviceContext> deviceContext;

        private ComPtr<ID3D11VertexShader> drawToPremulVertexShader;
        private ComPtr<ID3D11PixelShader> drawToPremulPixelShader;
        private ComPtr<ID3D11InputLayout> drawToPremulInputLayout;
        private ComPtr<ID3D11Buffer> drawToPremulVertexBuffer;
        private ComPtr<ID3D11Buffer> drawToPremulVertexConstantBuffer;
        private ComPtr<ID3D11Buffer> drawToPremulIndexBuffer;

        private ComPtr<ID3D11VertexShader> makeStraightVertexShader;
        private ComPtr<ID3D11PixelShader> makeStraightPixelShader;
        private ComPtr<ID3D11InputLayout> makeStraightInputLayout;
        private ComPtr<ID3D11Buffer> makeStraightVertexBuffer;
        private ComPtr<ID3D11Buffer> makeStraightIndexBuffer;

        private ComPtr<ID3D11SamplerState> samplerState;
        private ComPtr<ID3D11BlendState> blendState;
        private ComPtr<ID3D11RasterizerState> rasterizerState;
        private ComPtr<ID3D11DepthStencilState> depthStencilState;
        private int vertexBufferSize;
        private int indexBufferSize;

        [ServiceManager.ServiceConstructor]
        private Renderer(InterfaceManager.InterfaceManagerWithScene iwms)
        {
            try
            {
                this.device = new((ID3D11Device*)iwms.Manager.Backend!.DeviceHandle);
                fixed (ID3D11DeviceContext** p = &this.deviceContext.GetPinnableReference())
                    this.device.Get()->GetImmediateContext(p);
                this.deviceContext.Get()->AddRef();

                this.Setup();
            }
            catch
            {
                this.ReleaseUnmanagedResources();
                throw;
            }
        }

        /// <summary>Finalizes an instance of the <see cref="Renderer"/> class.</summary>
        ~Renderer() => this.ReleaseUnmanagedResources();

        /// <inheritdoc/>
        public void DisposeService() => this.ReleaseUnmanagedResources();

        /// <summary>Renders draw data.</summary>
        /// <param name="prtv">The render target.</param>
        /// <param name="drawData">Pointer to the draw data.</param>
        public void RenderDrawData(ID3D11RenderTargetView* prtv, ImDrawDataPtr drawData)
        {
            ThreadSafety.AssertMainThread();

            if (drawData.DisplaySize.X <= 0 || drawData.DisplaySize.Y <= 0
                || !drawData.Valid || drawData.CmdListsCount < 1)
                return;
            var cmdLists = new Span<ImDrawListPtr>(drawData.CmdLists, drawData.CmdListsCount);

            // Create and grow vertex/index buffers if needed
            if (this.vertexBufferSize < drawData.TotalVtxCount)
                this.drawToPremulVertexBuffer.Dispose();
            if (this.drawToPremulVertexBuffer.Get() is null)
            {
                this.vertexBufferSize = drawData.TotalVtxCount + 5000;
                var desc = new D3D11_BUFFER_DESC(
                    (uint)(sizeof(ImDrawVert) * this.vertexBufferSize),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                    (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
                var buffer = default(ID3D11Buffer*);
                this.device.Get()->CreateBuffer(&desc, null, &buffer).ThrowOnError();
                this.drawToPremulVertexBuffer.Attach(buffer);
            }

            if (this.indexBufferSize < drawData.TotalIdxCount)
                this.drawToPremulIndexBuffer.Dispose();
            if (this.drawToPremulIndexBuffer.Get() is null)
            {
                this.indexBufferSize = drawData.TotalIdxCount + 5000;
                var desc = new D3D11_BUFFER_DESC(
                    (uint)(sizeof(ushort) * this.indexBufferSize),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                    (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
                var buffer = default(ID3D11Buffer*);
                this.device.Get()->CreateBuffer(&desc, null, &buffer).ThrowOnError();
                this.drawToPremulIndexBuffer.Attach(buffer);
            }

            // Upload vertex/index data into a single contiguous GPU buffer
            try
            {
                var vertexData = default(D3D11_MAPPED_SUBRESOURCE);
                var indexData = default(D3D11_MAPPED_SUBRESOURCE);
                this.deviceContext.Get()->Map(
                    (ID3D11Resource*)this.drawToPremulVertexBuffer.Get(),
                    0,
                    D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                    0,
                    &vertexData).ThrowOnError();
                this.deviceContext.Get()->Map(
                    (ID3D11Resource*)this.drawToPremulIndexBuffer.Get(),
                    0,
                    D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                    0,
                    &indexData).ThrowOnError();

                var targetVertices = new Span<ImDrawVert>(vertexData.pData, this.vertexBufferSize);
                var targetIndices = new Span<ushort>(indexData.pData, this.indexBufferSize);
                foreach (ref var cmdList in cmdLists)
                {
                    var vertices = new ImVectorWrapper<ImDrawVert>(cmdList.VtxBuffer.ToUntyped());
                    var indices = new ImVectorWrapper<ushort>(cmdList.IdxBuffer.ToUntyped());

                    vertices.DataSpan.CopyTo(targetVertices);
                    indices.DataSpan.CopyTo(targetIndices);

                    targetVertices = targetVertices[vertices.Length..];
                    targetIndices = targetIndices[indices.Length..];
                }
            }
            finally
            {
                this.deviceContext.Get()->Unmap((ID3D11Resource*)this.drawToPremulVertexBuffer.Get(), 0);
                this.deviceContext.Get()->Unmap((ID3D11Resource*)this.drawToPremulIndexBuffer.Get(), 0);
            }

            // Setup orthographic projection matrix into our constant buffer.
            // Our visible imgui space lies from DisplayPos (LT) to DisplayPos+DisplaySize (RB).
            // DisplayPos is (0,0) for single viewport apps.
            try
            {
                var data = default(D3D11_MAPPED_SUBRESOURCE);
                this.deviceContext.Get()->Map(
                    (ID3D11Resource*)this.drawToPremulVertexConstantBuffer.Get(),
                    0,
                    D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                    0,
                    &data).ThrowOnError();
                ref var xform = ref *(TransformationBuffer*)data.pData;
                xform.View =
                    Matrix4x4.CreateOrthographicOffCenter(
                        drawData.DisplayPos.X,
                        drawData.DisplayPos.X + drawData.DisplaySize.X,
                        drawData.DisplayPos.Y + drawData.DisplaySize.Y,
                        drawData.DisplayPos.Y,
                        1f,
                        0f);
            }
            finally
            {
                this.deviceContext.Get()->Unmap((ID3D11Resource*)this.drawToPremulVertexConstantBuffer.Get(), 0);
            }

            // Set up render state
            {
                this.deviceContext.Get()->IASetInputLayout(this.drawToPremulInputLayout);
                var buffer = this.drawToPremulVertexBuffer.Get();
                var stride = (uint)sizeof(ImDrawVert);
                var offset = 0u;
                this.deviceContext.Get()->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
                this.deviceContext.Get()->IASetIndexBuffer(
                    this.drawToPremulIndexBuffer,
                    DXGI_FORMAT.DXGI_FORMAT_R16_UINT,
                    0);
                this.deviceContext.Get()->IASetPrimitiveTopology(
                    D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

                var viewport = new D3D11_VIEWPORT(
                    0,
                    0,
                    drawData.DisplaySize.X * drawData.FramebufferScale.X,
                    drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
                this.deviceContext.Get()->RSSetState(this.rasterizerState);
                this.deviceContext.Get()->RSSetViewports(1, &viewport);

                var blendColor = default(Vector4);
                this.deviceContext.Get()->OMSetBlendState(this.blendState, (float*)&blendColor, 0xffffffff);
                this.deviceContext.Get()->OMSetDepthStencilState(this.depthStencilState, 0);
                this.deviceContext.Get()->OMSetRenderTargets(1, &prtv, null);

                this.deviceContext.Get()->VSSetShader(this.drawToPremulVertexShader.Get(), null, 0);
                buffer = this.drawToPremulVertexConstantBuffer.Get();
                this.deviceContext.Get()->VSSetConstantBuffers(0, 1, &buffer);

                // PS handled later

                this.deviceContext.Get()->GSSetShader(null, null, 0);
                this.deviceContext.Get()->HSSetShader(null, null, 0);
                this.deviceContext.Get()->DSSetShader(null, null, 0);
                this.deviceContext.Get()->CSSetShader(null, null, 0);
            }

            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            var vertexOffset = 0;
            var indexOffset = 0;
            var clipOff = new Vector4(drawData.DisplayPos, drawData.DisplayPos.X, drawData.DisplayPos.Y);
            var frameBufferScaleV4 =
                new Vector4(drawData.FramebufferScale, drawData.FramebufferScale.X, drawData.FramebufferScale.Y);
            foreach (ref var cmdList in cmdLists)
            {
                var cmds = new ImVectorWrapper<ImDrawCmd>(cmdList.CmdBuffer.ToUntyped());
                foreach (ref var cmd in cmds.DataSpan)
                {
                    var clipV4 = (cmd.ClipRect - clipOff) * frameBufferScaleV4;
                    var clipRect = new RECT((int)clipV4.X, (int)clipV4.Y, (int)clipV4.Z, (int)clipV4.W);

                    // Skip the draw if nothing would be visible
                    if (clipRect.left >= clipRect.right || clipRect.top >= clipRect.bottom || cmd.ElemCount == 0)
                        continue;

                    this.deviceContext.Get()->RSSetScissorRects(1, &clipRect);

                    if (cmd.UserCallback == null)
                    {
                        // Bind texture and draw
                        var samplerp = this.samplerState.Get();
                        var srvp = (ID3D11ShaderResourceView*)cmd.TextureId.Handle;
                        this.deviceContext.Get()->PSSetShader(this.drawToPremulPixelShader, null, 0);
                        this.deviceContext.Get()->PSSetSamplers(0, 1, &samplerp);
                        this.deviceContext.Get()->PSSetShaderResources(0, 1, &srvp);
                        this.deviceContext.Get()->DrawIndexed(
                            cmd.ElemCount,
                            (uint)(cmd.IdxOffset + indexOffset),
                            (int)(cmd.VtxOffset + vertexOffset));
                    }
                }

                indexOffset += cmdList.IdxBuffer.Size;
                vertexOffset += cmdList.VtxBuffer.Size;
            }
        }

        /// <summary>Renders draw data.</summary>
        /// <param name="psrv">The pointer to a Texture2D SRV to read premultiplied data from.</param>
        /// <param name="prtv">The pointer to a Texture2D RTV to write straightened data.</param>
        public void MakeStraight(ID3D11ShaderResourceView* psrv, ID3D11RenderTargetView* prtv)
        {
            ThreadSafety.AssertMainThread();

            D3D11_TEXTURE2D_DESC texDesc;
            using (var texRes = default(ComPtr<ID3D11Resource>))
            {
                prtv->GetResource(texRes.GetAddressOf());

                using var tex = default(ComPtr<ID3D11Texture2D>);
                texRes.As(&tex).ThrowOnError();
                tex.Get()->GetDesc(&texDesc);
            }

            this.deviceContext.Get()->IASetInputLayout(this.makeStraightInputLayout);
            var buffer = this.makeStraightVertexBuffer.Get();
            var stride = (uint)sizeof(Vector2);
            var offset = 0u;
            this.deviceContext.Get()->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
            this.deviceContext.Get()->IASetIndexBuffer(
                this.makeStraightIndexBuffer,
                DXGI_FORMAT.DXGI_FORMAT_R16_UINT,
                0);
            this.deviceContext.Get()->IASetPrimitiveTopology(
                D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

            var scissorRect = new RECT(0, 0, (int)texDesc.Width, (int)texDesc.Height);
            this.deviceContext.Get()->RSSetScissorRects(1, &scissorRect);
            this.deviceContext.Get()->RSSetState(this.rasterizerState);
            var viewport = new D3D11_VIEWPORT(0, 0, texDesc.Width, texDesc.Height);
            this.deviceContext.Get()->RSSetViewports(1, &viewport);

            this.deviceContext.Get()->OMSetBlendState(null, null, 0xffffffff);
            this.deviceContext.Get()->OMSetDepthStencilState(this.depthStencilState, 0);
            this.deviceContext.Get()->OMSetRenderTargets(1, &prtv, null);

            this.deviceContext.Get()->VSSetShader(this.makeStraightVertexShader.Get(), null, 0);
            this.deviceContext.Get()->PSSetShader(this.makeStraightPixelShader.Get(), null, 0);
            this.deviceContext.Get()->GSSetShader(null, null, 0);
            this.deviceContext.Get()->HSSetShader(null, null, 0);
            this.deviceContext.Get()->DSSetShader(null, null, 0);
            this.deviceContext.Get()->CSSetShader(null, null, 0);

            this.deviceContext.Get()->PSSetShaderResources(0, 1, &psrv);
            this.deviceContext.Get()->DrawIndexed(6, 0, 0);
        }

        [SuppressMessage(
            "StyleCop.CSharp.LayoutRules",
            "SA1519:Braces should not be omitted from multi-line child statement",
            Justification = "Multiple fixed")]
        private void Setup()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var rendererName = typeof(Renderer).FullName!.Replace('+', '.');

            if (this.drawToPremulVertexShader.IsEmpty() || this.drawToPremulInputLayout.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream($"{rendererName}.DrawToPremul.vs.bin")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);

                using var tempShader = default(ComPtr<ID3D11VertexShader>);
                using var tempInputLayout = default(ComPtr<ID3D11InputLayout>);

                fixed (byte* pArray = array)
                fixed (void* pszPosition = "POSITION"u8)
                fixed (void* pszTexCoord = "TEXCOORD"u8)
                fixed (void* pszColor = "COLOR"u8)
                {
                    this.device.Get()->CreateVertexShader(
                        pArray,
                        (nuint)stream.Length,
                        null,
                        tempShader.GetAddressOf()).ThrowOnError();

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
                    this.device.Get()->CreateInputLayout(
                            ied,
                            3,
                            pArray,
                            (nuint)stream.Length,
                            tempInputLayout.GetAddressOf())
                        .ThrowOnError();
                }

                ArrayPool<byte>.Shared.Return(array);

                tempShader.Swap(ref this.drawToPremulVertexShader);
                tempInputLayout.Swap(ref this.drawToPremulInputLayout);
            }

            if (this.drawToPremulPixelShader.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream($"{rendererName}.DrawToPremul.ps.bin")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);

                using var tmp = default(ComPtr<ID3D11PixelShader>);
                fixed (byte* pArray = array)
                {
                    this.device.Get()->CreatePixelShader(pArray, (nuint)stream.Length, null, tmp.GetAddressOf())
                        .ThrowOnError();
                }

                ArrayPool<byte>.Shared.Return(array);

                tmp.Swap(ref this.drawToPremulPixelShader);
            }

            if (this.makeStraightVertexShader.IsEmpty() || this.makeStraightInputLayout.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream($"{rendererName}.MakeStraight.vs.bin")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);

                using var tempShader = default(ComPtr<ID3D11VertexShader>);
                using var tempInputLayout = default(ComPtr<ID3D11InputLayout>);

                fixed (byte* pArray = array)
                fixed (void* pszPosition = "POSITION"u8)
                {
                    this.device.Get()->CreateVertexShader(
                        pArray,
                        (nuint)stream.Length,
                        null,
                        tempShader.GetAddressOf()).ThrowOnError();

                    var ied = stackalloc D3D11_INPUT_ELEMENT_DESC[]
                    {
                        new()
                        {
                            SemanticName = (sbyte*)pszPosition,
                            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                            AlignedByteOffset = uint.MaxValue,
                        },
                    };
                    this.device.Get()->CreateInputLayout(
                            ied,
                            1,
                            pArray,
                            (nuint)stream.Length,
                            tempInputLayout.GetAddressOf())
                        .ThrowOnError();
                }

                ArrayPool<byte>.Shared.Return(array);

                tempShader.Swap(ref this.makeStraightVertexShader);
                tempInputLayout.Swap(ref this.makeStraightInputLayout);
            }

            if (this.makeStraightPixelShader.IsEmpty())
            {
                using var stream = assembly.GetManifestResourceStream($"{rendererName}.MakeStraight.ps.bin")!;
                var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(array, 0, (int)stream.Length);

                using var tmp = default(ComPtr<ID3D11PixelShader>);
                fixed (byte* pArray = array)
                {
                    this.device.Get()->CreatePixelShader(pArray, (nuint)stream.Length, null, tmp.GetAddressOf())
                        .ThrowOnError();
                }

                ArrayPool<byte>.Shared.Return(array);

                tmp.Swap(ref this.makeStraightPixelShader);
            }

            if (this.makeStraightVertexBuffer.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11Buffer>);
                var desc = new D3D11_BUFFER_DESC(
                    (uint)(sizeof(Vector2) * 4),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
                var data = stackalloc Vector2[] { new(-1, 1), new(-1, -1), new(1, 1), new(1, -1) };
                var subr = new D3D11_SUBRESOURCE_DATA { pSysMem = data };
                this.device.Get()->CreateBuffer(&desc, &subr, tmp.GetAddressOf()).ThrowOnError();
                tmp.Swap(ref this.makeStraightVertexBuffer);
            }

            if (this.makeStraightIndexBuffer.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11Buffer>);
                var desc = new D3D11_BUFFER_DESC(
                    sizeof(ushort) * 6,
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
                var data = stackalloc ushort[] { 0, 1, 2, 1, 2, 3 };
                var subr = new D3D11_SUBRESOURCE_DATA { pSysMem = data };
                this.device.Get()->CreateBuffer(&desc, &subr, tmp.GetAddressOf()).ThrowOnError();
                tmp.Swap(ref this.makeStraightIndexBuffer);
            }

            if (this.drawToPremulVertexConstantBuffer.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11Buffer>);
                var bufferDesc = new D3D11_BUFFER_DESC(
                    (uint)sizeof(TransformationBuffer),
                    (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER,
                    D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                    (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
                this.device.Get()->CreateBuffer(&bufferDesc, null, tmp.GetAddressOf()).ThrowOnError();

                tmp.Swap(ref this.drawToPremulVertexConstantBuffer);
            }

            if (this.samplerState.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11SamplerState>);
                var samplerDesc = new D3D11_SAMPLER_DESC
                {
                    Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
                    AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
                    MipLODBias = 0,
                    MaxAnisotropy = 0,
                    ComparisonFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
                    MinLOD = 0,
                    MaxLOD = 0,
                };
                this.device.Get()->CreateSamplerState(&samplerDesc, tmp.GetAddressOf()).ThrowOnError();

                tmp.Swap(ref this.samplerState);
            }

            // Create the blending setup
            if (this.blendState.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11BlendState>);
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
                this.device.Get()->CreateBlendState(&blendStateDesc, tmp.GetAddressOf()).ThrowOnError();

                tmp.Swap(ref this.blendState);
            }

            // Create the rasterizer state
            if (this.rasterizerState.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11RasterizerState>);
                var rasterizerDesc = new D3D11_RASTERIZER_DESC
                {
                    FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
                    CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
                    ScissorEnable = true,
                    DepthClipEnable = true,
                };
                this.device.Get()->CreateRasterizerState(&rasterizerDesc, tmp.GetAddressOf()).ThrowOnError();

                tmp.Swap(ref this.rasterizerState);
            }

            // Create the depth-stencil State
            if (this.depthStencilState.IsEmpty())
            {
                using var tmp = default(ComPtr<ID3D11DepthStencilState>);
                var dsDesc = new D3D11_DEPTH_STENCIL_DESC
                {
                    DepthEnable = false,
                    StencilEnable = false,
                };
                this.device.Get()->CreateDepthStencilState(&dsDesc, tmp.GetAddressOf()).ThrowOnError();

                tmp.Swap(ref this.depthStencilState);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            this.device.Reset();
            this.deviceContext.Reset();
            this.drawToPremulVertexShader.Reset();
            this.drawToPremulPixelShader.Reset();
            this.drawToPremulInputLayout.Reset();
            this.makeStraightVertexShader.Reset();
            this.makeStraightPixelShader.Reset();
            this.makeStraightInputLayout.Reset();
            this.samplerState.Reset();
            this.drawToPremulVertexConstantBuffer.Reset();
            this.blendState.Reset();
            this.rasterizerState.Reset();
            this.depthStencilState.Reset();
            this.drawToPremulVertexBuffer.Reset();
            this.drawToPremulIndexBuffer.Reset();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TransformationBuffer
        {
            public Matrix4x4 View;
            public Vector4 ColorMultiplier;
        }
    }
}

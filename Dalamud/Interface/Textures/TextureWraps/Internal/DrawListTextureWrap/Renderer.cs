using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.ImGuiBackend.Helpers.D3D11;
using Dalamud.Interface.ImGuiBackend.Renderers;
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
        private readonly D3D11Shaders.ImDrawPipeline pipeline = new();

        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11DeviceContext> deviceContext;

        private ComPtr<ID3D11VertexShader> makeStraightVertexShader;
        private ComPtr<ID3D11PixelShader> makeStraightPixelShader;
        private ComPtr<ID3D11InputLayout> makeStraightInputLayout;
        private ComPtr<ID3D11Buffer> makeStraightVertexBuffer;
        private ComPtr<ID3D11Buffer> makeStraightIndexBuffer;

        [ServiceManager.ServiceConstructor]
        private Renderer(InterfaceManager.InterfaceManagerWithScene iwms)
        {
            try
            {
                this.device = new((ID3D11Device*)iwms.Manager.Backend!.DeviceHandle);
                fixed (ID3D11DeviceContext** p = &this.deviceContext.GetPinnableReference())
                    this.device.Get()->GetImmediateContext(p);

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
        /// <remarks>Callers must invoke this on the main thread with non-empty draw data.</remarks>
        public void RenderDrawData(ID3D11RenderTargetView* prtv, ImDrawDataPtr drawData)
        {
            var cmdLists = drawData.CmdLists.AsSpan();
            var ctx = this.deviceContext.Get();

            // The target starts transparent, so subpixel text falls back to grayscale.
            var monitor = MonitorCache.ForViewport(drawData.OwnerViewport).Text;
            this.pipeline.Upload(this.device, ctx, drawData, monitor, false);

            var glyphs = GlyphAtlas.Shared.Upload(this.deviceContext);
            ctx->OMSetRenderTargets(1, &prtv, null);
            this.pipeline.Bind(ctx, drawData.DisplaySize * drawData.FramebufferScale, glyphs);

            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            var vertexOffset = 0;
            var indexOffset = 0;
            var clipOff = new Vector4(drawData.DisplayPos, drawData.DisplayPos.X, drawData.DisplayPos.Y);
            foreach (ref var cmdList in cmdLists)
            {
                var cmds = new ImVectorWrapper<ImDrawCmd>(cmdList.CmdBuffer.ToUntyped());
                foreach (ref var cmd in cmds.DataSpan)
                {
                    if (cmd.UserCallback != null)
                    {
                        if ((nint)cmd.UserCallback == (nint)CustomImDrawCallbackEnum.BindGlyphTextures)
                        {
                            D3D11Shaders.ImDrawPipeline.BindGlyphTextures(
                                ctx,
                                GlyphAtlas.GetTexturesToBind(cmd.UserCallbackData, glyphs));
                        }

                        continue;
                    }

                    D3D11Shaders.ImDrawPipeline.DrawCommand(
                        ctx, cmd, clipOff, drawData.FramebufferScale, indexOffset, vertexOffset);
                }

                indexOffset += cmdList.IdxBuffer.Size;
                vertexOffset += cmdList.VtxBuffer.Size;
            }
        }

        /// <summary>Converts a texture from premultiplied alpha to straight alpha.</summary>
        /// <param name="psrv">The pointer to a Texture2D SRV to read premultiplied data from.</param>
        /// <param name="prtv">The pointer to a Texture2D RTV to write straightened data.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        public void MakeStraight(ID3D11ShaderResourceView* psrv, ID3D11RenderTargetView* prtv, int width, int height)
        {
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

            var scissorRect = new RECT(0, 0, width, height);
            this.deviceContext.Get()->RSSetScissorRects(1, &scissorRect);
            this.deviceContext.Get()->RSSetState(this.pipeline.RasterizerState);
            var viewport = new D3D11_VIEWPORT(0, 0, width, height);
            this.deviceContext.Get()->RSSetViewports(1, &viewport);

            this.deviceContext.Get()->OMSetBlendState(null, null, 0xffffffff);
            this.deviceContext.Get()->OMSetDepthStencilState(this.pipeline.DepthStencilState, 0);
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
            var rendererName = typeof(Renderer).FullName!.Replace('+', '.');
            var pDevice = this.device.Get();

            this.pipeline.Create(pDevice);

            var bytes = D3D11Shaders.ReadResource($"{rendererName}.MakeStraight.vs.bin");
            fixed (byte* pArray = bytes)
            fixed (void* pszPosition = "POSITION"u8)
            fixed (ID3D11VertexShader** ppShader = &this.makeStraightVertexShader.GetPinnableReference())
            fixed (ID3D11InputLayout** ppInputLayout = &this.makeStraightInputLayout.GetPinnableReference())
            {
                pDevice->CreateVertexShader(pArray, (nuint)bytes.Length, null, ppShader).ThrowOnError();

                var ied = stackalloc D3D11_INPUT_ELEMENT_DESC[]
                {
                    new()
                    {
                        SemanticName = (sbyte*)pszPosition,
                        Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                        AlignedByteOffset = uint.MaxValue,
                    },
                };
                pDevice->CreateInputLayout(ied, 1, pArray, (nuint)bytes.Length, ppInputLayout).ThrowOnError();
            }

            fixed (ID3D11PixelShader** ppShader = &this.makeStraightPixelShader.GetPinnableReference())
                D3D11Shaders.CreatePixelShader(pDevice, $"{rendererName}.MakeStraight.ps.bin", ppShader);

            var vertexBufferDesc = new D3D11_BUFFER_DESC(
                (uint)(sizeof(Vector2) * 4),
                (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
            var vertices = stackalloc Vector2[] { new(-1, 1), new(-1, -1), new(1, 1), new(1, -1) };
            var vertexData = new D3D11_SUBRESOURCE_DATA { pSysMem = vertices };
            fixed (ID3D11Buffer** ppBuffer = &this.makeStraightVertexBuffer.GetPinnableReference())
                pDevice->CreateBuffer(&vertexBufferDesc, &vertexData, ppBuffer).ThrowOnError();

            var indexBufferDesc = new D3D11_BUFFER_DESC(
                sizeof(ushort) * 6,
                (uint)D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                D3D11_USAGE.D3D11_USAGE_IMMUTABLE);
            var indices = stackalloc ushort[] { 0, 1, 2, 1, 2, 3 };
            var indexData = new D3D11_SUBRESOURCE_DATA { pSysMem = indices };
            fixed (ID3D11Buffer** ppBuffer = &this.makeStraightIndexBuffer.GetPinnableReference())
                pDevice->CreateBuffer(&indexBufferDesc, &indexData, ppBuffer).ThrowOnError();
        }

        private void ReleaseUnmanagedResources()
        {
            this.device.Reset();
            this.deviceContext.Reset();
            this.pipeline.Dispose();
            this.makeStraightVertexShader.Reset();
            this.makeStraightPixelShader.Reset();
            this.makeStraightInputLayout.Reset();
            this.makeStraightVertexBuffer.Reset();
            this.makeStraightIndexBuffer.Reset();
        }
    }
}

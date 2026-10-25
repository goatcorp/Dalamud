using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiBackend.Helpers.D3D11;

/// <summary>Creates and fills the shaders, states and buffers shared by the renderers of <see cref="ImDrawVert"/>.
/// </summary>
/// <remarks>All functions throw if the device fails.</remarks>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal static unsafe class D3D11Shaders
{
    /// <summary>Reads a manifest resource of this assembly.</summary>
    /// <param name="name">The name of the resource.</param>
    /// <returns>The bytes.</returns>
    public static byte[] ReadResource(string name)
    {
        using var stream = typeof(D3D11Shaders).Assembly.GetManifestResourceStream(name)!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>Creates a vertex shader from a manifest resource.</summary>
    /// <param name="device">The device.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="shader">The created shader.</param>
    public static void CreateVertexShader(ID3D11Device* device, string name, ID3D11VertexShader** shader)
    {
        var bytes = ReadResource(name);
        fixed (byte* p = bytes)
            device->CreateVertexShader(p, (nuint)bytes.Length, null, shader).ThrowOnError();
    }

    /// <summary>Creates a pixel shader from a manifest resource.</summary>
    /// <param name="device">The device.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="shader">The created shader.</param>
    public static void CreatePixelShader(ID3D11Device* device, string name, ID3D11PixelShader** shader)
    {
        var bytes = ReadResource(name);
        fixed (byte* p = bytes)
            device->CreatePixelShader(p, (nuint)bytes.Length, null, shader).ThrowOnError();
    }

    /// <summary>Creates the vertex shader of imgui-vertex.hlsl, which reads <see cref="ImDrawVert"/>, and its input
    /// layout.</summary>
    /// <param name="device">The device.</param>
    /// <param name="shader">The created shader.</param>
    /// <param name="layout">The created input layout.</param>
    /// <remarks>The elements are <c>POSITION</c>, <c>TEXCOORD0</c>, <c>COLOR</c>, and <see cref="ImDrawVert.Ext"/> as
    /// unsigned integers. <c>ext[0]</c> to <c>ext[3]</c> are in <c>TEXCOORD1</c>, and <c>ext[4]</c> is in
    /// <c>TEXCOORD2</c>.</remarks>
    public static void CreateImDrawVertShader(
        ID3D11Device* device,
        ID3D11VertexShader** shader,
        ID3D11InputLayout** layout)
    {
        var bytes = ReadResource("imgui-vertex.hlsl.bytes");
        fixed (byte* pBytecode = bytes)
        fixed (byte* pszPosition = "POSITION"u8)
        fixed (byte* pszTexCoord = "TEXCOORD"u8)
        fixed (byte* pszColor = "COLOR"u8)
        {
            device->CreateVertexShader(pBytecode, (nuint)bytes.Length, null, shader).ThrowOnError();

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
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    SemanticIndex = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    SemanticIndex = 2,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32_UINT,
                    AlignedByteOffset = uint.MaxValue,
                },
            };
            device->CreateInputLayout(ied, 5, pBytecode, (nuint)bytes.Length, layout)
                  .ThrowOnError();
        }
    }

    /// <summary>Creates a linear sampler with wrap addressing.</summary>
    /// <param name="device">The device.</param>
    /// <param name="state">The created sampler state.</param>
    public static void CreateLinearWrapSampler(ID3D11Device* device, ID3D11SamplerState** state)
    {
        var desc = new D3D11_SAMPLER_DESC
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
        device->CreateSamplerState(&desc, state).ThrowOnError();
    }

    /// <summary>Creates the blend state for the straight-alpha colors of ImGui.</summary>
    /// <param name="device">The device.</param>
    /// <param name="state">The created blend state.</param>
    public static void CreateStraightAlphaBlendState(ID3D11Device* device, ID3D11BlendState** state)
    {
        var desc = new D3D11_BLEND_DESC
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
        device->CreateBlendState(&desc, state).ThrowOnError();
    }

    /// <summary>Creates the blend state of the text pixel shader.</summary>
    /// <param name="device">The device.</param>
    /// <param name="state">The created blend state.</param>
    /// <remarks>It uses dual-source blending.</remarks>
    public static void CreateTextBlendState(ID3D11Device* device, ID3D11BlendState** state)
    {
        var desc = new D3D11_BLEND_DESC
        {
            RenderTarget =
            {
                e0 =
                {
                    BlendEnable = true,
                    SrcBlend = D3D11_BLEND.D3D11_BLEND_ONE,
                    DestBlend = D3D11_BLEND.D3D11_BLEND_INV_SRC1_COLOR,
                    BlendOp = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                    SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_INV_DEST_ALPHA,
                    DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_ONE,
                    BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                    RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALL,
                },
            },
        };
        device->CreateBlendState(&desc, state).ThrowOnError();
    }

    /// <summary>Creates a CPU-writable dynamic buffer.</summary>
    /// <param name="device">The device.</param>
    /// <param name="byteWidth">The size in bytes.</param>
    /// <param name="bindFlag">What the buffer is bound as.</param>
    /// <param name="buffer">The created buffer.</param>
    public static void CreateDynamicBuffer(
        ID3D11Device* device, uint byteWidth, D3D11_BIND_FLAG bindFlag, ID3D11Buffer** buffer)
    {
        var desc = new D3D11_BUFFER_DESC(
            byteWidth,
            (uint)bindFlag,
            D3D11_USAGE.D3D11_USAGE_DYNAMIC,
            (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
        device->CreateBuffer(&desc, null, buffer).ThrowOnError();
    }

    private static void CreateScissorRasterizerState(ID3D11Device* device, ID3D11RasterizerState** state)
    {
        var desc = new D3D11_RASTERIZER_DESC
        {
            FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
            CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
            ScissorEnable = true,
            DepthClipEnable = true,
        };
        device->CreateRasterizerState(&desc, state).ThrowOnError();
    }

    private static void CreateDisabledDepthStencilState(ID3D11Device* device, ID3D11DepthStencilState** state)
    {
        var desc = new D3D11_DEPTH_STENCIL_DESC
        {
            DepthEnable = false,
            DepthWriteMask = D3D11_DEPTH_WRITE_MASK.D3D11_DEPTH_WRITE_MASK_ALL,
            DepthFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
            StencilEnable = false,
            StencilReadMask = byte.MaxValue,
            StencilWriteMask = byte.MaxValue,
            FrontFace =
            {
                StencilFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilDepthFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilPassOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
            },
            BackFace =
            {
                StencilFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilDepthFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilPassOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
            },
        };
        device->CreateDepthStencilState(&desc, state).ThrowOnError();
    }

    private static void EnsureDynamicBuffer(
        ID3D11Device* device,
        ref ComPtr<ID3D11Buffer> buffer,
        ref int capacity,
        int needed,
        int slack,
        int stride,
        D3D11_BIND_FLAG bindFlag)
    {
        if (capacity < needed)
            buffer.Dispose();
        if (buffer.Get() is not null)
            return;

        capacity = needed + slack;
        var created = default(ID3D11Buffer*);
        CreateDynamicBuffer(device, (uint)(stride * capacity), bindFlag, &created);
        buffer.Attach(created);
    }

    private static void WriteDynamic<T>(ID3D11DeviceContext* context, ID3D11Buffer* buffer, in T value)
        where T : unmanaged
    {
        var mapped = default(D3D11_MAPPED_SUBRESOURCE);
        context->Map((ID3D11Resource*)buffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0, &mapped).ThrowOnError();
        *(T*)mapped.pData = value;
        context->Unmap((ID3D11Resource*)buffer, 0);
    }

    private static void UploadDrawLists(
        ID3D11DeviceContext* context,
        ID3D11Buffer* vertexBuffer,
        int vertexCapacity,
        ID3D11Buffer* indexBuffer,
        int indexCapacity,
        Span<ImDrawListPtr> cmdLists)
    {
        var vertexData = default(D3D11_MAPPED_SUBRESOURCE);
        var indexData = default(D3D11_MAPPED_SUBRESOURCE);
        context->Map((ID3D11Resource*)vertexBuffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0, &vertexData)
               .ThrowOnError();
        var hr = context->Map((ID3D11Resource*)indexBuffer, 0, D3D11_MAP.D3D11_MAP_WRITE_DISCARD, 0, &indexData);
        if (hr.FAILED)
        {
            context->Unmap((ID3D11Resource*)vertexBuffer, 0);
            hr.ThrowOnError();
        }

        try
        {
            var targetVertices = new Span<ImDrawVert>(vertexData.pData, vertexCapacity);
            var targetIndices = new Span<ushort>(indexData.pData, indexCapacity);
            foreach (ref var cmdList in cmdLists)
            {
                var vertices = new ImVectorWrapper<ImDrawVert>(cmdList.Handle->VtxBuffer.ToUntyped());
                var indices = new ImVectorWrapper<ushort>(cmdList.Handle->IdxBuffer.ToUntyped());

                vertices.DataSpan.CopyTo(targetVertices);
                indices.DataSpan.CopyTo(targetIndices);

                targetVertices = targetVertices[vertices.Length..];
                targetIndices = targetIndices[indices.Length..];
            }
        }
        finally
        {
            context->Unmap((ID3D11Resource*)vertexBuffer, 0);
            context->Unmap((ID3D11Resource*)indexBuffer, 0);
        }
    }

    private static Matrix4x4 CreateProjectionMatrix(ImDrawDataPtr drawData) =>
        Matrix4x4.CreateOrthographicOffCenter(
            drawData.DisplayPos.X,
            drawData.DisplayPos.X + drawData.DisplaySize.X,
            drawData.DisplayPos.Y + drawData.DisplaySize.Y,
            drawData.DisplayPos.Y,
            1f,
            0f);

    // Constant buffer b0 of imgui-frag.hlsl, padded to 48 bytes.
    [StructLayout(LayoutKind.Sequential)]
    private struct TextConstants
    {
        // Alpha correction coefficients for the display gamma.
        public Vector4 GammaRatios;

        public float EnhancedContrast;

        public float ClearTypeLevel;

        // 1 when subpixel rendering is allowed, or 0 otherwise.
        public float SubpixelAllowed;

        public float GrayscaleEnhancedContrast;

        // 1 for BGR subpixel order, or 0 for RGB.
        public float Bgr;

        public Vector3 Padding;

        public static TextConstants From(in SubpixelSettings.MonitorSettings monitor, bool subpixelAllowed) => new()
        {
            GammaRatios = monitor.GammaRatios,
            EnhancedContrast = monitor.EnhancedContrast,
            ClearTypeLevel = monitor.ClearTypeLevel,
            SubpixelAllowed = subpixelAllowed && monitor.Geometry != SubpixelSettings.SubpixelGeometry.None ? 1 : 0,
            GrayscaleEnhancedContrast = monitor.GrayscaleEnhancedContrast,
            Bgr = monitor.Geometry == SubpixelSettings.SubpixelGeometry.Bgr ? 1 : 0,
        };
    }

    /// <summary>The shaders, states and constant buffers that draw <see cref="ImDrawVert"/> with the text pixel shader.
    /// </summary>
    public sealed class ImDrawPipeline : IDisposable
    {
        // Extra element capacity allocated on growth to avoid reallocating on small frame-to-frame increases.
        private const int BufferSlack = 8192;

        private ComPtr<ID3D11VertexShader> vertexShader;
        private ComPtr<ID3D11InputLayout> inputLayout;
        private ComPtr<ID3D11PixelShader> pixelShader;
        private ComPtr<ID3D11SamplerState> sampler;
        private ComPtr<ID3D11Buffer> vertexConstantBuffer;
        private ComPtr<ID3D11Buffer> textConstantBuffer;
        private ComPtr<ID3D11BlendState> textBlendState;
        private ComPtr<ID3D11RasterizerState> rasterizerState;
        private ComPtr<ID3D11DepthStencilState> depthStencilState;
        private ComPtr<ID3D11Buffer> vertexBuffer;
        private ComPtr<ID3D11Buffer> indexBuffer;
        private int vertexCapacity;
        private int indexCapacity;

        /// <summary>Gets the text pixel shader.</summary>
        public ID3D11PixelShader* PixelShader => this.pixelShader.Get();

        /// <summary>Gets the linear wrap sampler.</summary>
        public ID3D11SamplerState* Sampler => this.sampler.Get();

        /// <summary>Gets the rasterizer state that enables the scissor test.</summary>
        public ID3D11RasterizerState* RasterizerState => this.rasterizerState.Get();

        /// <summary>Gets the depth-stencil state with the tests disabled.</summary>
        public ID3D11DepthStencilState* DepthStencilState => this.depthStencilState.Get();

        /// <summary>Binds the texture of a draw command and draws it, clipped to its rectangle.</summary>
        /// <param name="ctx">The device context.</param>
        /// <param name="cmd">The command.</param>
        /// <param name="clipOff">The display position of the draw data, as both corners of a rectangle.</param>
        /// <param name="framebufferScale">The scale from display to framebuffer pixels.</param>
        /// <param name="indexOffset">The index of the first index of the draw list in the index buffer.</param>
        /// <param name="vertexOffset">The index of the first vertex of the draw list in the vertex buffer.</param>
        /// <remarks>A command without elements or with an empty clip rectangle draws nothing. The texture is read as
        /// <c>ImDrawCmd::GetTexID</c> does, without a P/Invoke per command.</remarks>
        public static void DrawCommand(
            ID3D11DeviceContext* ctx, in ImDrawCmd cmd, Vector4 clipOff, Vector2 framebufferScale, int indexOffset,
            int vertexOffset)
        {
            if (cmd.ElemCount == 0)
                return;
            var clipV4 = (cmd.ClipRect - clipOff) * new Vector4(framebufferScale, framebufferScale.X, framebufferScale.Y);
            var clipRect = new RECT((int)clipV4.X, (int)clipV4.Y, (int)clipV4.Z, (int)clipV4.W);
            if (clipRect.left >= clipRect.right || clipRect.top >= clipRect.bottom)
                return;

            ctx->RSSetScissorRects(1, &clipRect);
            var texId = cmd.TexRef.TexData is not null ? cmd.TexRef.TexData->TexID : cmd.TexRef.TexID;
            var srv = (ID3D11ShaderResourceView*)texId.Handle;
            ctx->PSSetShaderResources(0, 1, &srv);
            ctx->DrawIndexed(cmd.ElemCount, (uint)(cmd.IdxOffset + indexOffset), (int)(cmd.VtxOffset + vertexOffset));
        }

        /// <summary>Binds the glyph arrays of the text pixel shader.</summary>
        /// <param name="ctx">The device context.</param>
        /// <param name="glyphs">The coverage and color glyph arrays.</param>
        public static void BindGlyphTextures(ID3D11DeviceContext* ctx, (nint Coverage, nint Color) glyphs)
        {
            var views = stackalloc ID3D11ShaderResourceView*[2]
            {
                (ID3D11ShaderResourceView*)glyphs.Coverage,
                (ID3D11ShaderResourceView*)glyphs.Color,
            };
            ctx->PSSetShaderResources(1, 2, views);
        }

        /// <summary>Creates any uninitialized D3D11 resources.</summary>
        /// <param name="device">The device.</param>
        public void Create(ID3D11Device* device)
        {
            if (this.vertexShader.IsEmpty() || this.inputLayout.IsEmpty())
            {
                this.vertexShader.Reset();
                this.inputLayout.Reset();
                fixed (ID3D11VertexShader** ppShader = &this.vertexShader.GetPinnableReference())
                fixed (ID3D11InputLayout** ppInputLayout = &this.inputLayout.GetPinnableReference())
                    CreateImDrawVertShader(device, ppShader, ppInputLayout);
            }

            if (this.pixelShader.IsEmpty())
            {
                fixed (ID3D11PixelShader** ppShader = &this.pixelShader.GetPinnableReference())
                    CreatePixelShader(device, "imgui-frag.hlsl.bytes", ppShader);
            }

            if (this.sampler.IsEmpty())
            {
                fixed (ID3D11SamplerState** ppSampler = &this.sampler.GetPinnableReference())
                    CreateLinearWrapSampler(device, ppSampler);
            }

            if (this.vertexConstantBuffer.IsEmpty())
            {
                fixed (ID3D11Buffer** ppBuffer = &this.vertexConstantBuffer.GetPinnableReference())
                {
                    CreateDynamicBuffer(
                        device, (uint)sizeof(Matrix4x4), D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER, ppBuffer);
                }
            }

            if (this.textConstantBuffer.IsEmpty())
            {
                fixed (ID3D11Buffer** ppBuffer = &this.textConstantBuffer.GetPinnableReference())
                {
                    CreateDynamicBuffer(
                        device, (uint)sizeof(TextConstants), D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER, ppBuffer);
                }
            }

            if (this.textBlendState.IsEmpty())
            {
                fixed (ID3D11BlendState** ppBlendState = &this.textBlendState.GetPinnableReference())
                    CreateTextBlendState(device, ppBlendState);
            }

            if (this.rasterizerState.IsEmpty())
            {
                fixed (ID3D11RasterizerState** ppRasterizerState = &this.rasterizerState.GetPinnableReference())
                    CreateScissorRasterizerState(device, ppRasterizerState);
            }

            if (this.depthStencilState.IsEmpty())
            {
                fixed (ID3D11DepthStencilState** ppDepthStencilState = &this.depthStencilState.GetPinnableReference())
                    CreateDisabledDepthStencilState(device, ppDepthStencilState);
            }
        }

        /// <summary>Copies the vertices and indices of draw data, its projection, and the text constants of a monitor to
        /// the buffers of the pipeline.</summary>
        /// <param name="device">The D3D11 device.</param>
        /// <param name="ctx">The device context.</param>
        /// <param name="drawData">The draw data.</param>
        /// <param name="monitor">Monitor gamma, contrast, and subpixel settings.</param>
        /// <param name="subpixelAllowed">Whether the target can show subpixel text.</param>
        public void Upload(
            ID3D11Device* device, ID3D11DeviceContext* ctx, ImDrawDataPtr drawData,
            in SubpixelSettings.MonitorSettings monitor, bool subpixelAllowed)
        {
            EnsureDynamicBuffer(
                device,
                ref this.vertexBuffer,
                ref this.vertexCapacity,
                drawData.TotalVtxCount,
                BufferSlack,
                sizeof(ImDrawVert),
                D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER);
            EnsureDynamicBuffer(
                device,
                ref this.indexBuffer,
                ref this.indexCapacity,
                drawData.TotalIdxCount,
                BufferSlack,
                sizeof(ushort),
                D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER);
            UploadDrawLists(
                ctx,
                this.vertexBuffer,
                this.vertexCapacity,
                this.indexBuffer,
                this.indexCapacity,
                drawData.CmdLists.AsSpan());
            WriteDynamic(ctx, this.vertexConstantBuffer, CreateProjectionMatrix(drawData));
            WriteDynamic(ctx, this.textConstantBuffer, TextConstants.From(monitor, subpixelAllowed));
        }

        /// <summary>Binds the pipeline to draw the draw lists of the last <see cref="Upload"/>.</summary>
        /// <param name="ctx">The device context.</param>
        /// <param name="viewportSize">The size of the viewport in framebuffer pixels.</param>
        /// <param name="glyphs">The coverage and color glyph arrays.</param>
        /// <remarks>The render target is left as it is. The geometry, hull, domain and compute shaders are unbound.
        /// </remarks>
        public void Bind(ID3D11DeviceContext* ctx, Vector2 viewportSize, (nint Coverage, nint Color) glyphs)
        {
            ctx->IASetInputLayout(this.inputLayout);
            var vertexBuffer = this.vertexBuffer.Get();
            var stride = (uint)sizeof(ImDrawVert);
            var offset = 0u;
            ctx->IASetVertexBuffers(0, 1, &vertexBuffer, &stride, &offset);
            ctx->IASetIndexBuffer(this.indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
            ctx->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

            var viewport = new D3D11_VIEWPORT(0, 0, viewportSize.X, viewportSize.Y);
            ctx->RSSetState(this.rasterizerState);
            ctx->RSSetViewports(1, &viewport);

            ctx->OMSetDepthStencilState(this.depthStencilState, 0);

            ctx->VSSetShader(this.vertexShader, null, 0);
            var buffer = this.vertexConstantBuffer.Get();
            ctx->VSSetConstantBuffers(0, 1, &buffer);

            ctx->PSSetShader(this.pixelShader, null, 0);
            var pSampler = this.sampler.Get();
            ctx->PSSetSamplers(0, 1, &pSampler);
            this.BindText(ctx, glyphs, true);

            ctx->GSSetShader(null, null, 0);
            ctx->HSSetShader(null, null, 0);
            ctx->DSSetShader(null, null, 0);
            ctx->CSSetShader(null, null, 0);
        }

        /// <summary>Binds the blend state, and the constants and glyph arrays of the text pixel shader.</summary>
        /// <param name="ctx">The device context.</param>
        /// <param name="glyphs">The coverage and color glyph arrays.</param>
        /// <param name="restoreBlend">Whether to bind the text blend state too.</param>
        public void BindText(ID3D11DeviceContext* ctx, (nint Coverage, nint Color) glyphs, bool restoreBlend)
        {
            if (restoreBlend)
            {
                var blendColor = default(Vector4);
                ctx->OMSetBlendState(this.textBlendState, (float*)&blendColor, 0xffffffff);
            }

            var buffer = this.textConstantBuffer.Get();
            ctx->PSSetConstantBuffers(0, 1, &buffer);
            BindGlyphTextures(ctx, glyphs);
        }

        /// <summary>Releases all D3D11 resources. Call <see cref="Create"/> to reinitialize them.</summary>
        public void Dispose()
        {
            this.vertexShader.Reset();
            this.inputLayout.Reset();
            this.pixelShader.Reset();
            this.sampler.Reset();
            this.vertexConstantBuffer.Reset();
            this.textConstantBuffer.Reset();
            this.textBlendState.Reset();
            this.rasterizerState.Reset();
            this.depthStencilState.Reset();
            this.vertexBuffer.Reset();
            this.indexBuffer.Reset();
            this.vertexCapacity = 0;
            this.indexCapacity = 0;
        }
    }
}

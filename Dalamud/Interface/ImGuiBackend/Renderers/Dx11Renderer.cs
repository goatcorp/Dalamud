using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiBackend.Helpers;
using Dalamud.Interface.ImGuiBackend.Helpers.D3D11;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures.TextureWraps.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>
/// Deals with rendering ImGui using DirectX 11.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx11Renderer : IImGuiRenderer
{
    private readonly List<IDalamudTextureWrap> fontTextures = new();
    private readonly D3D_FEATURE_LEVEL featureLevel;
    private readonly ViewportHandler viewportHandler;
    private readonly nint renderNamePtr;
    private readonly DXGI_FORMAT rtvFormat;
    private readonly ViewportData mainViewport;

    private bool releaseUnmanagedResourceCalled;

    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> context;
    private ComPtr<ID3D11VertexShader> vertexShader;
    private ComPtr<ID3D11PixelShader> pixelShader;
    private ComPtr<ID3D11SamplerState> sampler;
    private ComPtr<ID3D11InputLayout> inputLayout;
    private ComPtr<ID3D11Buffer> vertexConstantBuffer;
    private ComPtr<ID3D11BlendState> blendState;
    private ComPtr<ID3D11RasterizerState> rasterizerState;
    private ComPtr<ID3D11DepthStencilState> depthStencilState;
    private ComPtr<ID3D11Buffer> vertexBuffer;
    private ComPtr<ID3D11Buffer> indexBuffer;
    private int vertexBufferSize;
    private int indexBufferSize;

    private ComPtr<IDCompositionDevice> dcompDevice;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx11Renderer"/> class.
    /// </summary>
    /// <param name="swapChain">The swap chain.</param>
    /// <param name="device">A pointer to an instance of <see cref="ID3D11Device"/>.</param>
    /// <param name="context">A pointer to an instance of <see cref="ID3D11DeviceContext"/>.</param>
    public Dx11Renderer(IDXGISwapChain* swapChain, ID3D11Device* device, ID3D11DeviceContext* context)
    {
        var io = ImGui.GetIO();
        if (ImGui.GetIO().Handle->BackendRendererName is not null)
            throw new InvalidOperationException("ImGui backend renderer seems to be have been already attached.");
        try
        {
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain->GetDesc(&desc).ThrowOnError();
            this.rtvFormat = desc.BufferDesc.Format;
            this.device = new(device);
            this.context = new(context);
            this.featureLevel = device->GetFeatureLevel();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasViewports;

            this.renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx11_c#");
            io.Handle->BackendRendererName = (byte*)this.renderNamePtr;

            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                try
                {
                    fixed (IDCompositionDevice** pp = &this.dcompDevice.GetPinnableReference())
                    fixed (Guid* piidDCompositionDevice = &IID.IID_IDCompositionDevice)
                        DirectX.DCompositionCreateDevice(null, piidDCompositionDevice, (void**)pp).ThrowOnError();

                    ImGuiViewportHelpers.EnableViewportWindowBackgroundAlpha();
                }
                catch
                {
                    // don't care; not using DComposition then
                }

                this.viewportHandler = new(this);
            }

            this.mainViewport = ViewportData.Create(this, swapChain, null, null);
            var vp = ImGui.GetPlatformIO().Viewports[0];
            vp.RendererUserData = this.mainViewport.Handle;
        }
        catch
        {
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Dx11Renderer"/> class.
    /// </summary>
    ~Dx11Renderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void OnNewFrame()
    {
        this.EnsureDeviceObjects();
    }

    /// <inheritdoc/>
    public void OnPreResize() => this.mainViewport.ResetBuffers();

    /// <inheritdoc/>
    public void OnPostResize(int width, int height) => this.mainViewport.ResizeBuffers(width, height, false);

    /// <inheritdoc/>
    public void RenderDrawData(ImDrawDataPtr drawData) =>
        this.mainViewport.Draw(drawData, this.mainViewport.SwapChain == null);

    /// <summary>
    /// Rebuilds font texture.
    /// </summary>
    public void RebuildFontTexture()
    {
        foreach (var fontResourceView in this.fontTextures)
            fontResourceView.Dispose();
        this.fontTextures.Clear();

        this.CreateFontsTexture();
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2D(
        ReadOnlySpan<byte> data,
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        bool allowRenderTarget,
        [CallerMemberName] string debugName = "")
    {
        if (cpuRead && cpuWrite)
            throw new ArgumentException("cpuRead and cpuWrite cannot be set at the same time.");

        var cpuaf = default(D3D11_CPU_ACCESS_FLAG);
        if (cpuRead)
            cpuaf |= D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
        if (cpuWrite)
            cpuaf |= D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE;

        D3D11_USAGE usage;
        if (cpuRead)
            usage = D3D11_USAGE.D3D11_USAGE_STAGING;
        else if (cpuWrite)
            usage = D3D11_USAGE.D3D11_USAGE_DYNAMIC;
        else
            usage = D3D11_USAGE.D3D11_USAGE_DEFAULT;

        var texDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)specs.Width,
            Height = (uint)specs.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = specs.Format,
            SampleDesc = new(1, 0),
            Usage = usage,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE |
                               (allowRenderTarget ? D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET : 0)),
            CPUAccessFlags = (uint)cpuaf,
            MiscFlags = 0,
        };
        using var texture = default(ComPtr<ID3D11Texture2D>);
        if (data.IsEmpty)
        {
            Marshal.ThrowExceptionForHR(this.device.Get()->CreateTexture2D(&texDesc, null, texture.GetAddressOf()));
        }
        else
        {
            fixed (void* dataPtr = data)
            {
                var subrdata = new D3D11_SUBRESOURCE_DATA { pSysMem = dataPtr, SysMemPitch = (uint)specs.Pitch };
                Marshal.ThrowExceptionForHR(
                    this.device.Get()->CreateTexture2D(&texDesc, &subrdata, texture.GetAddressOf()));
            }
        }

        texture.Get()->SetDebugName($"Texture:{debugName}:SRV");

        using var srvTemp = default(ComPtr<ID3D11ShaderResourceView>);
        var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
            texture,
            D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
        this.device.Get()->CreateShaderResourceView((ID3D11Resource*)texture.Get(), &srvDesc, srvTemp.GetAddressOf())
            .ThrowOnError();
        srvTemp.Get()->SetDebugName($"Texture:{debugName}:SRV");

        return new UnknownTextureWrap((IUnknown*)srvTemp.Get(), specs.Width, specs.Height, true);
    }

    private void RenderDrawDataInternal(
        ID3D11RenderTargetView* renderTargetView,
        ImDrawDataPtr drawData,
        bool clearRenderTarget)
    {
        // Do nothing when there's nothing to draw
        if (drawData.IsNull || !drawData.Valid)
            return;

        // Avoid rendering when minimized
        if (drawData.DisplaySize.X <= 0 || drawData.DisplaySize.Y <= 0)
            return;

        // Set up render target
        this.context.Get()->OMSetRenderTargets(1, &renderTargetView, null);
        if (clearRenderTarget)
        {
            var color = default(Vector4);
            this.context.Get()->ClearRenderTargetView(renderTargetView, (float*)&color);
        }

        // Stop if there's nothing to draw
        var cmdLists = new Span<ImDrawListPtr>(drawData.Handle->CmdLists, drawData.Handle->CmdListsCount);
        if (cmdLists.IsEmpty)
            return;

        // Create and grow vertex/index buffers if needed
        if (this.vertexBufferSize < drawData.TotalVtxCount)
            this.vertexBuffer.Dispose();
        if (this.vertexBuffer.Get() is null)
        {
            this.vertexBufferSize = drawData.TotalVtxCount + 8192;
            var desc = new D3D11_BUFFER_DESC(
                (uint)(sizeof(ImDrawVert) * this.vertexBufferSize),
                (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER,
                D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
            var buffer = default(ID3D11Buffer*);
            this.device.Get()->CreateBuffer(&desc, null, &buffer).ThrowOnError();
            this.vertexBuffer.Attach(buffer);
        }

        if (this.indexBufferSize < drawData.TotalIdxCount)
            this.indexBuffer.Dispose();
        if (this.indexBuffer.Get() is null)
        {
            this.indexBufferSize = drawData.TotalIdxCount + 16384;
            var desc = new D3D11_BUFFER_DESC(
                (uint)(sizeof(ushort) * this.indexBufferSize),
                (uint)D3D11_BIND_FLAG.D3D11_BIND_INDEX_BUFFER,
                D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
            var buffer = default(ID3D11Buffer*);
            this.device.Get()->CreateBuffer(&desc, null, &buffer).ThrowOnError();
            this.indexBuffer.Attach(buffer);
        }

        using var oldState = new D3D11DeviceContextStateBackup(this.featureLevel, this.context.Get());

        // Setup desired DX state
        this.SetupRenderState(drawData);

        try
        {
            // Upload vertex/index data into a single contiguous GPU buffer.
            var vertexData = default(D3D11_MAPPED_SUBRESOURCE);
            var indexData = default(D3D11_MAPPED_SUBRESOURCE);
            this.context.Get()->Map(
                (ID3D11Resource*)this.vertexBuffer.Get(),
                0,
                D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                0,
                &vertexData).ThrowOnError();
            this.context.Get()->Map(
                (ID3D11Resource*)this.indexBuffer.Get(),
                0,
                D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                0,
                &indexData).ThrowOnError();

            var targetVertices = new Span<ImDrawVert>(vertexData.pData, this.vertexBufferSize);
            var targetIndices = new Span<ushort>(indexData.pData, this.indexBufferSize);
            foreach (ref var cmdList in cmdLists)
            {
                var vertices = new ImVectorWrapper<ImDrawVert>(cmdList.Handle->VtxBuffer.ToUntyped());
                var indices = new ImVectorWrapper<ushort>(cmdList.Handle->IdxBuffer.ToUntyped());

                vertices.DataSpan.CopyTo(targetVertices);
                indices.DataSpan.CopyTo(targetIndices);

                targetVertices = targetVertices[vertices.Length..];
                targetIndices = targetIndices[indices.Length..];
            }

            // Setup orthographic projection matrix into our constant buffer.
            // Our visible imgui space lies from DisplayPos (LT) to DisplayPos+DisplaySize (RB).
            // DisplayPos is (0,0) for single viewport apps.
            var constantBufferData = default(D3D11_MAPPED_SUBRESOURCE);
            this.context.Get()->Map(
                (ID3D11Resource*)this.vertexConstantBuffer.Get(),
                0,
                D3D11_MAP.D3D11_MAP_WRITE_DISCARD,
                0,
                &constantBufferData).ThrowOnError();
            *(Matrix4x4*)constantBufferData.pData = Matrix4x4.CreateOrthographicOffCenter(
                drawData.DisplayPos.X,
                drawData.DisplayPos.X + drawData.DisplaySize.X,
                drawData.DisplayPos.Y + drawData.DisplaySize.Y,
                drawData.DisplayPos.Y,
                1f,
                0f);
        }
        finally
        {
            this.context.Get()->Unmap((ID3D11Resource*)this.vertexBuffer.Get(), 0);
            this.context.Get()->Unmap((ID3D11Resource*)this.indexBuffer.Get(), 0);
            this.context.Get()->Unmap((ID3D11Resource*)this.vertexConstantBuffer.Get(), 0);
        }

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        var vertexOffset = 0;
        var indexOffset = 0;
        var clipOff = new Vector4(drawData.DisplayPos, drawData.DisplayPos.X, drawData.DisplayPos.Y);
        foreach (ref var cmdList in cmdLists)
        {
            var cmds = new ImVectorWrapper<ImDrawCmd>(cmdList.Handle->CmdBuffer.ToUntyped());
            foreach (ref var cmd in cmds.DataSpan)
            {
                switch ((ImDrawCallbackEnum)(nint)cmd.UserCallback)
                {
                    case ImDrawCallbackEnum.Empty:
                    {
                        var clipV4 = cmd.ClipRect - clipOff;
                        var clipRect = new RECT((int)clipV4.X, (int)clipV4.Y, (int)clipV4.Z, (int)clipV4.W);

                        // Skip the draw if nothing would be visible
                        if (clipRect.left >= clipRect.right || clipRect.top >= clipRect.bottom)
                            continue;

                        this.context.Get()->RSSetScissorRects(1, &clipRect);

                        // Bind texture and draw
                        var srv = (ID3D11ShaderResourceView*)cmd.TextureId.Handle;
                        this.context.Get()->PSSetShaderResources(0, 1, &srv);
                        this.context.Get()->DrawIndexed(
                            cmd.ElemCount,
                            (uint)(cmd.IdxOffset + indexOffset),
                            (int)(cmd.VtxOffset + vertexOffset));
                        break;
                    }

                    case ImDrawCallbackEnum.ResetRenderState:
                    {
                        // Special callback value used by the user to request the renderer to reset render state.
                        this.SetupRenderState(drawData);
                        break;
                    }

                    default:
                    {
                        // User callback, registered via ImDrawList::AddCallback()
                        var cb = (delegate*<ImDrawListPtr, ref ImDrawCmd, void>)cmd.UserCallback;
                        cb(cmdList, ref cmd);
                        break;
                    }
                }
            }

            indexOffset += cmdList.IdxBuffer.Size;
            vertexOffset += cmdList.VtxBuffer.Size;
        }
    }

    /// <summary>
    /// Builds fonts as necessary, and uploads the built data onto the GPU.<br />
    /// No-op if it has already been done.
    /// </summary>
    private void CreateFontsTexture()
    {
        if (this.device.IsEmpty())
            throw new ObjectDisposedException(nameof(Dx11Renderer));

        if (this.fontTextures.Any())
            return;

        var io = ImGui.GetIO();
        if (io.Fonts.Textures.Size == 0)
            io.Fonts.Build();

        for (int textureIndex = 0, textureCount = io.Fonts.Textures.Size;
             textureIndex < textureCount;
             textureIndex++)
        {
            int width = 0, height = 0, bytespp = 0;
            byte* fontPixels = null;

            // Build texture atlas
            io.Fonts.GetTexDataAsRGBA32(
                textureIndex,
                &fontPixels,
                ref width,
                ref height,
                ref bytespp);

            var tex = this.CreateTexture2D(
                new(fontPixels, width * height * bytespp),
                new(width, height, (int)DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, width * bytespp),
                false,
                false,
                false,
                $"Font#{textureIndex}");
            io.Fonts.SetTexID(textureIndex, tex.Handle);
            this.fontTextures.Add(tex);
        }

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Initializes the device context's render state to what we would use for rendering ImGui by default.
    /// </summary>
    /// <param name="drawData">The relevant ImGui draw data.</param>
    private void SetupRenderState(ImDrawDataPtr drawData)
    {
        var ctx = this.context.Get();
        ctx->IASetInputLayout(this.inputLayout);
        var buffer = this.vertexBuffer.Get();
        var stride = (uint)sizeof(ImDrawVert);
        var offset = 0u;
        ctx->IASetVertexBuffers(0, 1, &buffer, &stride, &offset);
        ctx->IASetIndexBuffer(this.indexBuffer, DXGI_FORMAT.DXGI_FORMAT_R16_UINT, 0);
        ctx->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        var viewport = new D3D11_VIEWPORT(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y);
        ctx->RSSetState(this.rasterizerState);
        ctx->RSSetViewports(1, &viewport);

        var blendColor = default(Vector4);
        ctx->OMSetBlendState(this.blendState, (float*)&blendColor, 0xffffffff);
        ctx->OMSetDepthStencilState(this.depthStencilState, 0);

        ctx->VSSetShader(this.vertexShader.Get(), null, 0);
        buffer = this.vertexConstantBuffer.Get();
        ctx->VSSetConstantBuffers(0, 1, &buffer);

        ctx->PSSetShader(this.pixelShader, null, 0);
        ctx->PSSetSamplers(0, 1, this.sampler.GetAddressOf());

        ctx->GSSetShader(null, null, 0);
        ctx->HSSetShader(null, null, 0);
        ctx->DSSetShader(null, null, 0);
        ctx->CSSetShader(null, null, 0);
    }

    /// <summary>
    /// Creates objects from the device as necessary.<br />
    /// No-op if objects already are built.
    /// </summary>
    private void EnsureDeviceObjects()
    {
        if (this.device.IsEmpty())
            throw new ObjectDisposedException(nameof(Dx11Renderer));

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
                this.device.Get()->CreateVertexShader(pArray, (nuint)stream.Length, null, ppShader).ThrowOnError();

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
                this.device.Get()->CreateInputLayout(ied, 3, pArray, (nuint)stream.Length, ppInputLayout)
                    .ThrowOnError();
            }

            ArrayPool<byte>.Shared.Return(array);
        }

        // Create the pixel shader
        if (this.pixelShader.IsEmpty())
        {
            using var stream = assembly.GetManifestResourceStream("imgui-frag.hlsl.bytes")!;
            var array = ArrayPool<byte>.Shared.Rent((int)stream.Length);
            stream.ReadExactly(array, 0, (int)stream.Length);
            fixed (byte* pArray = array)
            fixed (ID3D11PixelShader** ppShader = &this.pixelShader.GetPinnableReference())
                this.device.Get()->CreatePixelShader(pArray, (nuint)stream.Length, null, ppShader).ThrowOnError();
            ArrayPool<byte>.Shared.Return(array);
        }

        // Create the sampler state
        if (this.sampler.IsEmpty())
        {
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

            fixed (ID3D11SamplerState** ppSampler = &this.sampler.GetPinnableReference())
                this.device.Get()->CreateSamplerState(&samplerDesc, ppSampler).ThrowOnError();
        }

        // Create the constant buffer
        if (this.vertexConstantBuffer.IsEmpty())
        {
            var bufferDesc = new D3D11_BUFFER_DESC(
                (uint)sizeof(Matrix4x4),
                (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER,
                D3D11_USAGE.D3D11_USAGE_DYNAMIC,
                (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE);
            fixed (ID3D11Buffer** ppBuffer = &this.vertexConstantBuffer.GetPinnableReference())
                this.device.Get()->CreateBuffer(&bufferDesc, null, ppBuffer).ThrowOnError();
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
                this.device.Get()->CreateBlendState(&blendStateDesc, ppBlendState).ThrowOnError();
        }

        // Create the rasterizer state
        if (this.rasterizerState.IsEmpty())
        {
            var rasterizerDesc = new D3D11_RASTERIZER_DESC
            {
                FillMode = D3D11_FILL_MODE.D3D11_FILL_SOLID,
                CullMode = D3D11_CULL_MODE.D3D11_CULL_NONE,
                ScissorEnable = true,
                DepthClipEnable = true,
            };
            fixed (ID3D11RasterizerState** ppRasterizerState = &this.rasterizerState.GetPinnableReference())
                this.device.Get()->CreateRasterizerState(&rasterizerDesc, ppRasterizerState).ThrowOnError();
        }

        // Create the depth-stencil State
        if (this.depthStencilState.IsEmpty())
        {
            var dsDesc = new D3D11_DEPTH_STENCIL_DESC
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
            fixed (ID3D11DepthStencilState** ppDepthStencilState = &this.depthStencilState.GetPinnableReference())
                this.device.Get()->CreateDepthStencilState(&dsDesc, ppDepthStencilState).ThrowOnError();
        }

        this.CreateFontsTexture();
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.releaseUnmanagedResourceCalled)
            return;
        this.releaseUnmanagedResourceCalled = true;

        this.mainViewport.Dispose();
        var vp = ImGui.GetPlatformIO().Viewports[0];
        vp.RendererUserData = null;
        ImGui.DestroyPlatformWindows();

        this.viewportHandler.Dispose();

        var io = ImGui.GetIO();
        if (io.Handle->BackendRendererName == (void*)this.renderNamePtr)
            io.Handle->BackendRendererName = null;
        if (this.renderNamePtr != 0)
            Marshal.FreeHGlobal(this.renderNamePtr);

        foreach (var fontResourceView in this.fontTextures)
            fontResourceView.Dispose();

        foreach (var i in Enumerable.Range(0, io.Fonts.Textures.Size))
            io.Fonts.SetTexID(i, ImTextureID.Null);

        this.device.Reset();
        this.context.Reset();
        this.vertexShader.Reset();
        this.pixelShader.Reset();
        this.sampler.Reset();
        this.inputLayout.Reset();
        this.vertexConstantBuffer.Reset();
        this.blendState.Reset();
        this.rasterizerState.Reset();
        this.depthStencilState.Reset();
        this.vertexBuffer.Reset();
        this.indexBuffer.Reset();
        this.dcompDevice.Reset();
    }
}

using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ImGuiScene
{
    /// <summary>
    /// Currently undocumented because it is a horrible mess.
    /// A near-direct port of https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
    /// State backup follows the general layout of imgui's sample (which is a mess), but has been rather
    /// expanded to cover the vast majority of render state, following the example here
    /// https://github.com/GPUOpen-LibrariesAndSDKs/CrossfireAPI11/blob/master/amd_lib/src/AMD_SaveRestoreState.cpp
    /// Would be nice to organize it better, but it seems to work
    /// </summary>
    public unsafe class ImGui_Impl_DX11 : IImGuiRenderer
    {
        private IntPtr _renderNamePtr;
        private Device _device;
        private DeviceContext _deviceContext;
        private List<ShaderResourceView> _fontResourceViews = new();
        private SamplerState _fontSampler;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private Buffer _vertexConstantBuffer;
        private BlendState _blendState;
        private RasterizerState _rasterizerState;
        private DepthStencilState _depthStencilState;
        private Buffer _vertexBuffer;
        private Buffer _indexBuffer;
        private int _vertexBufferSize;
        private int _indexBufferSize;
        private VertexBufferBinding _vertexBinding;
        // so we don't make a temporary object every frame
        private RawColor4 _blendColor = new RawColor4(0, 0, 0, 0);

        // TODO: I'll clean this up better later
        private class StateBackup : IDisposable
        {
            private DeviceContext deviceContext;

            // IA
            public InputLayout InputLayout;
            public PrimitiveTopology PrimitiveTopology;
            public Buffer IndexBuffer;
            public Format IndexBufferFormat;
            public int IndexBufferOffset;
            public Buffer[] VertexBuffers;
            public int[] VertexBufferStrides;
            public int[] VertexBufferOffsets;

            // RS
            public RasterizerState RS;
            public Rectangle[] ScissorRects;
            public RawViewportF[] Viewports;

            // OM
            public BlendState BlendState;
            public RawColor4 BlendFactor;
            public int SampleMask;
            public DepthStencilState DepthStencilState;
            public int DepthStencilRef;
            public DepthStencilView DepthStencilView;
            public RenderTargetView[] RenderTargetViews;

            // VS
            public VertexShader VS;
            public Buffer[] VSConstantBuffers;
            public SamplerState[] VSSamplers;
            public ShaderResourceView[] VSResourceViews;

            // HS
            public HullShader HS;
            public Buffer[] HSConstantBuffers;
            public SamplerState[] HSSamplers;
            public ShaderResourceView[] HSResourceViews;

            // DS
            public DomainShader DS;
            public Buffer[] DSConstantBuffers;
            public SamplerState[] DSSamplers;
            public ShaderResourceView[] DSResourceViews;

            // GS
            public GeometryShader GS;
            public Buffer[] GSConstantBuffers;
            public SamplerState[] GSSamplers;
            public ShaderResourceView[] GSResourceViews;

            // PS
            public PixelShader PS;
            public Buffer[] PSConstantBuffers;
            public SamplerState[] PSSamplers;
            public ShaderResourceView[] PSResourceViews;

            public ComputeShader CS;
            public Buffer[] CSConstantBuffers;
            public SamplerState[] CSSamplers;
            public ShaderResourceView[] CSResourceViews;
            public UnorderedAccessView[] CSUAVs;

            private bool disposedValue = false; // To detect redundant calls

            public StateBackup(DeviceContext deviceContext)
            {
                this.deviceContext = deviceContext;

                this.ScissorRects = new Rectangle[16];    // I couldn't find D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE as a SharpDX enum
                this.Viewports = new RawViewportF[16];
                this.VertexBuffers = new Buffer[InputAssemblerStage.VertexInputResourceSlotCount];
                this.VertexBufferStrides = new int[InputAssemblerStage.VertexInputResourceSlotCount];
                this.VertexBufferOffsets = new int[InputAssemblerStage.VertexInputResourceSlotCount];

                // IA
                this.InputLayout = deviceContext.InputAssembler.InputLayout;
                this.deviceContext.InputAssembler.GetIndexBuffer(out this.IndexBuffer, out this.IndexBufferFormat, out this.IndexBufferOffset);
                this.PrimitiveTopology = this.deviceContext.InputAssembler.PrimitiveTopology;
                this.deviceContext.InputAssembler.GetVertexBuffers(0, InputAssemblerStage.VertexInputResourceSlotCount, this.VertexBuffers, this.VertexBufferStrides, this.VertexBufferOffsets);

                // RS
                this.RS = this.deviceContext.Rasterizer.State;
                this.deviceContext.Rasterizer.GetScissorRectangles<Rectangle>(this.ScissorRects);
                this.deviceContext.Rasterizer.GetViewports<RawViewportF>(this.Viewports);

                // OM
                this.BlendState = this.deviceContext.OutputMerger.GetBlendState(out this.BlendFactor, out this.SampleMask);
                this.DepthStencilState = this.deviceContext.OutputMerger.GetDepthStencilState(out this.DepthStencilRef);
                this.RenderTargetViews = this.deviceContext.OutputMerger.GetRenderTargets(OutputMergerStage.SimultaneousRenderTargetCount, out this.DepthStencilView);

                // VS
                this.VS = this.deviceContext.VertexShader.Get();
                this.VSSamplers = this.deviceContext.VertexShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.VSConstantBuffers = this.deviceContext.VertexShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.VSResourceViews = this.deviceContext.VertexShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

                // HS
                this.HS = this.deviceContext.HullShader.Get();
                this.HSSamplers = this.deviceContext.HullShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.HSConstantBuffers = this.deviceContext.HullShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.HSResourceViews = this.deviceContext.HullShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

                // DS
                this.DS = this.deviceContext.DomainShader.Get();
                this.DSSamplers = this.deviceContext.DomainShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.DSConstantBuffers = this.deviceContext.DomainShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.DSResourceViews = this.deviceContext.DomainShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

                // GS
                this.GS = this.deviceContext.GeometryShader.Get();
                this.GSSamplers = this.deviceContext.GeometryShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.GSConstantBuffers = this.deviceContext.GeometryShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.GSResourceViews = this.deviceContext.GeometryShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

                // PS
                this.PS = this.deviceContext.PixelShader.Get();
                this.PSSamplers = this.deviceContext.PixelShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.PSConstantBuffers = this.deviceContext.PixelShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.PSResourceViews = this.deviceContext.PixelShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

                // CS
                this.CS = this.deviceContext.ComputeShader.Get();
                this.CSSamplers = this.deviceContext.ComputeShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
                this.CSConstantBuffers = this.deviceContext.ComputeShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
                this.CSResourceViews = this.deviceContext.ComputeShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);
                this.CSUAVs = this.deviceContext.ComputeShader.GetUnorderedAccessViews(0, ComputeShaderStage.UnorderedAccessViewSlotCount);   // should be register count and not slot, but the value is correct
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    // IA
                    this.deviceContext.InputAssembler.InputLayout = this.InputLayout;
                    this.deviceContext.InputAssembler.SetIndexBuffer(this.IndexBuffer, this.IndexBufferFormat, this.IndexBufferOffset);
                    this.deviceContext.InputAssembler.PrimitiveTopology = this.PrimitiveTopology;
                    this.deviceContext.InputAssembler.SetVertexBuffers(0, this.VertexBuffers, this.VertexBufferStrides, this.VertexBufferOffsets);

                    // RS
                    this.deviceContext.Rasterizer.State = this.RS;
                    this.deviceContext.Rasterizer.SetScissorRectangles(this.ScissorRects);
                    this.deviceContext.Rasterizer.SetViewports(this.Viewports, this.Viewports.Length);

                    // OM
                    this.deviceContext.OutputMerger.SetBlendState(this.BlendState, this.BlendFactor, this.SampleMask);
                    this.deviceContext.OutputMerger.SetDepthStencilState(this.DepthStencilState, this.DepthStencilRef);
                    this.deviceContext.OutputMerger.SetRenderTargets(this.DepthStencilView, this.RenderTargetViews);

                    // VS
                    this.deviceContext.VertexShader.Set(this.VS);
                    this.deviceContext.VertexShader.SetSamplers(0, this.VSSamplers);
                    this.deviceContext.VertexShader.SetConstantBuffers(0, this.VSConstantBuffers);
                    this.deviceContext.VertexShader.SetShaderResources(0, this.VSResourceViews);

                    // HS
                    this.deviceContext.HullShader.Set(this.HS);
                    this.deviceContext.HullShader.SetSamplers(0, this.HSSamplers);
                    this.deviceContext.HullShader.SetConstantBuffers(0, this.HSConstantBuffers);
                    this.deviceContext.HullShader.SetShaderResources(0, this.HSResourceViews);

                    // DS
                    this.deviceContext.DomainShader.Set(this.DS);
                    this.deviceContext.DomainShader.SetSamplers(0, this.DSSamplers);
                    this.deviceContext.DomainShader.SetConstantBuffers(0, this.DSConstantBuffers);
                    this.deviceContext.DomainShader.SetShaderResources(0, this.DSResourceViews);

                    // GS
                    this.deviceContext.GeometryShader.Set(this.GS);
                    this.deviceContext.GeometryShader.SetSamplers(0, this.GSSamplers);
                    this.deviceContext.GeometryShader.SetConstantBuffers(0, this.GSConstantBuffers);
                    this.deviceContext.GeometryShader.SetShaderResources(0, this.GSResourceViews);

                    // PS
                    this.deviceContext.PixelShader.Set(this.PS);
                    this.deviceContext.PixelShader.SetSamplers(0, this.PSSamplers);
                    this.deviceContext.PixelShader.SetConstantBuffers(0, this.PSConstantBuffers);
                    this.deviceContext.PixelShader.SetShaderResources(0, this.PSResourceViews);

                    // CS
                    this.deviceContext.ComputeShader.Set(this.CS);
                    this.deviceContext.ComputeShader.SetSamplers(0, this.CSSamplers);
                    this.deviceContext.ComputeShader.SetConstantBuffers(0, this.CSConstantBuffers);
                    this.deviceContext.ComputeShader.SetShaderResources(0, this.CSResourceViews);
                    this.deviceContext.ComputeShader.SetUnorderedAccessViews(0, this.CSUAVs);

                    // force free these references immediately, or they hang around too long and calls
                    // to swapchain->ResizeBuffers() will fail due to outstanding references
                    // We could force free other things too, but nothing else should cause errors
                    // and these should get gc'd and disposed eventually
                    foreach (var rtv in this.RenderTargetViews)
                    {
                        rtv?.Dispose();
                    }

                    this.RenderTargetViews = null;
                    this.DepthStencilView = null;
                    this.VSResourceViews = null;
                    this.HSResourceViews = null;
                    this.DSResourceViews = null;
                    this.GSResourceViews = null;
                    this.PSResourceViews = null;
                    this.CSResourceViews = null;

                    disposedValue = true;
                }
            }

            ~StateBackup()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(false);
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public void SetupRenderState(ImDrawDataPtr drawData)
        {
            // Setup viewport
            _deviceContext.Rasterizer.SetViewport(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y);

            // Setup shader and vertex buffers
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _vertexBinding.Buffer = _vertexBuffer;
            _deviceContext.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            _deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.VertexShader.Set(_vertexShader);
            _deviceContext.VertexShader.SetConstantBuffer(0, _vertexConstantBuffer);
            _deviceContext.PixelShader.Set(_pixelShader);
            _deviceContext.PixelShader.SetSampler(0, _fontSampler);
            _deviceContext.GeometryShader.Set(null);
            _deviceContext.HullShader.Set(null);
            _deviceContext.DomainShader.Set(null);
            _deviceContext.ComputeShader.Set(null);

            // Setup blend state
            _deviceContext.OutputMerger.BlendState = _blendState;
            _deviceContext.OutputMerger.BlendFactor = _blendColor;
            _deviceContext.OutputMerger.DepthStencilState = _depthStencilState;
            _deviceContext.Rasterizer.State = _rasterizerState;
        }

        public void RenderDrawData(ImDrawDataPtr drawData)
        {
            // Avoid rendering when minimized
            if (drawData.DisplaySize.X <= 0 || drawData.DisplaySize.Y <= 0)
            {
                return;
            }

            if (!drawData.Valid || drawData.CmdListsCount == 0)
            {
                return;
            }

            // drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            // Create and grow vertex/index buffers if needed
            if (_vertexBuffer == null || _vertexBufferSize < drawData.TotalVtxCount)
            {
                _vertexBuffer?.Dispose();
                _vertexBufferSize = drawData.TotalVtxCount + 5000;

                _vertexBuffer = new Buffer(_device, new BufferDescription
                {
                    Usage = ResourceUsage.Dynamic,
                    SizeInBytes = Unsafe.SizeOf<ImDrawVert>() * _vertexBufferSize,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None
                });

                // (Re)make this here rather than every frame
                _vertexBinding = new VertexBufferBinding
                {
                    Buffer = _vertexBuffer,
                    Stride = Unsafe.SizeOf<ImDrawVert>(),
                    Offset = 0
                };
            }

            if (_indexBuffer == null || _indexBufferSize < drawData.TotalIdxCount)
            {
                _indexBuffer?.Dispose();
                _indexBufferSize = drawData.TotalIdxCount + 10000;

                _indexBuffer = new Buffer(_device, new BufferDescription
                {
                    Usage = ResourceUsage.Dynamic,
                    SizeInBytes = sizeof(ushort) * _indexBufferSize,    // ImGui.NET doesn't provide an ImDrawIdx, but their sample uses ushort
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
            }

            // Upload vertex/index data into a single contiguous GPU buffer
            int vertexOffset = 0, indexOffset = 0;
            var vertexData = _deviceContext.MapSubresource(_vertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None).DataPointer;
            var indexData = _deviceContext.MapSubresource(_indexBuffer, 0, MapMode.WriteDiscard, MapFlags.None).DataPointer;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdListsRange[n];
                unsafe
                {
                    System.Buffer.MemoryCopy(cmdList.VtxBuffer.Data.ToPointer(),
                                                (ImDrawVert*)vertexData + vertexOffset,
                                                Unsafe.SizeOf<ImDrawVert>() * _vertexBufferSize,
                                                Unsafe.SizeOf<ImDrawVert>() * cmdList.VtxBuffer.Size);

                    System.Buffer.MemoryCopy(cmdList.IdxBuffer.Data.ToPointer(),
                                                (ushort*)indexData + indexOffset,
                                                sizeof(ushort) * _indexBufferSize,
                                                sizeof(ushort) * cmdList.IdxBuffer.Size);

                    vertexOffset += cmdList.VtxBuffer.Size;
                    indexOffset += cmdList.IdxBuffer.Size;
                }
            }
            _deviceContext.UnmapSubresource(_vertexBuffer, 0);
            _deviceContext.UnmapSubresource(_indexBuffer, 0);

            // Setup orthographic projection matrix into our constant buffer
            // Our visible imgui space lies from drawData.DisplayPos (top left) to drawData.DisplayPos+drawData.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
            var L = drawData.DisplayPos.X;
            var R = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var T = drawData.DisplayPos.Y;
            var B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
            var mvp = new float[]
            {
                2f/(R-L),     0,              0,      0,
                0,            2f/(T-B),       0,      0,
                0,            0,              0.5f,   0,
                (R+L)/(L-R),  (T+B)/(B-T),    0.5f,   1f
            };

            var constantBuffer = _deviceContext.MapSubresource(_vertexConstantBuffer, 0, MapMode.WriteDiscard, MapFlags.None).DataPointer;
            unsafe
            {
                fixed (void* mvpPtr = mvp)
                {
                    System.Buffer.MemoryCopy(mvpPtr, constantBuffer.ToPointer(), 16 * sizeof(float), 16 * sizeof(float));
                }
            }
            _deviceContext.UnmapSubresource(_vertexConstantBuffer, 0);

            var oldState = new StateBackup(_deviceContext);

            // Setup desired DX state
            SetupRenderState(drawData);

            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            vertexOffset = 0;
            indexOffset = 0;
            var clipOff = drawData.DisplayPos;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdListsRange[n];
                for (int cmd = 0; cmd < cmdList.CmdBuffer.Size; cmd++)
                {
                    var pcmd = cmdList.CmdBuffer[cmd];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        // TODO
                        throw new NotImplementedException();
                    }
                    else
                    {
                        // Apply scissor/clipping rectangle
                        _deviceContext.Rasterizer.SetScissorRectangle((int)(pcmd.ClipRect.X - clipOff.X), (int)(pcmd.ClipRect.Y - clipOff.Y), (int)(pcmd.ClipRect.Z - clipOff.X), (int)(pcmd.ClipRect.W - clipOff.Y));

                        // Bind texture, Draw
                        // TODO: might be nice to store samplers for loaded textures so that we can look them up and apply them here
                        // rather than just always using the font sampler
                        var textureSrv = ShaderResourceView.FromPointer<ShaderResourceView>(pcmd.TextureId);
                        _deviceContext.PixelShader.SetShaderResource(0, textureSrv);
                        _deviceContext.DrawIndexed((int)pcmd.ElemCount, (int)(pcmd.IdxOffset + indexOffset), (int)(pcmd.VtxOffset + vertexOffset));
                    }
                }

                indexOffset += cmdList.IdxBuffer.Size;
                vertexOffset += cmdList.VtxBuffer.Size;
            }

            oldState.Dispose(); // restores the previous state
            oldState = null;
        }

        public void CreateFontsTexture()
        {
            var io = ImGui.GetIO();
            if (io.Fonts.Textures.Size == 0)
                io.Fonts.Build();
            
            for (int textureIndex = 0, textureCount = io.Fonts.Textures.Size;
                 textureIndex < textureCount;
                 textureIndex++) {

                // Build texture atlas
                io.Fonts.GetTexDataAsRGBA32(textureIndex, out IntPtr fontPixels, out int fontWidth, out int fontHeight,
                                            out int fontBytesPerPixel);

                // Upload texture to graphics system
                var texDesc = new Texture2DDescription {
                    Width = fontWidth,
                    Height = fontHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

                using var fontTexture = new Texture2D(
                    _device, texDesc, new DataRectangle(fontPixels, fontWidth * fontBytesPerPixel));

                // Create texture view
                var fontResourceView = new ShaderResourceView(_device, fontTexture, new ShaderResourceViewDescription {
                    Format = texDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = texDesc.MipLevels }
                });

                // Store our identifier
                _fontResourceViews.Add(fontResourceView);
                io.Fonts.SetTexID(textureIndex, fontResourceView.NativePointer);
            }

            io.Fonts.ClearTexData();

            // Create texture sampler
            _fontSampler = new SamplerState(_device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLodBias = 0,
                ComparisonFunction = Comparison.Always,
                MinimumLod = 0,
                MaximumLod = 0
            });
        }

        public bool CreateDeviceObjects()
        {
            if (_device == null)
            {
                return false;
            }

            if (_fontSampler != null)
            {
                InvalidateDeviceObjects();
            }

            // Create the vertex shader
            byte[] shaderData;

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("imgui-vertex.hlsl.bytes"))
            {
                shaderData = new byte[stream.Length];
                stream.Read(shaderData, 0, shaderData.Length);
            }

            _vertexShader = new VertexShader(_device, shaderData);

            // Create the input layout
            _inputLayout = new InputLayout(_device, shaderData, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32_Float, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0),
                new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 0)
            });

            // Create the constant buffer
            _vertexConstantBuffer = new Buffer(_device, new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 16 * sizeof(float)
            });

            // Create the pixel shader
            using (var stream = assembly.GetManifestResourceStream("imgui-frag.hlsl.bytes"))
            {
                shaderData = new byte[stream.Length];
                stream.Read(shaderData, 0, shaderData.Length);
            }

            _pixelShader = new PixelShader(_device, shaderData);

            // Create the blending setup
            // ...of course this was setup in a way that can't be done inline
            var blendStateDesc = new BlendStateDescription();
            blendStateDesc.AlphaToCoverageEnable = false;
            blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
            blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseSourceAlpha;
            blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _blendState = new BlendState(_device, blendStateDesc);

            // Create the rasterizer state
            _rasterizerState = new RasterizerState(_device, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsScissorEnabled = true,
                IsDepthClipEnabled = true
            });

            // Create the depth-stencil State
            _depthStencilState = new DepthStencilState(_device, new DepthStencilStateDescription
            {
                IsDepthEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Always,
                IsStencilEnabled = false,
                FrontFace =
                {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                },
                BackFace =
                {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                }
            });

            CreateFontsTexture();

            return true;
        }

        // Added to support dynamic rebuilding of the font texture
        // for adding fonts after initialization time
        public void RebuildFontTexture()
        {
            _fontSampler?.Dispose();
            foreach (var fontResourceView in this._fontResourceViews)
                fontResourceView.Dispose();
            this._fontResourceViews.Clear();

            CreateFontsTexture();
        }

        public void InvalidateDeviceObjects()
        {
            if (_device == null)
            {
                return;
            }

            _fontSampler?.Dispose();
            _fontSampler = null;

            foreach (var fontResourceView in this._fontResourceViews)
                fontResourceView.Dispose();
            this._fontResourceViews.Clear();
            for (int textureIndex = 0, textureCount = ImGui.GetIO().Fonts.Textures.Size;
                 textureIndex < textureCount;
                 textureIndex++)
                ImGui.GetIO().Fonts.SetTexID(textureIndex, IntPtr.Zero);

            _indexBuffer?.Dispose();
            _indexBuffer = null;

            _vertexBuffer?.Dispose();
            _vertexBuffer = null;

            _blendState?.Dispose();
            _blendState = null;

            _depthStencilState?.Dispose();
            _depthStencilState = null;

            _rasterizerState?.Dispose();
            _rasterizerState = null;

            _pixelShader?.Dispose();
            _pixelShader = null;

            _vertexConstantBuffer?.Dispose();
            _vertexConstantBuffer = null;

            _inputLayout?.Dispose();
            _inputLayout = null;

            _vertexShader?.Dispose();
            _vertexShader = null;
        }

        public void Init(params object[] initParams)
        {
            ImGui.GetIO().BackendFlags = ImGui.GetIO().BackendFlags | ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasViewports;

            // BackendRendererName is readonly (and null) in ImGui.NET for some reason, but we can hack it via its internal pointer
            _renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx11_c#");
            unsafe
            {
                ImGui.GetIO().NativePtr->BackendRendererName = (byte*)_renderNamePtr.ToPointer();
            }

            _device = (Device)initParams[0];
            _deviceContext = (DeviceContext)initParams[1];

            InitPlatformInterface();

            // SharpDX also doesn't allow reference managment
        }

        public void Shutdown()
        {
            ShutdownPlatformInterface();
            InvalidateDeviceObjects();

            // we don't own these, so no Dispose()
            _device = null;
            _deviceContext = null;

            if (_renderNamePtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_renderNamePtr);
                _renderNamePtr = IntPtr.Zero;
            }
        }

        public void NewFrame()
        {
            if (_fontSampler == null)
            {
                CreateDeviceObjects();
            }
        }

        /** Viewport support **/
        private struct ImGuiViewportDataDx11
        {
            public IntPtr SwapChain;
            public IntPtr View;
        }

        // Viewport interface
        private delegate void CreateWindowDelegate(ImGuiViewportPtr ptr);
        private delegate void DestroyWindowDelegate(ImGuiViewportPtr ptr);
        private delegate void SetWindowSizeDelegate(ImGuiViewportPtr ptr, Vector2 size);
        private delegate void RenderWindowDelegate(ImGuiViewportPtr ptr, IntPtr v);
        private delegate void SwapBuffersDelegate(ImGuiViewportPtr ptr, IntPtr v);

        private CreateWindowDelegate _createWindow;
        private DestroyWindowDelegate _destroyWindow;
        private SetWindowSizeDelegate _setWindowSize;
        private RenderWindowDelegate _renderWindow;
        private SwapBuffersDelegate _swapBuffers;

        private void InitPlatformInterface()
        {
            ImGuiPlatformIOPtr ptr = ImGui.GetPlatformIO();
            _createWindow = CreateWindow;
            _destroyWindow = DestroyWindow;
            _setWindowSize = SetWindowSize;
            _renderWindow = RenderWindow;
            _swapBuffers = SwapBuffers;

            ptr.Renderer_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
            ptr.Renderer_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
            ptr.Renderer_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
            ptr.Renderer_RenderWindow = Marshal.GetFunctionPointerForDelegate(_renderWindow);
            ptr.Renderer_SwapBuffers = Marshal.GetFunctionPointerForDelegate(_swapBuffers);
        }

        private void ShutdownPlatformInterface()
        {
            ImGui.DestroyPlatformWindows();
        }

        // Viewport functions
        public void CreateWindow(ImGuiViewportPtr viewport)
        {
            var data = (ImGuiViewportDataDx11*) Marshal.AllocHGlobal(Marshal.SizeOf<ImGuiViewportDataDx11>());

            // PlatformHandleRaw should always be a HWND, whereas PlatformHandle might be a higher-level handle (e.g. GLFWWindow*, SDL_Window*).
            // Some backend will leave PlatformHandleRaw NULL, in which case we assume PlatformHandle will contain the HWND.
            IntPtr hWnd = viewport.PlatformHandleRaw;
            if (hWnd == IntPtr.Zero)
                hWnd = viewport.PlatformHandle;

            // Create swapchain
            SwapChainDescription desc = new SwapChainDescription
            {
                ModeDescription = new ModeDescription
                {
                    Width = 0,
                    Height = 0,
                    Format = Format.R8G8B8A8_UNorm,
                    RefreshRate = new Rational(0, 0)
                },
                SampleDescription = new SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = hWnd,
                IsWindowed = true,
                SwapEffect = SwapEffect.Discard,
                Flags = SwapChainFlags.None
            };

            data->SwapChain = CreateSwapChain(desc);

            // Create the render target view
            using (var backbuffer = new SwapChain(data->SwapChain).GetBackBuffer<Texture2D>(0))
                data->View = new RenderTargetView(_device, backbuffer).NativePointer;
                
            viewport.RendererUserData = (IntPtr) data;
        }

        private IntPtr CreateSwapChain(SwapChainDescription desc) {

            // Create a swapchain using the existing game hardware (I think)
            using (var dxgi = _device.QueryInterface<SharpDX.DXGI.Device>())
            using (var adapter = dxgi.Adapter)
            using (var factory = adapter.GetParent<Factory>())
            {
                return new SwapChain(factory, _device, desc).NativePointer;
            }
        }

        public void DestroyWindow(ImGuiViewportPtr viewport)
        {
            // This is also called on the main viewport for some reason, and we never set that viewport's RendererUserData
            if (viewport.RendererUserData == IntPtr.Zero) return;

            var data = (ImGuiViewportDataDx11*) viewport.RendererUserData;

            new SwapChain(data->SwapChain).Dispose();
            new RenderTargetView(data->View).Dispose();
            data->SwapChain = IntPtr.Zero;
            data->View = IntPtr.Zero;

            Marshal.FreeHGlobal(viewport.RendererUserData);
            viewport.RendererUserData = IntPtr.Zero;
        }

        public void SetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
        {
            var data = (ImGuiViewportDataDx11*)viewport.RendererUserData;

            // Delete our existing view
            new RenderTargetView(data->View).Dispose();
            var tmpSwap = new SwapChain(data->SwapChain);

            // Resize buffers and recreate view
            tmpSwap.ResizeBuffers(1, (int)size.X, (int)size.Y, Format.Unknown, SwapChainFlags.None);
            using (var backbuffer = tmpSwap.GetBackBuffer<Texture2D>(0))
                data->View = new RenderTargetView(_device, backbuffer).NativePointer;
        }

        public void RenderWindow(ImGuiViewportPtr viewport, IntPtr v)
        {
            var data = (ImGuiViewportDataDx11*)viewport.RendererUserData;

            var tmpRtv = new RenderTargetView(data->View);
            this._deviceContext.OutputMerger.SetTargets(tmpRtv);
            if ((viewport.Flags & ImGuiViewportFlags.NoRendererClear) != ImGuiViewportFlags.NoRendererClear)
                this._deviceContext.ClearRenderTargetView(tmpRtv, new RawColor4(0f, 0f, 0f, 1f));
            RenderDrawData(viewport.DrawData);
        }

        public void SwapBuffers(ImGuiViewportPtr viewport, IntPtr v)
        {
            var data = (ImGuiViewportDataDx11*)viewport.RendererUserData;
            new SwapChain(data->SwapChain).Present(0, PresentFlags.None);
        }
    }
}

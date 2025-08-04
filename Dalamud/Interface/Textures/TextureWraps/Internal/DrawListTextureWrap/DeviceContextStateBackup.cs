using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <inheritdoc cref="IDrawListTextureWrap"/>
internal sealed unsafe partial class DrawListTextureWrap
{
    /// <summary>Captures states of a <see cref="ID3D11DeviceContext"/>.</summary>
    // TODO: Use the one in https://github.com/goatcorp/Dalamud/pull/1923 once the PR goes in
    internal struct DeviceContextStateBackup : IDisposable
    {
        private InputAssemblerState inputAssemblerState;
        private RasterizerState rasterizerState;
        private OutputMergerState outputMergerState;
        private VertexShaderState vertexShaderState;
        private HullShaderState hullShaderState;
        private DomainShaderState domainShaderState;
        private GeometryShaderState geometryShaderState;
        private PixelShaderState pixelShaderState;
        private ComputeShaderState computeShaderState;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceContextStateBackup"/> struct,
        /// by capturing all states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        /// <param name="featureLevel">The feature level.</param>
        /// <param name="ctx">The device context.</param>
        public DeviceContextStateBackup(D3D_FEATURE_LEVEL featureLevel, ID3D11DeviceContext* ctx)
        {
            this.inputAssemblerState = InputAssemblerState.From(ctx);
            this.rasterizerState = RasterizerState.From(ctx);
            this.outputMergerState = OutputMergerState.From(featureLevel, ctx);
            this.vertexShaderState = VertexShaderState.From(ctx);
            this.hullShaderState = HullShaderState.From(ctx);
            this.domainShaderState = DomainShaderState.From(ctx);
            this.geometryShaderState = GeometryShaderState.From(ctx);
            this.pixelShaderState = PixelShaderState.From(ctx);
            this.computeShaderState = ComputeShaderState.From(featureLevel, ctx);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.inputAssemblerState.Dispose();
            this.rasterizerState.Dispose();
            this.outputMergerState.Dispose();
            this.vertexShaderState.Dispose();
            this.hullShaderState.Dispose();
            this.domainShaderState.Dispose();
            this.geometryShaderState.Dispose();
            this.pixelShaderState.Dispose();
            this.computeShaderState.Dispose();
        }

        /// <summary>
        /// Captures Input Assembler states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct InputAssemblerState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT;

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11InputLayout> layout;
            private ComPtr<ID3D11Buffer> indexBuffer;
            private DXGI_FORMAT indexFormat;
            private uint indexOffset;
            private D3D_PRIMITIVE_TOPOLOGY topology;
            private fixed ulong buffers[BufferCount];
            private fixed uint strides[BufferCount];
            private fixed uint offsets[BufferCount];

            /// <summary>
            /// Creates a new instance of <see cref="InputAssemblerState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static InputAssemblerState From(ID3D11DeviceContext* ctx)
            {
                var state = default(InputAssemblerState);
                state.context.Attach(ctx);
                ctx->AddRef();
                ctx->IAGetInputLayout(state.layout.GetAddressOf());
                ctx->IAGetPrimitiveTopology(&state.topology);
                ctx->IAGetIndexBuffer(state.indexBuffer.GetAddressOf(), &state.indexFormat, &state.indexOffset);
                ctx->IAGetVertexBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers, state.strides, state.offsets);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (InputAssemblerState* pThis = &this)
                {
                    ctx->IASetInputLayout(pThis->layout);
                    ctx->IASetPrimitiveTopology(pThis->topology);
                    ctx->IASetIndexBuffer(pThis->indexBuffer, pThis->indexFormat, pThis->indexOffset);
                    ctx->IASetVertexBuffers(
                        0,
                        BufferCount,
                        (ID3D11Buffer**)pThis->buffers,
                        pThis->strides,
                        pThis->offsets);

                    pThis->context.Dispose();
                    pThis->layout.Dispose();
                    pThis->indexBuffer.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Rasterizer states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RasterizerState : IDisposable
        {
            private const int Count = D3D11.D3D11_VIEWPORT_AND_SCISSORRECT_MAX_INDEX;

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11RasterizerState> state;
            private fixed byte viewports[24 * Count];
            private fixed ulong scissorRects[16 * Count];

            /// <summary>
            /// Creates a new instance of <see cref="RasterizerState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static RasterizerState From(ID3D11DeviceContext* ctx)
            {
                var state = default(RasterizerState);
                state.context.Attach(ctx);
                ctx->AddRef();
                ctx->RSGetState(state.state.GetAddressOf());
                uint n = Count;
                ctx->RSGetViewports(&n, (D3D11_VIEWPORT*)state.viewports);
                n = Count;
                ctx->RSGetScissorRects(&n, (RECT*)state.scissorRects);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (RasterizerState* pThis = &this)
                {
                    ctx->RSSetState(pThis->state);
                    ctx->RSSetViewports(Count, (D3D11_VIEWPORT*)pThis->viewports);
                    ctx->RSSetScissorRects(Count, (RECT*)pThis->scissorRects);

                    pThis->context.Dispose();
                    pThis->state.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Output Merger states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct OutputMergerState : IDisposable
        {
            private const int RtvCount = D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT;
            private const int UavCountMax = D3D11.D3D11_1_UAV_SLOT_COUNT;

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11BlendState> blendState;
            private fixed float blendFactor[4];
            private uint sampleMask;
            private uint stencilRef;
            private ComPtr<ID3D11DepthStencilState> depthStencilState;
            private fixed ulong rtvs[RtvCount]; // ID3D11RenderTargetView*[RtvCount]
            private ComPtr<ID3D11DepthStencilView> dsv;
            private fixed ulong uavs[UavCountMax]; // ID3D11UnorderedAccessView*[UavCount]
            private int uavCount;

            /// <summary>
            /// Creates a new instance of <see cref="OutputMergerState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="featureLevel">The feature level.</param>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static OutputMergerState From(D3D_FEATURE_LEVEL featureLevel, ID3D11DeviceContext* ctx)
            {
                var state = default(OutputMergerState);
                state.uavCount = featureLevel >= D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1
                                     ? D3D11.D3D11_1_UAV_SLOT_COUNT
                                     : D3D11.D3D11_PS_CS_UAV_REGISTER_COUNT;
                state.context.Attach(ctx);
                ctx->AddRef();
                ctx->OMGetBlendState(state.blendState.GetAddressOf(), state.blendFactor, &state.sampleMask);
                ctx->OMGetDepthStencilState(state.depthStencilState.GetAddressOf(), &state.stencilRef);
                ctx->OMGetRenderTargetsAndUnorderedAccessViews(
                    RtvCount,
                    (ID3D11RenderTargetView**)state.rtvs,
                    state.dsv.GetAddressOf(),
                    0,
                    (uint)state.uavCount,
                    (ID3D11UnorderedAccessView**)state.uavs);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (OutputMergerState* pThis = &this)
                {
                    ctx->OMSetBlendState(pThis->blendState, pThis->blendFactor, pThis->sampleMask);
                    ctx->OMSetDepthStencilState(pThis->depthStencilState, pThis->stencilRef);
                    var rtvc = (uint)RtvCount;
                    while (rtvc > 0 && pThis->rtvs[rtvc - 1] == 0)
                        rtvc--;

                    var uavlb = rtvc;
                    while (uavlb < this.uavCount && pThis->uavs[uavlb] == 0)
                        uavlb++;

                    var uavc = (uint)this.uavCount;
                    while (uavc > uavlb && pThis->uavs[uavc - 1] == 0)
                        uavlb--;
                    uavc -= uavlb;

                    ctx->OMSetRenderTargetsAndUnorderedAccessViews(
                        rtvc,
                        (ID3D11RenderTargetView**)pThis->rtvs,
                        pThis->dsv,
                        uavc == 0 ? 0 : uavlb,
                        uavc,
                        uavc == 0 ? null : (ID3D11UnorderedAccessView**)pThis->uavs,
                        null);

                    this.context.Reset();
                    this.blendState.Reset();
                    this.depthStencilState.Reset();
                    this.dsv.Reset();
                    foreach (ref var b in new Span<ComPtr<ID3D11RenderTargetView>>(pThis->rtvs, RtvCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11UnorderedAccessView>>(pThis->uavs, this.uavCount))
                        b.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Vertex Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int ClassInstanceCount = 256; // According to msdn

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11VertexShader> shader;
            private fixed ulong insts[ClassInstanceCount];
            private fixed ulong buffers[BufferCount];
            private fixed ulong samplers[SamplerCount];
            private fixed ulong resources[ResourceCount];
            private uint instCount;

            /// <summary>
            /// Creates a new instance of <see cref="VertexShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static VertexShaderState From(ID3D11DeviceContext* ctx)
            {
                var state = default(VertexShaderState);
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = ClassInstanceCount;
                ctx->VSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->VSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->VSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->VSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (VertexShaderState* pThis = &this)
                {
                    ctx->VSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->VSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->VSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->VSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Hull Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct HullShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int ClassInstanceCount = 256; // According to msdn

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11HullShader> shader;
            private fixed ulong insts[ClassInstanceCount];
            private fixed ulong buffers[BufferCount];
            private fixed ulong samplers[SamplerCount];
            private fixed ulong resources[ResourceCount];
            private uint instCount;

            /// <summary>
            /// Creates a new instance of <see cref="HullShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static HullShaderState From(ID3D11DeviceContext* ctx)
            {
                var state = default(HullShaderState);
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = ClassInstanceCount;
                ctx->HSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->HSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->HSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->HSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (HullShaderState* pThis = &this)
                {
                    ctx->HSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->HSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->HSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->HSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Domain Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DomainShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int ClassInstanceCount = 256; // According to msdn

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11DomainShader> shader;
            private fixed ulong insts[ClassInstanceCount];
            private fixed ulong buffers[BufferCount];
            private fixed ulong samplers[SamplerCount];
            private fixed ulong resources[ResourceCount];
            private uint instCount;

            /// <summary>
            /// Creates a new instance of <see cref="DomainShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static DomainShaderState From(ID3D11DeviceContext* ctx)
            {
                var state = default(DomainShaderState);
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = ClassInstanceCount;
                ctx->DSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->DSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->DSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->DSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (DomainShaderState* pThis = &this)
                {
                    ctx->DSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->DSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->DSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->DSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Geometry Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct GeometryShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int ClassInstanceCount = 256; // According to msdn

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11GeometryShader> shader;
            private fixed ulong insts[ClassInstanceCount];
            private fixed ulong buffers[BufferCount];
            private fixed ulong samplers[SamplerCount];
            private fixed ulong resources[ResourceCount];
            private uint instCount;

            /// <summary>
            /// Creates a new instance of <see cref="GeometryShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static GeometryShaderState From(ID3D11DeviceContext* ctx)
            {
                var state = default(GeometryShaderState);
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = ClassInstanceCount;
                ctx->GSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->GSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->GSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->GSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (GeometryShaderState* pThis = &this)
                {
                    ctx->GSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->GSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->GSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->GSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Pixel Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PixelShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int ClassInstanceCount = 256; // According to msdn

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11PixelShader> shader;
            private fixed ulong insts[ClassInstanceCount];
            private fixed ulong buffers[BufferCount];
            private fixed ulong samplers[SamplerCount];
            private fixed ulong resources[ResourceCount];
            private uint instCount;

            /// <summary>
            /// Creates a new instance of <see cref="PixelShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static PixelShaderState From(ID3D11DeviceContext* ctx)
            {
                var state = default(PixelShaderState);
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = ClassInstanceCount;
                ctx->PSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->PSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->PSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->PSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (PixelShaderState* pThis = &this)
                {
                    ctx->PSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->PSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->PSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->PSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }

        /// <summary>
        /// Captures Compute Shader states of a <see cref="ID3D11DeviceContext"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ComputeShaderState : IDisposable
        {
            private const int BufferCount = D3D11.D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT;
            private const int SamplerCount = D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT;
            private const int ResourceCount = D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT;
            private const int InstanceCount = 256; // According to msdn
            private const int UavCountMax = D3D11.D3D11_1_UAV_SLOT_COUNT;

            private ComPtr<ID3D11DeviceContext> context;
            private ComPtr<ID3D11ComputeShader> shader;
            private fixed ulong insts[InstanceCount]; // ID3D11ClassInstance*[BufferCount]
            private fixed ulong buffers[BufferCount]; // ID3D11Buffer*[BufferCount]
            private fixed ulong samplers[SamplerCount]; // ID3D11SamplerState*[SamplerCount]
            private fixed ulong resources[ResourceCount]; // ID3D11ShaderResourceView*[ResourceCount]
            private fixed ulong uavs[UavCountMax]; // ID3D11UnorderedAccessView*[UavCountMax]
            private uint instCount;
            private int uavCount;

            /// <summary>
            /// Creates a new instance of <see cref="ComputeShaderState"/> from <paramref name="ctx"/>.
            /// </summary>
            /// <param name="featureLevel">The feature level.</param>
            /// <param name="ctx">The device context.</param>
            /// <returns>The captured state.</returns>
            public static ComputeShaderState From(D3D_FEATURE_LEVEL featureLevel, ID3D11DeviceContext* ctx)
            {
                var state = default(ComputeShaderState);
                state.uavCount = featureLevel >= D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1
                                     ? D3D11.D3D11_1_UAV_SLOT_COUNT
                                     : D3D11.D3D11_PS_CS_UAV_REGISTER_COUNT;
                state.context.Attach(ctx);
                ctx->AddRef();
                state.instCount = InstanceCount;
                ctx->CSGetShader(state.shader.GetAddressOf(), (ID3D11ClassInstance**)state.insts, &state.instCount);
                ctx->CSGetConstantBuffers(0, BufferCount, (ID3D11Buffer**)state.buffers);
                ctx->CSGetSamplers(0, SamplerCount, (ID3D11SamplerState**)state.samplers);
                ctx->CSGetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)state.resources);
                ctx->CSGetUnorderedAccessViews(0, (uint)state.uavCount, (ID3D11UnorderedAccessView**)state.uavs);
                return state;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var ctx = this.context.Get();
                if (ctx is null)
                    return;

                fixed (ComputeShaderState* pThis = &this)
                {
                    ctx->CSSetShader(pThis->shader, (ID3D11ClassInstance**)pThis->insts, pThis->instCount);
                    ctx->CSSetConstantBuffers(0, BufferCount, (ID3D11Buffer**)pThis->buffers);
                    ctx->CSSetSamplers(0, SamplerCount, (ID3D11SamplerState**)pThis->samplers);
                    ctx->CSSetShaderResources(0, ResourceCount, (ID3D11ShaderResourceView**)pThis->resources);
                    ctx->CSSetUnorderedAccessViews(
                        0,
                        (uint)this.uavCount,
                        (ID3D11UnorderedAccessView**)pThis->uavs,
                        null);

                    foreach (ref var b in new Span<ComPtr<ID3D11Buffer>>(pThis->buffers, BufferCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11SamplerState>>(pThis->samplers, SamplerCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ShaderResourceView>>(pThis->resources, ResourceCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11ClassInstance>>(pThis->insts, (int)pThis->instCount))
                        b.Dispose();
                    foreach (ref var b in new Span<ComPtr<ID3D11UnorderedAccessView>>(pThis->uavs, this.uavCount))
                        b.Dispose();
                    pThis->context.Dispose();
                    pThis->shader.Dispose();
                }
            }
        }
    }
}

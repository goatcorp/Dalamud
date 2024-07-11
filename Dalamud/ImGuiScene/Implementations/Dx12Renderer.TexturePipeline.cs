using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "DX12")]
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer
{
    [Guid("a29f9ceb-f89f-4f35-bc1c-a5f7dc8af0ff")]
    private sealed class TexturePipeline : ManagedComObjectBase<TexturePipeline>, INativeGuid
    {
        public static readonly Guid MyGuid =
            new(0xa29f9ceb, 0xf89f, 0x4f35, 0xbc, 0x1c, 0xa5, 0xf7, 0xdc, 0x8a, 0xf0, 0xff);

        private ComPtr<ID3D12RootSignature> rootSignature;
        private ComPtr<ID3D12PipelineState> pipelineState;

        public TexturePipeline(ID3D12RootSignature* rootSignature, ID3D12PipelineState* pipelineState)
        {
            this.rootSignature = new(rootSignature);
            this.pipelineState = new(pipelineState);
        }

        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public static TexturePipeline From(
            ID3D12Device* device,
            DXGI_FORMAT rtvFormat,
            ReadOnlySpan<byte> vs,
            ReadOnlySpan<byte> ps,
            in D3D12_STATIC_SAMPLER_DESC samplerDesc,
            string debugName)
        {
            var descRange = new D3D12_DESCRIPTOR_RANGE
            {
                RangeType = D3D12_DESCRIPTOR_RANGE_TYPE.D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
                NumDescriptors = 1,
                BaseShaderRegister = 0,
                RegisterSpace = 0,
                OffsetInDescriptorsFromTableStart = 0,
            };

            var rootParams = stackalloc D3D12_ROOT_PARAMETER[]
            {
                new()
                {
                    ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
                    Constants = new()
                    {
                        ShaderRegister = 0,
                        RegisterSpace = 0,
                        Num32BitValues = (uint)(sizeof(Matrix4x4) / sizeof(float)),
                    },
                    ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_VERTEX,
                },
                new()
                {
                    ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
                    DescriptorTable = new() { NumDescriptorRanges = 1, pDescriptorRanges = &descRange },
                    ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_PIXEL,
                },
            };

            using var rootSignature = default(ComPtr<ID3D12RootSignature>);
            using (var successBlob = default(ComPtr<ID3DBlob>))
            using (var errorBlob = default(ComPtr<ID3DBlob>))
            {
                fixed (D3D12_STATIC_SAMPLER_DESC* pSamplerDesc = &samplerDesc)
                {
                    var signatureDesc = new D3D12_ROOT_SIGNATURE_DESC
                    {
                        NumParameters = 2,
                        pParameters = rootParams,
                        NumStaticSamplers = 1,
                        pStaticSamplers = pSamplerDesc,
                        Flags =
                            D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
                            D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
                            D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
                            D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS,
                    };

                    var hr = DirectX.D3D12SerializeRootSignature(
                        &signatureDesc,
                        D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1,
                        successBlob.GetAddressOf(),
                        errorBlob.GetAddressOf());
                    if (hr.FAILED)
                    {
                        var err = new Span<byte>(
                            errorBlob.Get()->GetBufferPointer(),
                            (int)errorBlob.Get()->GetBufferSize());
                        throw new AggregateException(Encoding.UTF8.GetString(err), Marshal.GetExceptionForHR(hr)!);
                    }
                }

                fixed (Guid* piid = &IID.IID_ID3D12RootSignature)
                {
                    device->CreateRootSignature(
                        0,
                        successBlob.Get()->GetBufferPointer(),
                        successBlob.Get()->GetBufferSize(),
                        piid,
                        (void**)rootSignature.GetAddressOf()).ThrowOnError();
                }
            }

            fixed (void* pName = $"{debugName}:RootSignature")
                rootSignature.Get()->SetName((ushort*)pName).ThrowOnError();

            using var pipelineState = default(ComPtr<ID3D12PipelineState>);
            fixed (void* pvs = vs)
            fixed (void* pps = ps)
            fixed (Guid* piidPipelineState = &IID.IID_ID3D12PipelineState)
            fixed (void* pszPosition = "POSITION"u8)
            fixed (void* pszTexCoord = "TEXCOORD"u8)
            fixed (void* pszColor = "COLOR"u8)
            {
                var layout = stackalloc D3D12_INPUT_ELEMENT_DESC[]
                {
                    new()
                    {
                        SemanticName = (sbyte*)pszPosition,
                        Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                        AlignedByteOffset = uint.MaxValue,
                        InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                    },
                    new()
                    {
                        SemanticName = (sbyte*)pszTexCoord,
                        Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                        AlignedByteOffset = uint.MaxValue,
                        InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                    },
                    new()
                    {
                        SemanticName = (sbyte*)pszColor,
                        Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                        AlignedByteOffset = uint.MaxValue,
                        InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                    },
                };
                var pipelineDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC
                {
                    NodeMask = 1,
                    PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                    pRootSignature = rootSignature,
                    SampleMask = uint.MaxValue,
                    NumRenderTargets = 1,
                    RTVFormats = new() { e0 = rtvFormat },
                    SampleDesc = new(1, 0),
                    Flags = D3D12_PIPELINE_STATE_FLAGS.D3D12_PIPELINE_STATE_FLAG_NONE,
                    VS = new(pvs, (nuint)vs.Length),
                    PS = new(pps, (nuint)ps.Length),
                    InputLayout = new() { pInputElementDescs = layout, NumElements = 3 },
                    BlendState = new()
                    {
                        AlphaToCoverageEnable = false,
                        RenderTarget = new()
                        {
                            e0 = new()
                            {
                                BlendEnable = true,
                                SrcBlend = D3D12_BLEND.D3D12_BLEND_SRC_ALPHA,
                                DestBlend = D3D12_BLEND.D3D12_BLEND_INV_SRC_ALPHA,
                                BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
                                SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_INV_DEST_ALPHA,
                                DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE,
                                BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
                                RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_ALL,
                            },
                        },
                    },
                    RasterizerState = new()
                    {
                        FillMode = D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
                        CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_NONE,
                        FrontCounterClockwise = false,
                        DepthBias = D3D12.D3D12_DEFAULT_DEPTH_BIAS,
                        DepthBiasClamp = D3D12.D3D12_DEFAULT_DEPTH_BIAS_CLAMP,
                        SlopeScaledDepthBias = D3D12.D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS,
                        DepthClipEnable = true,
                        MultisampleEnable = false,
                        AntialiasedLineEnable = false,
                        ForcedSampleCount = 0,
                        ConservativeRaster =
                            D3D12_CONSERVATIVE_RASTERIZATION_MODE.D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF,
                    },
                    DepthStencilState = new()
                    {
                        DepthEnable = false,
                        DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ALL,
                        DepthFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_ALWAYS,
                        StencilEnable = false,
                        FrontFace = new()
                        {
                            StencilFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilDepthFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilPassOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_ALWAYS,
                        },
                        BackFace = new()
                        {
                            StencilFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilDepthFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilPassOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                            StencilFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_ALWAYS,
                        },
                    },
                };
                device->CreateGraphicsPipelineState(
                    &pipelineDesc,
                    piidPipelineState,
                    (void**)pipelineState.GetAddressOf()).ThrowOnError();
            }

            fixed (void* pName = $"{debugName}:PipelineState")
                pipelineState.Get()->SetName((ushort*)pName).ThrowOnError();

            return new(rootSignature, pipelineState);
        }

        public static TexturePipeline From(
            ID3D12Device* device,
            DXGI_FORMAT rtvFormat,
            ReadOnlySpan<byte> vs,
            ReadOnlySpan<byte> ps,
            string debugName) => From(
            device,
            rtvFormat,
            vs,
            ps,
            new()
            {
                Filter = D3D12_FILTER.D3D12_FILTER_MIN_MAG_MIP_LINEAR,
                AddressU = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_WRAP,
                AddressV = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_WRAP,
                AddressW = D3D12_TEXTURE_ADDRESS_MODE.D3D12_TEXTURE_ADDRESS_MODE_WRAP,
                MipLODBias = 0,
                MaxAnisotropy = 0,
                ComparisonFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_ALWAYS,
                BorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK,
                MinLOD = 0,
                MaxLOD = 0,
                ShaderRegister = 0,
                RegisterSpace = 0,
                ShaderVisibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_PIXEL,
            },
            debugName);

        public void BindTo(ID3D12GraphicsCommandList* ctx)
        {
            ctx->SetPipelineState(this.pipelineState);
            ctx->SetGraphicsRootSignature(this.rootSignature);
        }

        protected override void* DynamicCast(in Guid iid) =>
            iid == MyGuid ? this.AsComInterface() : base.DynamicCast(iid);

        protected override void FinalRelease()
        {
            this.rootSignature.Reset();
            this.pipelineState.Reset();
        }
    }

    private class TexturePipelineWrap : ITexturePipelineWrap
    {
        private TexturePipeline? data;

        private TexturePipelineWrap(TexturePipeline data) => this.data = data;

        ~TexturePipelineWrap() => this.ReleaseUnmanagedResources();

        public TexturePipeline Data => this.data ?? throw new ObjectDisposedException(nameof(TextureWrap));

        public bool IsDisposed => this.data is null;

        public static TexturePipelineWrap TakeOwnership(TexturePipeline data) => new(data);

        public static TexturePipelineWrap NewReference(TexturePipeline data) => new(data.CloneRef());

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public ITexturePipelineWrap Clone() => NewReference(this.Data);

        object ICloneable.Clone() => this.Clone();

        private void ReleaseUnmanagedResources()
        {
            this.data?.Dispose();
            this.data = null;
        }
    }
}

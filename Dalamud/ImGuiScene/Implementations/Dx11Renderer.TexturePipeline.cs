using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 11.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx11Renderer
{
    [Guid("d3a0fe60-060a-49d6-8f6d-68e2ec5905c5")]
    private class TexturePipeline : ManagedComObjectBase<TexturePipeline>, INativeGuid
    {
        private static readonly Guid MyGuid =
            new(0xd3a0fe60, 0x060a, 0x49d6, 0x8f, 0x6d, 0x68, 0xe2, 0xec, 0x59, 0x05, 0xc5);

        private ComPtr<ID3D11PixelShader> shader;
        private ComPtr<ID3D11SamplerState> sampler;

        public TexturePipeline(ID3D11PixelShader* pixelShader, ID3D11SamplerState* samplerState)
        {
            this.shader = new(pixelShader);
            this.sampler = new(samplerState);
        }

        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public static TexturePipeline From(
            ID3D11Device* device,
            ReadOnlySpan<byte> ps,
            in D3D11_SAMPLER_DESC samplerDesc)
        {
            using var shader = default(ComPtr<ID3D11PixelShader>);
            fixed (byte* pArray = ps)
                device->CreatePixelShader(pArray, (nuint)ps.Length, null, shader.GetAddressOf()).ThrowOnError();

            using var sampler = default(ComPtr<ID3D11SamplerState>);
            fixed (D3D11_SAMPLER_DESC* pSamplerDesc = &samplerDesc)
                device->CreateSamplerState(pSamplerDesc, sampler.GetAddressOf()).ThrowOnError();

            return new(shader, sampler);
        }

        public static TexturePipeline From(
            ID3D11Device* device,
            ReadOnlySpan<byte> ps) => From(
            device,
            ps,
            new()
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
            });

        public void BindTo(ID3D11DeviceContext* ctx)
        {
            ctx->PSSetShader(this.shader, null, 0);
            ctx->PSSetSamplers(0, 1, this.sampler.GetAddressOf());
        }

        protected override void* DynamicCast(in Guid guid) => guid == MyGuid ? this.AsComInterface() : null;

        protected override void FinalRelease()
        {
            this.shader.Reset();
            this.sampler.Reset();
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

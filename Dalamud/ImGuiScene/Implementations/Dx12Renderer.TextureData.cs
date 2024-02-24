using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Internal;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using Win32 = TerraFX.Interop.Windows.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer
{
    [Guid("f58175a6-37da-4daa-82fd-5993f6847643")]
    private class TextureData : ManagedComObjectBase<TextureData>, INativeGuid
    {
        public static readonly Guid MyGuid =
            new(0xf58175a6, 0x37da, 0x4daa, 0x82, 0xfd, 0x59, 0x93, 0xf6, 0x84, 0x76, 0x43);

        private ComPtr<ID3D12Resource> texture;
        private ComPtr<ID3D12Resource> uploadBuffer;
        private TexturePipeline? customPipeline;

        public TextureData(
            DXGI_FORMAT format,
            int width,
            int height,
            int uploadPitch,
            ID3D12Resource* texture,
            ID3D12Resource* uploadBuffer)
        {
            this.texture = new(texture);
            this.uploadBuffer = new(uploadBuffer);
            this.Format = format;
            this.Width = width;
            this.Height = height;
            this.UploadPitch = uploadPitch;
        }
        
        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public DXGI_FORMAT Format { get; private init; }

        public int Width { get; private init; }

        public int Height { get; private init; }

        public int UploadPitch { get; private init; }

        public TexturePipeline? CustomPipeline
        {
            get => this.customPipeline;
            set
            {
                if (value == this.customPipeline)
                    return;
                this.customPipeline?.Dispose();
                this.customPipeline = value?.CloneRef();
            }
        }

        public ID3D12Resource* Texture => this.texture;

        public void ClearUploadBuffer() => this.uploadBuffer.Reset();

        public void WriteCopyCommand(ID3D12GraphicsCommandList* cmdList)
        {
            var srcLocation = new D3D12_TEXTURE_COPY_LOCATION
            {
                pResource = this.uploadBuffer,
                Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                PlacedFootprint = new()
                {
                    Footprint = new()
                    {
                        Format = this.Format,
                        Width = (uint)this.Width,
                        Height = (uint)this.Height,
                        Depth = 1,
                        RowPitch = (uint)this.UploadPitch,
                    },
                },
            };

            var dstLocation = new D3D12_TEXTURE_COPY_LOCATION
            {
                pResource = this.texture,
                Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                SubresourceIndex = 0,
            };

            cmdList->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocation, null);
        }

        protected override void* DynamicCast(in Guid iid) =>
            iid == MyGuid ? this.AsComInterface() : base.DynamicCast(iid);

        protected override void FinalRelease()
        {
            this.texture.Reset();
            this.uploadBuffer.Reset();
            this.customPipeline?.Dispose();
            this.customPipeline = null;
        }
    }

    private class TextureWrap : IDalamudTextureWrap, ICloneable
    {
        private TextureData? data;

        private TextureWrap(TextureData data) => this.data = data;

        ~TextureWrap() => this.ReleaseUnmanagedResources();

        public TextureData Data => this.data ?? throw new ObjectDisposedException(nameof(TextureWrap));

        public bool IsDisposed => this.data is null;

        public nint ImGuiHandle => (nint)this.Data.AsComInterface();

        public int Width => this.Data.Width;

        public int Height => this.Data.Height;

        public static TextureWrap TakeOwnership(TextureData data) => new(data);

        public static TextureWrap NewReference(TextureData data) => new(data.CloneRef());

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public IDalamudTextureWrap Clone() => NewReference(this.Data);

        object ICloneable.Clone() => this.Clone();

        private void ReleaseUnmanagedResources()
        {
            this.data?.Dispose();
            this.data = null;
        }
    }
}

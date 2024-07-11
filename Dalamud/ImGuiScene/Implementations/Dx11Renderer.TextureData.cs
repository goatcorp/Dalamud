using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Textures.TextureWraps;

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
    [Guid("72fe3f82-3ffc-4be9-b008-4aef7a942f55")]
    private class TextureData : ManagedComObjectBase<TextureData>, INativeGuid
    {
        public static readonly Guid MyGuid =
            new(0x72fe3f82, 0x3ffc, 0x4be9, 0xb0, 0x08, 0x4a, 0xef, 0x7a, 0x94, 0x2f, 0x55);

        private ComPtr<ID3D11Texture2D> tex2D;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private TexturePipeline? customPipeline;

        public TextureData(ID3D11Texture2D* tex2D, ID3D11ShaderResourceView* srv, int width, int height)
        {
            this.tex2D = new(tex2D);
            this.srv = new(srv);
            this.Width = width;
            this.Height = height;
        }

        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public int Width { get; private init; }

        public int Height { get; private init; }

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

        public ID3D11Texture2D* Resource => this.tex2D;

        public ID3D11ShaderResourceView* ShaderResourceView => this.srv;

        protected override void* DynamicCast(in Guid iid) =>
            iid == MyGuid ? this.AsComInterface() : base.DynamicCast(iid);

        protected override void FinalRelease()
        {
            this.tex2D.Reset();
            this.srv.Reset();
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

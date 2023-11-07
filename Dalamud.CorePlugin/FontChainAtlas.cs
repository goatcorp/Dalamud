#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Dalamud.CorePlugin;

public sealed unsafe class FontChainAtlas : IDisposable
{
    private readonly ImFontAtlas* pAtlas;

    public FontChainAtlas()
    {
        this.pAtlas = ImGuiNative.ImFontAtlas_ImFontAtlas();
        this.pAtlas->TexDesiredWidth = 1024;
        this.pAtlas->TexDesiredWidth = 1024;
        this.pAtlas->TexGlyphPadding = 1;
        this.pAtlas->TexReady = 0;
        this.pAtlas->TexWidth = 1024;
        this.pAtlas->TexWidth = 1024;
        this.ImTextures = new(&this.pAtlas->Textures, null);
        this.Textures = new();
        this.Fonts = new(&this.pAtlas->Fonts, x => x->Destroy());
        this.CustomRects = new(&this.pAtlas->CustomRects, null);
        this.FontConfigs = new(&this.pAtlas->ConfigData, null);

        // need a space for shapes, so need to call Build
        // calling Build does AddFontDefault anyway if no font is configured
        this.AtlasPtr.AddFontDefault();
        this.AtlasPtr.Build();
        this.EnsureTextures();
    }

    ~FontChainAtlas() => this.ReleaseUnmanagedResources();

    public bool IsDisposed { get; private set; } = true;

    private ImFontAtlasPtr AtlasPtr => new(this.pAtlas);

    private Dictionary<(FontIdent Ident, float Size), FontWrapper> FontEntries { get; } = new();

    private Dictionary<FontChain, FontWrapper> FontChains { get; } = new();

    private Dictionary<FontWrapper, int> FontWrapperIndices { get; } = new();

    private ImVectorWrapper<ImFontAtlasTexture> ImTextures { get; }

    private List<TextureInfo> Textures { get; }

    private ImVectorWrapper<ImFontPtr> Fonts { get; }

    private ImVectorWrapper<ImGuiHelpers.ImFontAtlasCustomRectReal> CustomRects { get; }

    private ImVectorWrapper<ImFontConfig> FontConfigs { get; }

    public ImFontPtr this[int index] => this.Fonts[index];

    public ImFontPtr this[FontIdent ident, float size]
    {
        get
        {
            if (this.FontEntries.TryGetValue((ident, size), out var wrapper))
                return this[this.FontWrapperIndices[wrapper]];

            throw new NotImplementedException();
        }
    }

    public ImFontPtr this[FontChain chain]
    {
        get
        {
            if (this.FontChains.TryGetValue(chain, out var wrapper))
                return this[this.FontWrapperIndices[wrapper]];

            wrapper = new();
            try
            {
                wrapper.Font.FontSize = MathF.Round(chain.Fonts.First().SizePx * chain.LineHeight);
                throw new NotImplementedException();
            }
            catch (Exception)
            {
                wrapper.Dispose();
                throw;
            }
        }
    }

    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.ReleaseUnmanagedResources();
        this.Textures.DisposeItems();

        this.IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    public ImFontPtr EnsureGlyphs(ImFontPtr font, IEnumerable<char> str)
    {
        throw new NotImplementedException();
    }

    private void EnsureTextures()
    {
        while (this.Textures.Count < this.ImTextures.Length)
            this.Textures.Add(new(Service<InterfaceManager>.Get().Device!, this, this.Textures.Count));
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.IsDisposed)
        {
            ImGuiNative.ImFontAtlas_destroy(this.pAtlas);
            this.IsDisposed = true;
        }
    }

    private sealed class TextureInfo : IDisposable
    {
        public TextureInfo(D3D11Device device, FontChainAtlas owner, int textureIndex)
        {
            var errorDisposals = new DisposeStack();

            ref var imTexture = ref owner.ImTextures[textureIndex];
            owner.AtlasPtr.GetTexDataAsRGBA32(textureIndex, out nint pixels, out var width, out var height);
            this.Data = new Span<byte>((void*)pixels, width * height * 4).ToArray();

            try
            {
                this.Device = errorDisposals.Add(device.QueryInterface<D3D11Device>());

                this.Description = new()
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    SampleDescription = new(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                };

                this.View = errorDisposals.Add(
                    new ShaderResourceView(
                        device,
                        this.Texture = errorDisposals.Add(
                            new Texture2D(device, this.Description, new DataRectangle(pixels, width * 4))),
                        new()
                        {
                            Format = this.Description.Format,
                            Dimension = ShaderResourceViewDimension.Texture2D,
                            Texture2D = { MipLevels = this.Description.MipLevels },
                        }));

                imTexture.TexID = this.View.NativePointer;
            }
            catch (Exception)
            {
                errorDisposals.Dispose();
                throw;
            }
        }

        public byte[] Data { get; }

        public Texture2DDescription Description { get; }

        public ShaderResourceView View { get; }

        public Texture2D Texture { get; }

        private D3D11Device Device { get; }

        public void Dispose()
        {
            this.View.Dispose();
            this.Texture.Dispose();
            this.Device.Dispose();
        }

        public void ApplyChanges()
        {
            var box = this.Device.ImmediateContext.MapSubresource(
                this.Texture,
                0,
                MapMode.WriteDiscard,
                D3D11MapFlags.None);
            this.Data.AsSpan().CopyTo(new((void*)box.DataPointer, this.Data.Length));
            this.Device.ImmediateContext.UnmapSubresource(this.Texture, 0);
        }
    }

    private sealed class FontWrapper : IDisposable
    {
        public FontWrapper()
        {
            this.FontNative = ImGuiNative.ImFont_ImFont();
            this.IndexedHotData = new(&this.FontNative->IndexedHotData, null);
            this.FrequentKerningPairs = new(&this.FontNative->FrequentKerningPairs, null);
            this.IndexLookup = new(&this.FontNative->IndexLookup, null);
            this.Glyphs = new(&this.FontNative->Glyphs, null);
            this.KerningPairs = new(&this.FontNative->KerningPairs, null);
        }

        ~FontWrapper()
        {
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            ImGuiNative.ImFont_destroy(this.FontNative);
        }

        public bool IsDisposed { get; private set; }

        public ref ImFont Font => ref *this.FontNative;

        public ImFontPtr FontPtr => new(this.FontNative);

        public ImVectorWrapper<float> FrequentKerningPairs { get; }

        public ImVectorWrapper<ImGuiHelpers.ImFontGlyphReal> Glyphs { get; }

        public ImVectorWrapper<ImGuiHelpers.ImFontGlyphHotDataReal> IndexedHotData { get; }

        public ImVectorWrapper<ushort> IndexLookup { get; }

        public ImVectorWrapper<ImFontKerningPairPtr> KerningPairs { get; }

        private ImFont* FontNative { get; }

        public void AddChars(IEnumerable<char> chars)
        {
            foreach (var c in chars)
            {
                this.GrowIndex(c);
            }

            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            GC.SuppressFinalize(this);
            ImGuiNative.ImFont_destroy(this.FontNative);
        }

        public void GrowIndex(ushort maxCodepoint)
        {
            var oldLength = this.Glyphs.Length;
            if (oldLength >= maxCodepoint)
                return;

            this.Glyphs.Resize(maxCodepoint);
            this.IndexedHotData.Resize(maxCodepoint);
            this.IndexLookup.Resize(maxCodepoint);
            for (var i = oldLength; i < this.Glyphs.Length; i++)
            {
                this.Glyphs[i].Codepoint = i;
                this.Glyphs[i].Visible = true;
                this.IndexLookup[i] = unchecked((ushort)i);

                // Mark 4K page as used
                var pageIndex = unchecked((ushort)(i / 4096));
                this.Font.Used4kPagesMap[pageIndex >> 3] |= unchecked((byte)(1 << (pageIndex & 7)));
            }
        }
    }
}

#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Dalamud.CorePlugin.MyFonts.ImFontWrappers;
using Dalamud.Data;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Data.Files;

namespace Dalamud.CorePlugin.MyFonts;

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
        this.TextureWraps = new();
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

    public bool IsDisposed { get; private set; }

    private ImFontAtlasPtr AtlasPtr => new(this.pAtlas);

    private Dictionary<(FontIdent Ident, float Size), ImFontWrapper> FontEntries { get; } = new();

    private Dictionary<FontChain, ImFontWrapper> FontChains { get; } = new();

    private Dictionary<ImFontWrapper, int> ImFontWrapperIndices { get; } = new();

    private List<IDalamudTextureWrap> TextureWraps { get; }

    private Dictionary<string, int[]> GameFontTextures { get; } = new();

    private ImVectorWrapper<ImFontAtlasTexture> ImTextures { get; }

    private ImVectorWrapper<ImFontPtr> Fonts { get; }

    private ImVectorWrapper<ImGuiHelpers.ImFontAtlasCustomRectReal> CustomRects { get; }

    private ImVectorWrapper<ImFontConfig> FontConfigs { get; }

    public ImFontPtr this[in FontIdent ident, float sizePx] =>
        this.Fonts[this.ImFontWrapperIndices[this.GetWrapper(ident, sizePx)]];

    public ImFontPtr this[in FontChain chain]
    {
        get
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(FontChainAtlas));

            if (chain.Fonts.All(x => x.Ident == default))
                throw new ArgumentException("Font chain cannot be empty", nameof(chain));

            if (this.FontChains.TryGetValue(chain, out var wrapper))
                return this.Fonts[this.ImFontWrapperIndices[wrapper]];

            wrapper = new ChainedImFontWrapper(
                this,
                chain,
                chain.Fonts.Select(entry => this.GetWrapper(entry.Ident, entry.SizePx)));

            this.FontChains[chain] = wrapper;
            this.ImFontWrapperIndices[wrapper] = this.Fonts.Length;
            this.Fonts.Add(wrapper.FontPtr);
            return wrapper.FontPtr;
        }
    }

    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.ReleaseUnmanagedResources();
        this.TextureWraps.DisposeItems();
        this.TextureWraps.Clear();
        this.FontEntries.Clear();
        this.FontChains.Clear();
        this.ImFontWrapperIndices.Clear();

        this.IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Load the glyphs corresponding to the given chars into currently active ImGui font, if it is managed by this.
    /// </summary>
    /// <param name="str">Chars.</param>
    public void LoadGlyphs(IEnumerable<char> str) => this.LoadGlyphs(ImGui.GetFont(), str);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into <paramref name="font"/>, if it is managed by this.
    /// </summary>
    /// <param name="font">Relevant font.</param>
    /// <param name="str">Chars.</param>
    public void LoadGlyphs(ImFontPtr font, IEnumerable<char> str) =>
        this.FontChains.Values
            .Concat(this.FontEntries.Values)
            .FirstOrDefault(x => x.FontPtr.NativePtr == font.NativePtr)
            ?.LoadGlyphs(str);

    private ImFontWrapper GetWrapper(in FontIdent ident, float sizePx)
    {
        if (this.IsDisposed)
            throw new ObjectDisposedException(nameof(FontChainAtlas));

        if (this.FontEntries.TryGetValue((ident, sizePx), out var wrapper))
            return wrapper;

        switch (ident)
        {
            case { Game: not GameFontFamily.Undefined }:
            {
                var dm = Service<DataManager>.Get();
                var gfm = Service<GameFontManager>.Get();
                var tm = Service<TextureManager>.Get();

                var gfs = new GameFontStyle(ident.Game, sizePx);
                if (Math.Abs(gfs.SizePx - sizePx) < 0.0001)
                {
                    const string filename = "font{}.tex";
                    var fdt = gfm.GetFdtReader(gfs.FamilyAndSize)
                              ?? throw new FileNotFoundException($"{gfs} not found");
                    var numExpectedTex = fdt.Glyphs.Max(x => x.TextureFileIndex) + 1;
                    if (!this.GameFontTextures.TryGetValue(filename, out var textureIndices)
                        || textureIndices.Length < numExpectedTex)
                    {
                        this.EnsureTextures();

                        var newTextureWraps = new IDalamudTextureWrap?[numExpectedTex];
                        var newTextureIndices = new int[numExpectedTex];
                        using (var errorDispose = new DisposeStack())
                        {
                            var addCounter = 0;
                            for (var i = 0; i < numExpectedTex; i++)
                            {
                                // Note: texture index for these cannot be 0, since it is occupied by ImGui.
                                if (textureIndices is not null && i < textureIndices.Length)
                                {
                                    newTextureIndices[i] = textureIndices[i];
                                    Debug.Assert(
                                        this.TextureWraps[i] is not null,
                                        "textureIndices[i] != 0 but this.TextureWraps[i] is null");
                                    continue;
                                }

                                var path = $"common/font/font{i + 1}.tex";
                                newTextureWraps[i] = errorDispose.Add(
                                    tm.GetTexture(
                                        dm.GetFile<TexFile>(path)
                                        ?? throw new FileNotFoundException("File not found", path)));
                                newTextureIndices[i] = this.TextureWraps.Count + addCounter++;
                            }

                            this.ImTextures.EnsureCapacity(this.ImTextures.Length + addCounter);
                            this.TextureWraps.EnsureCapacity(this.TextureWraps.Count + addCounter);
                            errorDispose.Cancel();
                        }

                        this.GameFontTextures[filename] = textureIndices = newTextureIndices;

                        foreach (var i in Enumerable.Range(0, numExpectedTex))
                        {
                            if (newTextureWraps[i] is not { } wrap)
                                continue;

                            Debug.Assert(
                                textureIndices[i] == this.ImTextures.Length
                                && textureIndices[i] == this.TextureWraps.Count,
                                "Counts must be same");
                            this.ImTextures.Add(new() { TexID = wrap.ImGuiHandle });
                            this.TextureWraps.Add(wrap);
                        }
                    }

                    wrapper = new AxisImFontWrapper(this, fdt, textureIndices);
                }
                else
                {
                    var baseFontIdent = this[ident, gfs.SizePx].NativePtr;
                    wrapper = new ScaledImFontWrapper(
                        this,
                        this.FontEntries.Single(x => x.Value.FontPtr.NativePtr == baseFontIdent).Value,
                        sizePx / gfs.SizePx);
                }

                break;
            }

            case { System: { Name: { } name, Variant: { } variant } }:
                throw new NotImplementedException();

            case { File: { Path: { } path, Index: { } index } }:
                throw new NotImplementedException();

            default:
                throw new NotSupportedException();
        }

        this.FontEntries[(ident, sizePx)] = wrapper;
        this.ImFontWrapperIndices[wrapper] = this.Fonts.Length;
        this.Fonts.Add(wrapper.FontPtr);
        wrapper.Font.ContainerAtlas = this.AtlasPtr;

        return wrapper;
    }

    private void EnsureTextures()
    {
        var im = Service<InterfaceManager>.Get();
        while (this.TextureWraps.Count < this.ImTextures.Length)
        {
            var textureIndex = this.TextureWraps.Count;
            ref var imTexture = ref this.ImTextures[textureIndex];
            this.AtlasPtr.GetTexDataAsRGBA32(textureIndex, out nint pixels, out var width, out var height);

            this.TextureWraps.Add(new UpdateableTextureWrap(im.Device!, pixels, width, height));
            imTexture.TexID = this.TextureWraps[^1].ImGuiHandle;

            if (imTexture.TexPixelsAlpha8 is not null)
                ImGuiNative.igMemFree(imTexture.TexPixelsAlpha8);
            imTexture.TexPixelsAlpha8 = null;
            if (imTexture.TexPixelsRGBA32 is not null)
                ImGuiNative.igMemFree(imTexture.TexPixelsRGBA32);
            imTexture.TexPixelsRGBA32 = null;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.IsDisposed)
        {
            ImGuiNative.ImFontAtlas_destroy(this.pAtlas);
            this.IsDisposed = true;
        }
    }
}

#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;

using Dalamud.Configuration.Internal;
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
    private readonly byte[] gammaTable = new byte[256];
    private float lastGamma = float.NaN;

    public FontChainAtlas()
    {
        this.pAtlas = ImGuiNative.ImFontAtlas_ImFontAtlas();
        this.pAtlas->TexWidth = this.pAtlas->TexDesiredWidth = 1024;
        this.pAtlas->TexHeight = this.pAtlas->TexDesiredHeight = 1024;
        this.pAtlas->TexGlyphPadding = 1;
        this.pAtlas->TexReady = 0;
        this.ImTextures = new(&this.pAtlas->Textures, null);
        this.TextureWraps = new();
        this.Fonts = new(&this.pAtlas->Fonts, x => x->Destroy());
        this.CustomRects = new(&this.pAtlas->CustomRects, null);
        this.FontConfigs = new(&this.pAtlas->ConfigData, null);

        // need a space for shapes, so need to call Build
        // calling Build does AddFontDefault anyway if no font is configured
        var conf = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
        {
            GlyphRanges = Service<ImGuiRangeHandles>.Get().Dummy.AddrOfPinnedObject(),
        };
        try
        {
            this.AtlasPtr.AddFontDefault(conf);
            this.AtlasPtr.Build();
            this.Fonts.Clear();
        }
        finally
        {
            conf.Destroy();
        }

        this.UpdateTextures();
    }

    ~FontChainAtlas() => this.ReleaseUnmanagedResources();

    public bool IsDisposed { get; private set; }

    public int SuppressTextureUpdate { get; set; }

    internal List<IDalamudTextureWrap> TextureWraps { get; }

    internal ImVectorWrapper<ImFontAtlasTexture> ImTextures { get; }

    internal ImVectorWrapper<ImFontPtr> Fonts { get; }

    internal ImVectorWrapper<ImGuiHelpers.ImFontAtlasCustomRectReal> CustomRects { get; }

    internal ImVectorWrapper<ImFontConfig> FontConfigs { get; }

    internal ImFontAtlasPtr AtlasPtr => new(this.pAtlas);

    internal byte[] GammaMultiplicationTable
    {
        get
        {
            var gamma = Service<DalamudConfiguration>.Get().FontGammaLevel;
            if (Math.Abs(this.lastGamma - gamma) >= 0.0001)
                return this.gammaTable;

            for (var i = 0; i < 256; i++)
                this.gammaTable[i] = (byte)(MathF.Pow(Math.Clamp(i / 255.0f, 0.0f, 1.0f), 1.0f / gamma) * 255.0f);
            this.lastGamma = gamma;
            return this.gammaTable;
        }
    }

    private Dictionary<(FontIdent Ident, float Size), ImFontWrapper> FontEntries { get; } = new();

    private Dictionary<FontChain, ImFontWrapper> FontChains { get; } = new();

    private Dictionary<ImFontWrapper, int> ImFontWrapperIndices { get; } = new();

    private Dictionary<string, int[]> GameFontTextures { get; } = new();

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
            wrapper.Font.ContainerAtlas = this.AtlasPtr;
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

    public IDisposable SuppressTextureUpdatesScoped()
    {
        this.SuppressTextureUpdate++;
        return Disposable.Create(() =>
        {
            this.SuppressTextureUpdate--;
            this.UpdateTextures();
        });
    }

    public void UpdateTextures()
    {
        var im = Service<InterfaceManager>.Get();
        foreach (var textureIndex in Enumerable.Range(0, this.ImTextures.Length))
        {
            if (textureIndex < this.TextureWraps.Count)
            {
                if (this.SuppressTextureUpdate <= 0 && this.TextureWraps[textureIndex] is UpdateableTextureWrap utw)
                    utw.ApplyChanges();
                continue;
            }

            ref var imTexture = ref this.ImTextures[textureIndex];
            UpdateableTextureWrap wrap;
            if (imTexture.TexPixelsAlpha8 is null && imTexture.TexPixelsRGBA32 is null)
            {
                var width = this.AtlasPtr.TexWidth;
                var height = this.AtlasPtr.TexHeight;
                wrap = new(im.Device!, 0, width, height);
            }
            else
            {
                // Note: pixels expect BGRA32, but we feed it RGBA32, and that's fine; R=G=B
                this.AtlasPtr.GetTexDataAsRGBA32(textureIndex, out nint pixels, out var width, out var height);
                wrap = new(im.Device!, pixels, width, height);

                // We rely on the implementation detail that default custom rects stick to top left.
                var occupiedWidth = this.CustomRects.Aggregate(0, (a, x) => Math.Max(a, x.X + x.Width)) + 1;
                var occupiedHeight = this.CustomRects.Aggregate(0, (a, x) => Math.Max(a, x.Y + x.Height)) + 1;
                foreach (var p in wrap.Packers)
                    p.PackRect(occupiedWidth, occupiedHeight, null!);
            }

            this.TextureWraps.Add(wrap);
            imTexture.TexID = this.TextureWraps[^1].ImGuiHandle;

            if (imTexture.TexPixelsAlpha8 is not null)
                ImGuiNative.igMemFree(imTexture.TexPixelsAlpha8);
            imTexture.TexPixelsAlpha8 = null;
            if (imTexture.TexPixelsRGBA32 is not null)
                ImGuiNative.igMemFree(imTexture.TexPixelsRGBA32);
            imTexture.TexPixelsRGBA32 = null;
        }
    }

    internal ImFontWrapper GetWrapper(in FontIdent ident, float sizePx)
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

                var gfs = new GameFontStyle(new GameFontStyle(ident.Game, sizePx).FamilyAndSize);
                if (Math.Abs(gfs.SizePx - sizePx) < 0.0001)
                {
                    const string filename = "font{}.tex";
                    var fdt = gfm.GetFdtReader(gfs.FamilyAndSize)
                              ?? throw new FileNotFoundException($"{gfs} not found");
                    var numExpectedTex = fdt.Glyphs.Max(x => x.TextureFileIndex) + 1;
                    if (!this.GameFontTextures.TryGetValue(filename, out var textureIndices)
                        || textureIndices.Length < numExpectedTex)
                    {
                        this.UpdateTextures();

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
                wrapper = DirectWriteFontWrapper.FromSystem(this, name, variant, sizePx);
                break;

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

    private void ReleaseUnmanagedResources()
    {
        if (this.IsDisposed)
        {
            ImGuiNative.ImFontAtlas_destroy(this.pAtlas);
            this.IsDisposed = true;
        }
    }
}

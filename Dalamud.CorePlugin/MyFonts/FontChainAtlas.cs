#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.Unicode;

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

/// <summary>
/// A wrapper for <see cref="ImFontAtlas"/> for managing fonts in a easy way.
/// </summary>
public sealed unsafe class FontChainAtlas : IDisposable
{
    private readonly ImFontAtlas* pAtlas;
    private readonly byte[] gammaTable = new byte[256];
    private float lastGamma = float.NaN;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontChainAtlas"/> class.
    /// </summary>
    // TODO: add function into InterfaceManager for plugins or smth
    internal FontChainAtlas()
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

    /// <summary>
    /// Finalizes an instance of the <see cref="FontChainAtlas"/> class.
    /// </summary>
    ~FontChainAtlas() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets a value indicating whether it is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the reasons why <see cref="FontChain"/>s have failed to load.
    /// </summary>
    public IReadOnlyDictionary<FontChain, Exception> FailedChains => this.FailedChainsPrivate;

    /// <summary>
    /// Gets the reasons why <see cref="FontIdent"/>s have failed to load.
    /// </summary>
    public IReadOnlyDictionary<(FontIdent Ident, float SizePx), Exception> FailedIdents => this.FailedIdentsPrivate;

    /// <summary>
    /// Gets the list of associated <see cref="IDalamudTextureWrap"/>.
    /// </summary>
    internal List<IDalamudTextureWrap> TextureWraps { get; }

    /// <summary>
    /// Gets the wrapped vector of <see cref="ImFontAtlasTexture"/>.
    /// </summary>
    internal ImVectorWrapper<ImFontAtlasTexture> ImTextures { get; }

    /// <summary>
    /// Gets the wrapped vector of <see cref="ImFontPtr"/>.
    /// </summary>
    internal ImVectorWrapper<ImFontPtr> Fonts { get; }

    /// <summary>
    /// Gets the wrapped vector of <see cref="ImGuiHelpers.ImFontAtlasCustomRectReal"/>.
    /// </summary>
    internal ImVectorWrapper<ImGuiHelpers.ImFontAtlasCustomRectReal> CustomRects { get; }

    /// <summary>
    /// Gets the wrapped vector of <see cref="ImFontConfig"/>.
    /// </summary>
    internal ImVectorWrapper<ImFontConfig> FontConfigs { get; }

    /// <summary>
    /// Gets the wrapped <see cref="ImFontAtlasPtr"/>.
    /// </summary>
    internal ImFontAtlasPtr AtlasPtr => new(this.pAtlas);

    /// <summary>
    /// Gets the gamma mapping table.
    /// </summary>
    internal byte[] GammaMappingTable
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

    private int SuppressTextureUpdate { get; set; }

    private Dictionary<(FontIdent Ident, float Size), ImFontWrapper> FontEntries { get; } = new();

    private Dictionary<FontChain, ImFontWrapper> FontChains { get; } = new();

    private Dictionary<ImFontWrapper, int> ImFontWrapperIndices { get; } = new();

    private Dictionary<string, int[]> GameFontTextures { get; } = new();

    private Dictionary<FontChain, Exception> FailedChainsPrivate { get; } = new();

    private Dictionary<(FontIdent Ident, float SizePx), Exception> FailedIdentsPrivate { get; } = new();

    /// <summary>
    /// Gets the font corresponding to the given specifications.
    /// </summary>
    /// <param name="ident">Font identifier.</param>
    /// <param name="sizePx">Size in pixels.</param>
    public ImFontPtr this[in FontIdent ident, float sizePx] =>
        this.Fonts[this.ImFontWrapperIndices[this.GetWrapper(ident, sizePx)]];

    /// <summary>
    /// Gets the font corresponding to the given specifications.
    /// </summary>
    /// <param name="chain">Font chain.</param>
    public ImFontPtr this[in FontChain chain]
    {
        get
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(FontChainAtlas));

            if (this.FailedChainsPrivate.TryGetValue(chain, out var previousException))
                throw previousException;

            try
            {
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
            catch (Exception e)
            {
                this.FailedChainsPrivate[chain] = e;
                throw;
            }
        }
    }

    /// <summary>
    /// Reset recorded font load errors, so that on next access, font will be attempted for load again.
    /// </summary>
    public void ClearLoadErrorHistory()
    {
        this.FailedChainsPrivate.Clear();
        this.FailedIdentsPrivate.Clear();
    }

    /// <inheritdoc/>
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
    /// <param name="chars">Chars.</param>
    public void LoadGlyphs(params char[] chars) => this.LoadGlyphs(ImGui.GetFont(), chars);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into currently active ImGui font, if it is managed by this.
    /// </summary>
    /// <param name="chars">Chars.</param>
    public void LoadGlyphs(IEnumerable<char> chars) => this.LoadGlyphs(ImGui.GetFont(), chars);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into <paramref name="font"/>, if it is managed by this.
    /// </summary>
    /// <param name="font">Relevant font.</param>
    /// <param name="chars">Chars.</param>
    public void LoadGlyphs(ImFontPtr font, IEnumerable<char> chars) =>
        this.FontChains.Values
            .Concat(this.FontEntries.Values)
            .FirstOrDefault(x => x.FontPtr.NativePtr == font.NativePtr)
            ?.LoadGlyphs(chars);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into currently active ImGui font, if it is managed by this.
    /// </summary>
    /// <param name="ranges">Ranges.</param>
    public void LoadGlyphs(params UnicodeRange[] ranges) => this.LoadGlyphs(ImGui.GetFont(), ranges);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into currently active ImGui font, if it is managed by this.
    /// </summary>
    /// <param name="ranges">Ranges.</param>
    public void LoadGlyphs(IEnumerable<UnicodeRange> ranges) => this.LoadGlyphs(ImGui.GetFont(), ranges);

    /// <summary>
    /// Load the glyphs corresponding to the given chars into <paramref name="font"/>, if it is managed by this.
    /// </summary>
    /// <param name="font">Relevant font.</param>
    /// <param name="ranges">Ranges.</param>
    public void LoadGlyphs(ImFontPtr font, IEnumerable<UnicodeRange> ranges) =>
        this.FontChains.Values
            .Concat(this.FontEntries.Values)
            .FirstOrDefault(x => x.FontPtr.NativePtr == font.NativePtr)
            ?.LoadGlyphs(ranges);

    /// <summary>
    /// Suppress uploading updated texture onto GPU for the scope.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that will make it update the texture on dispose.</returns>
    public IDisposable? SuppressTextureUpdatesScoped()
    {
        if (this.IsDisposed)
            return null;

        this.SuppressTextureUpdate++;
        return Disposable.Create(
            () =>
            {
                if (--this.SuppressTextureUpdate == 0)
                    this.UpdateTextures();
            });
    }

    /// <summary>
    /// Fetch a font, and if it succeeds, push it onto the stack.
    /// </summary>
    /// <param name="ident">Font identifier.</param>
    /// <param name="sizePx">Font size in pixels.</param>
    /// <returns>An <see cref="IDisposable"/> that will make it pop the font on dispose.</returns>
    /// <remarks>It will return null on failure, and exception will be stored in <see cref="FailedIdents"/>.</remarks>
    public IDisposable? PushFontScoped(in FontIdent ident, float sizePx)
    {
        if (this.IsDisposed)
            return null;

        if (this.FailedIdentsPrivate.TryGetValue((ident, sizePx), out _))
            return null;

        try
        {
            ImGui.PushFont(this[ident, sizePx]);
        }
        catch
        {
            return null;
        }

        this.SuppressTextureUpdate++;
        return Disposable.Create(
            () =>
            {
                ImGui.PopFont();
                if (--this.SuppressTextureUpdate == 0)
                    this.UpdateTextures();
            });
    }

    /// <summary>
    /// Fetch a font, and if it succeeds, push it onto the stack.
    /// </summary>
    /// <param name="chain">Font chain.</param>
    /// <returns>An <see cref="IDisposable"/> that will make it pop the font on dispose.</returns>
    /// <remarks>It will return null on failure, and exception will be stored in <see cref="FailedChains"/>.</remarks>
    public IDisposable? PushFontScoped(in FontChain chain)
    {
        if (this.IsDisposed)
            return null;

        if (this.FailedChainsPrivate.TryGetValue(chain, out _))
            return null;

        try
        {
            ImGui.PushFont(this[chain]);
        }
        catch
        {
            return null;
        }

        this.SuppressTextureUpdate++;
        return Disposable.Create(
            () =>
            {
                ImGui.PopFont();
                if (--this.SuppressTextureUpdate == 0)
                    this.UpdateTextures();
            });
    }

    /// <summary>
    /// Upload updated textures onto GPU, if not suppressed.
    /// </summary>
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

    /// <summary>
    /// Get the font wrapper.
    /// </summary>
    /// <param name="ident">Font identifier.</param>
    /// <param name="sizePx">Size in pixels.</param>
    /// <returns>Found font wrapper.</returns>
    internal ImFontWrapper GetWrapper(in FontIdent ident, float sizePx)
    {
        if (this.IsDisposed)
            throw new ObjectDisposedException(nameof(FontChainAtlas));

        if (this.FontEntries.TryGetValue((ident, sizePx), out var wrapper))
            return wrapper;

        if (this.FailedIdentsPrivate.TryGetValue((ident, sizePx), out var previousException))
            throw previousException;

        try
        {
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
                    // TODO
                    throw new NotImplementedException();

                default:
                    // TODO: ArgumentException?
                    throw new NotSupportedException();
            }

            this.FontEntries[(ident, sizePx)] = wrapper;
            this.ImFontWrapperIndices[wrapper] = this.Fonts.Length;
            this.Fonts.Add(wrapper.FontPtr);
            wrapper.Font.ContainerAtlas = this.AtlasPtr;

            return wrapper;
        }
        catch (Exception e)
        {
            this.FailedIdentsPrivate[(ident, sizePx)] = e;
            throw;
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

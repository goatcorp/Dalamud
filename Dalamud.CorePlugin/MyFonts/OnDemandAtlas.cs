#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Text.Unicode;

using Dalamud.Configuration.Internal;
using Dalamud.CorePlugin.MyFonts.OnDemandFonts;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts;

/// <summary>
/// A wrapper for <see cref="ImFontAtlas"/> for managing fonts in an easy way.
/// </summary>
public sealed unsafe class OnDemandAtlas : IDisposable
{
    private readonly InterfaceManager interfaceManager;
    private readonly ImFontAtlas* pAtlas;
    private readonly byte[] gammaTable = new byte[256];
    private readonly Dictionary<(FontIdent Ident, int SizePx), OnDemandFont> fontEntries = new();
    private readonly Dictionary<FontChain, OnDemandFont> fontChains = new();
    private readonly Dictionary<nint, OnDemandFont> fontPtrToFont = new();
    private readonly Dictionary<string, int[]> gameFontTextures = new();
    private readonly Dictionary<FontChain, Exception> failedChains = new();
    private readonly Dictionary<(FontIdent Ident, int SizePx), Exception> failedIdents = new();

    private float lastGamma = float.NaN;
    private int suppressTextureUpdateCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnDemandAtlas"/> class.
    /// </summary>
    /// <param name="interfaceManager">An instance of InterfaceManager.</param>
    // TODO: add function into InterfaceManager for plugins or smth
    internal OnDemandAtlas(InterfaceManager interfaceManager)
    {
        this.interfaceManager = interfaceManager;
        using var errorDispose = new DisposeStack();

        this.pAtlas = ImGuiNative.ImFontAtlas_ImFontAtlas();
        errorDispose.Add(() => ImGuiNative.ImFontAtlas_destroy(this.pAtlas));

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

        Debug.Assert(this.ImTextures.Length == 1, "this.ImTextures.Length == 1");

        // Note: pixels expect BGRA32, but we feed it RGBA32, and that's fine; R=G=B
        this.AtlasPtr.GetTexDataAsRGBA32(0, out nint pixels, out var width, out var height);
        var wrap = errorDispose.Add(
            new FontChainAtlasTextureWrap(
                this.interfaceManager.Device!,
                pixels,
                width,
                height,
                false));

        // We don't need to have ImGui keep the buffer.
        this.AtlasPtr.ClearTexData();

        // We rely on the implementation detail that default custom rects stick to top left,
        // and the rectpack we're using will stick the first item to the top left.
        wrap.Packers[0].PackRect(
            this.CustomRects.Aggregate(0, (a, x) => Math.Max(a, x.X + x.Width)) + 1,
            this.CustomRects.Aggregate(0, (a, x) => Math.Max(a, x.Y + x.Height)) + 1,
            null!);

        this.TextureWraps.Add(wrap);
        this.ImTextures[0].TexID = this.TextureWraps[^1].ImGuiHandle;

        // Mark them to use the first channel.
        foreach (ref var v4 in new Span<Vector4>(&this.pAtlas->TexUvLines_0, 64))
            v4 += new Vector4(1, 0, 1, 0);

        errorDispose.Cancel();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="OnDemandAtlas"/> class.
    /// </summary>
    ~OnDemandAtlas() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets a value indicating whether it is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the reasons why <see cref="FontChain"/>s have failed to load.
    /// </summary>
    public IReadOnlyDictionary<FontChain, Exception> FailedChains => this.failedChains;

    /// <summary>
    /// Gets the reasons why <see cref="FontIdent"/>s have failed to load.
    /// </summary>
    public IReadOnlyDictionary<(FontIdent Ident, int SizePx), Exception> FailedIdents => this.failedIdents;

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

    /// <summary>
    /// Gets the font corresponding to the given specifications.
    /// </summary>
    /// <param name="ident">Font identifier.</param>
    /// <param name="sizePx">Size in pixels.</param>
    public ImFontPtr this[in FontIdent ident, float sizePx] => this[new(new FontChainEntry(ident, sizePx))];

    /// <summary>
    /// Gets the font corresponding to the given specifications.
    /// </summary>
    /// <param name="chain">Font chain.</param>
    public ImFontPtr this[in FontChain chain]
    {
        get
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(OnDemandAtlas));

            if (this.failedChains.TryGetValue(chain, out var previousException))
                throw previousException;

            try
            {
                if (chain.Fonts.All(x => x.Ident == default))
                    throw new ArgumentException("Font chain cannot be empty", nameof(chain));

                if (this.fontChains.TryGetValue(chain, out var wrapper))
                    return wrapper.FontPtr;

                wrapper = new ChainedOnDemandFont(
                    this,
                    chain,
                    chain.Fonts.Select(entry => this.GetWrapper(entry.Ident, entry.SizePx)));

                this.fontChains[chain] = wrapper;
                this.fontPtrToFont[(nint)wrapper.FontPtr.NativePtr] = wrapper;
                this.Fonts.Add(wrapper.FontPtr);
                wrapper.Font.ContainerAtlas = this.AtlasPtr;
                return wrapper.FontPtr;
            }
            catch (Exception e)
            {
                this.failedChains[chain] = e;
                throw;
            }
        }
    }

    /// <summary>
    /// Reset recorded font load errors, so that on next access, font will be attempted for load again.
    /// </summary>
    public void ClearLoadErrorHistory()
    {
        this.failedChains.Clear();
        this.failedIdents.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.ReleaseUnmanagedResources();
        this.TextureWraps.DisposeItems();
        this.TextureWraps.Clear();
        this.fontEntries.Clear();
        this.fontChains.Clear();
        this.fontPtrToFont.Clear();

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
        this.fontPtrToFont.GetValueOrDefault((nint)font.NativePtr)?.LoadGlyphs(chars);

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
        this.fontPtrToFont.GetValueOrDefault((nint)font.NativePtr)?.LoadGlyphs(ranges);

    /// <summary>
    /// Suppress uploading updated texture onto GPU for the scope.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that will make it update the texture on dispose.</returns>
    public IDisposable? SuppressTextureUpdatesScoped()
    {
        if (this.IsDisposed)
            return null;

        this.suppressTextureUpdateCounter++;
        return Disposable.Create(
            () =>
            {
                if (--this.suppressTextureUpdateCounter == 0)
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

        if (!(sizePx > 0))
            return null;

        if (this.failedIdents.TryGetValue((ident, (int)MathF.Round(sizePx)), out _))
            return null;

        try
        {
            ImGui.PushFont(this[ident, sizePx]);
        }
        catch
        {
            return null;
        }

        this.suppressTextureUpdateCounter++;
        return Disposable.Create(
            () =>
            {
                ImGui.PopFont();
                if (--this.suppressTextureUpdateCounter == 0)
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

        if (this.failedChains.TryGetValue(chain, out _))
            return null;

        try
        {
            ImGui.PushFont(this[chain]);
        }
        catch
        {
            return null;
        }

        this.suppressTextureUpdateCounter++;
        return Disposable.Create(
            () =>
            {
                ImGui.PopFont();
                if (--this.suppressTextureUpdateCounter == 0)
                    this.UpdateTextures();
            });
    }

    /// <summary>
    /// Upload updated textures onto GPU, if not suppressed.
    /// </summary>
    public void UpdateTextures()
    {
        foreach (var tw in this.TextureWraps)
        {
            if (this.suppressTextureUpdateCounter <= 0 && tw is FontChainAtlasTextureWrap utw)
                utw.ApplyChanges();
        }
    }

    /// <summary>
    /// Get the font wrapper.
    /// </summary>
    /// <param name="ident">Font identifier.</param>
    /// <param name="sizePx">Size in pixels. Note that it will be rounded to nearest integers.</param>
    /// <returns>Found font wrapper.</returns>
    internal OnDemandFont GetWrapper(in FontIdent ident, float sizePx)
    {
        if (this.IsDisposed)
            throw new ObjectDisposedException(nameof(OnDemandAtlas));

        var sizeInt = (int)MathF.Round(sizePx);

        if (this.fontEntries.TryGetValue((ident, sizeInt), out var wrapper))
            return wrapper;

        if (this.failedIdents.TryGetValue((ident, sizeInt), out var previousException))
            throw previousException;

        try
        {
            switch (ident)
            {
                case { Game: not GameFontFamily.Undefined }:
                {
                    var gfm = Service<GameFontManager>.Get();
                    var tm = Service<TextureManager>.Get();

                    var gfs = new GameFontStyle(new GameFontStyle(ident.Game, sizeInt).FamilyAndSize);
                    if ((int)MathF.Round(gfs.SizePx) == sizeInt)
                    {
                        const string filename = "font{}.tex";
                        var fdt = gfm.GetFdtReader(gfs.FamilyAndSize)
                                  ?? throw new FileNotFoundException($"{gfs} not found");
                        var numExpectedTex = fdt.Glyphs.Max(x => x.TextureFileIndex) + 1;
                        if (!this.gameFontTextures.TryGetValue(filename, out var textureIndices)
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

                                    newTextureWraps[i] = errorDispose.Add(tm.GetTexture(gfm.TexFiles[i]));
                                    newTextureIndices[i] = this.TextureWraps.Count + addCounter++;
                                }

                                this.ImTextures.EnsureCapacity(this.ImTextures.Length + addCounter);
                                this.TextureWraps.EnsureCapacity(this.TextureWraps.Count + addCounter);
                                errorDispose.Cancel();
                            }

                            this.gameFontTextures[filename] = textureIndices = newTextureIndices;

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

                        wrapper = new AxisOnDemandFont(this, fdt, textureIndices);
                    }
                    else
                    {
                        var baseFontIdent = this[ident, gfs.SizePx].NativePtr;
                        wrapper = new ScaledOnDemandFont(
                            this,
                            this.fontEntries.Single(x => x.Value.FontPtr.NativePtr == baseFontIdent).Value,
                            sizeInt / gfs.SizePx);
                    }

                    break;
                }

                case { System: { Name: { } name, Variant: { } variant } }:
                    wrapper = DirectWriteOnDemandFont.FromSystem(this, name, variant, sizeInt);
                    break;

                case { File: { Path: { } path, Index: { } index } }:
                    // TODO
                    throw new NotImplementedException();

                default:
                    throw new ArgumentException("Invalid identifier specification", nameof(ident));
            }

            this.fontEntries[(ident, sizeInt)] = wrapper;
            this.fontPtrToFont[(nint)wrapper.FontPtr.NativePtr] = wrapper;
            this.Fonts.Add(wrapper.FontPtr);
            wrapper.Font.ContainerAtlas = this.AtlasPtr;

            return wrapper;
        }
        catch (Exception e)
        {
            this.failedIdents[(ident, sizeInt)] = e;
            throw;
        }
    }

    /// <summary>
    /// Allocate a space for the given glyph.
    /// </summary>
    /// <param name="glyph">The glyph.</param>
    internal void AllocateGlyphSpace(ref ImGuiHelpers.ImFontGlyphReal glyph)
    {
        if (!glyph.Visible)
            return;

        foreach (var i in Enumerable.Range(0, this.TextureWraps.Count + 1))
        {
            FontChainAtlasTextureWrap wrap;
            if (i < this.TextureWraps.Count)
            {
                if (this.TextureWraps[i] is not FontChainAtlasTextureWrap w
                    || w.UseColor != glyph.Colored)
                    continue;
                wrap = w;
            }
            else
            {
                if (i == 256)
                    throw new OutOfMemoryException();

                wrap = new(
                    this.interfaceManager.Device!,
                    0,
                    this.AtlasPtr.TexWidth,
                    this.AtlasPtr.TexHeight,
                    glyph.Colored);
                this.ImTextures.Add(new() { TexID = wrap.ImGuiHandle });
                this.TextureWraps.Add(wrap);
            }

            for (var j = 0; j < wrap.Packers.Length; j++)
            {
                var packer = wrap.Packers[j];
                var rc = packer.PackRect((int)((glyph.X1 - glyph.X0) + 1), (int)((glyph.Y1 - glyph.Y0) + 1), null!);
                if (rc is null)
                    continue;

                glyph.TextureIndex = i;
                var du = glyph.Colored ? 0 : 1 + j;
                glyph.U0 = du + ((float)(rc.X + 1) / wrap.Width);
                glyph.U1 = du + ((float)(rc.X + rc.Width) / wrap.Width);
                glyph.V0 = (float)(rc.Y + 1) / wrap.Height;
                glyph.V1 = (float)(rc.Y + rc.Height) / wrap.Height;
                return;
            }
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

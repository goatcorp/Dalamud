using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

using Dalamud.Game.Text;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Data.Files;

using Vector4 = System.Numerics.Vector4;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// A font handle that uses the game's built-in fonts, optionally with some styling.
/// </summary>
internal class GamePrebakedFontHandle : IFontHandle.IInternal
{
    /// <summary>
    /// The smallest value of <see cref="SeIconChar"/>.
    /// </summary>
    public static readonly char SeIconCharMin = (char)Enum.GetValues<SeIconChar>().Min();

    /// <summary>
    /// The largest value of <see cref="SeIconChar"/>.
    /// </summary>
    public static readonly char SeIconCharMax = (char)Enum.GetValues<SeIconChar>().Max();

    private IFontHandleManager? manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePrebakedFontHandle"/> class.
    /// </summary>
    /// <param name="manager">An instance of <see cref="IFontHandleManager"/>.</param>
    /// <param name="style">Font to use.</param>
    public GamePrebakedFontHandle(IFontHandleManager manager, GameFontStyle style)
    {
        if (!Enum.IsDefined(style.FamilyAndSize) || style.FamilyAndSize == GameFontFamilyAndSize.Undefined)
            throw new ArgumentOutOfRangeException(nameof(style), style, null);

        if (style.SizePt <= 0)
            throw new ArgumentException($"{nameof(style.SizePt)} must be a positive number.", nameof(style));

        this.manager = manager;
        this.FontStyle = style;
    }

    /// <summary>
    /// Provider for <see cref="IDalamudTextureWrap"/> for `common/font/fontNN.tex`.
    /// </summary>
    public interface IGameFontTextureProvider
    {
        /// <summary>
        /// Creates the <see cref="FdtFileView"/> for the <see cref="GameFontFamilyAndSize"/>.<br />
        /// <strong>Dispose after use.</strong>
        /// </summary>
        /// <param name="gffas">The font family and size.</param>
        /// <param name="fdtFileView">The view.</param>
        /// <returns>Dispose this after use..</returns>
        public MemoryHandle CreateFdtFileView(GameFontFamilyAndSize gffas, out FdtFileView fdtFileView);

        /// <summary>
        /// Gets the number of font textures.
        /// </summary>
        /// <param name="texPathFormat">Format of .tex path.</param>
        /// <returns>The number of textures.</returns>
        public int GetFontTextureCount(string texPathFormat);

        /// <summary>
        /// Gets the <see cref="TexFile"/> for the given index of a font.
        /// </summary>
        /// <param name="texPathFormat">Format of .tex path.</param>
        /// <param name="index">The index of .tex file.</param>
        /// <returns>The <see cref="TexFile"/>.</returns>
        public TexFile GetTexFile(string texPathFormat, int index);

        /// <summary>
        /// Gets a new reference of the font texture.
        /// </summary>
        /// <param name="texPathFormat">Format of .tex path.</param>
        /// <param name="textureIndex">Texture index.</param>
        /// <returns>The texture.</returns>
        public IDalamudTextureWrap NewFontTextureRef(string texPathFormat, int textureIndex);
    }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    public GameFontStyle FontStyle { get; }

    /// <inheritdoc/>
    public Exception? LoadException => this.ManagerNotDisposed.Substance?.GetBuildException(this);

    /// <inheritdoc/>
    public bool Available => this.ImFont.IsNotNullAndLoaded();

    /// <inheritdoc/>
    public ImFontPtr ImFont => this.ManagerNotDisposed.Substance?.GetFontPtr(this) ?? default;

    private IFontHandleManager ManagerNotDisposed =>
        this.manager ?? throw new ObjectDisposedException(nameof(GamePrebakedFontHandle));

    /// <inheritdoc/>
    public void Dispose()
    {
        this.manager?.FreeFontHandle(this);
        this.manager = null;
    }

    /// <inheritdoc/>
    public IDisposable Push() => ImRaii.PushFont(this.ImFont, this.Available);

    /// <summary>
    /// Manager for <see cref="GamePrebakedFontHandle"/>s.
    /// </summary>
    internal sealed class HandleManager : IFontHandleManager
    {
        private readonly Dictionary<GameFontStyle, int> gameFontsRc = new();
        private readonly object syncRoot = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleManager"/> class.
        /// </summary>
        /// <param name="atlasName">The name of the owner atlas.</param>
        /// <param name="gameFontTextureProvider">An instance of <see cref="IGameFontTextureProvider"/>.</param>
        public HandleManager(string atlasName, IGameFontTextureProvider gameFontTextureProvider)
        {
            this.GameFontTextureProvider = gameFontTextureProvider;
            this.Name = $"{atlasName}:{nameof(GamePrebakedFontHandle)}:Manager";
        }

        /// <inheritdoc/>
        public event Action? RebuildRecommend;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public IFontHandleSubstance? Substance { get; set; }

        /// <summary>
        /// Gets an instance of <see cref="IGameFontTextureProvider"/>.
        /// </summary>
        public IGameFontTextureProvider GameFontTextureProvider { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Substance?.Dispose();
            this.Substance = null;
        }

        /// <inheritdoc cref="IFontAtlas.NewGameFontHandle"/>
        public IFontHandle NewFontHandle(GameFontStyle style)
        {
            var handle = new GamePrebakedFontHandle(this, style);
            bool suggestRebuild;
            lock (this.syncRoot)
            {
                this.gameFontsRc[style] = this.gameFontsRc.GetValueOrDefault(style, 0) + 1;
                suggestRebuild = this.Substance?.GetFontPtr(handle).IsNotNullAndLoaded() is not true;
            }

            if (suggestRebuild)
                this.RebuildRecommend?.Invoke();

            return handle;
        }

        /// <inheritdoc/>
        public void FreeFontHandle(IFontHandle handle)
        {
            if (handle is not GamePrebakedFontHandle ggfh)
                return;

            lock (this.syncRoot)
            {
                if (!this.gameFontsRc.ContainsKey(ggfh.FontStyle))
                    return;

                if ((this.gameFontsRc[ggfh.FontStyle] -= 1) == 0)
                    this.gameFontsRc.Remove(ggfh.FontStyle);
            }
        }

        /// <inheritdoc/>
        public IFontHandleSubstance NewSubstance()
        {
            lock (this.syncRoot)
                return new HandleSubstance(this, this.gameFontsRc.Keys);
        }
    }

    /// <summary>
    /// Substance from <see cref="HandleManager"/>.
    /// </summary>
    internal sealed class HandleSubstance : IFontHandleSubstance
    {
        private readonly HandleManager handleManager;
        private readonly HashSet<GameFontStyle> gameFontStyles;

        // Owned by this class, but ImFontPtr values still do not belong to this.
        private readonly Dictionary<GameFontStyle, ImFontPtr> fonts = new();
        private readonly Dictionary<GameFontStyle, Exception?> buildExceptions = new();
        private readonly Dictionary<ImFontPtr, List<(ImFontPtr Font, ushort[]? Ranges)>> fontCopyTargets = new();

        private readonly HashSet<ImFontPtr> templatedFonts = new();
        private readonly Dictionary<ImFontPtr, List<(char From, char To)>> lateBuildRanges = new();

        private readonly Dictionary<GameFontStyle, Dictionary<char, (int RectId, int FdtGlyphIndex)>> glyphRectIds =
            new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleSubstance"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="gameFontStyles">The game font styles.</param>
        public HandleSubstance(HandleManager manager, IEnumerable<GameFontStyle> gameFontStyles)
        {
            this.handleManager = manager;
            Service<InterfaceManager>.Get();
            this.gameFontStyles = new(gameFontStyles);
        }

        /// <inheritdoc/>
        public IFontHandleManager Manager => this.handleManager;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <summary>
        /// Attaches game symbols to the given font.
        /// </summary>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <param name="font">The font to attach to.</param>
        /// <param name="sizePx">The font size in pixels.</param>
        /// <param name="glyphRanges">The intended glyph ranges.</param>
        /// <returns><paramref name="font"/> if it is not empty; otherwise a new font.</returns>
        public ImFontPtr AttachGameSymbols(
            IFontAtlasBuildToolkitPreBuild toolkitPreBuild,
            ImFontPtr font,
            float sizePx,
            ushort[]? glyphRanges)
        {
            var style = new GameFontStyle(GameFontFamily.Axis, sizePx);
            var referenceFont = this.GetOrCreateFont(style, toolkitPreBuild);

            if (font.IsNull())
                font = this.CreateTemplateFont(style, toolkitPreBuild);

            if (!this.fontCopyTargets.TryGetValue(referenceFont, out var copyTargets))
                this.fontCopyTargets[referenceFont] = copyTargets = new();

            copyTargets.Add((font, glyphRanges));
            return font;
        }

        /// <summary>
        /// Creates or gets a relevant <see cref="ImFontPtr"/> for the given <see cref="GameFontStyle"/>.
        /// </summary>
        /// <param name="style">The game font style.</param>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <returns>The font.</returns>
        public ImFontPtr GetOrCreateFont(GameFontStyle style, IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            if (this.fonts.TryGetValue(style, out var font))
                return font;

            try
            {
                font = this.CreateFontPrivate(style, toolkitPreBuild, ' ', '\uFFFE', true);
                this.fonts.Add(style, font);
                return font;
            }
            catch (Exception e)
            {
                this.buildExceptions[style] = e;
                throw;
            }
        }

        /// <inheritdoc/>
        public ImFontPtr GetFontPtr(IFontHandle handle) =>
            handle is GamePrebakedFontHandle ggfh ? this.fonts.GetValueOrDefault(ggfh.FontStyle) : default;

        /// <inheritdoc/>
        public Exception? GetBuildException(IFontHandle handle) =>
            handle is GamePrebakedFontHandle ggfh ? this.buildExceptions.GetValueOrDefault(ggfh.FontStyle) : default;

        /// <inheritdoc/>
        public void OnPreBuild(IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            foreach (var style in this.gameFontStyles)
            {
                if (this.fonts.ContainsKey(style))
                    continue;

                try
                {
                    _ = this.GetOrCreateFont(style, toolkitPreBuild);
                }
                catch
                {
                    // ignore; it should have been recorded from the call
                }
            }
        }

        /// <inheritdoc/>
        public unsafe void OnPostBuild(IFontAtlasBuildToolkitPostBuild toolkitPostBuild)
        {
            var allTextureIndices = new Dictionary<string, int[]>();
            var allTexFiles = new Dictionary<string, TexFile[]>();
            using var rentReturn = Disposable.Create(
                () =>
                {
                    foreach (var x in allTextureIndices.Values)
                        ArrayPool<int>.Shared.Return(x);
                    foreach (var x in allTexFiles.Values)
                        ArrayPool<TexFile>.Shared.Return(x);
                });

            var pixels8Array = new byte*[toolkitPostBuild.NewImAtlas.Textures.Size];
            var widths = new int[toolkitPostBuild.NewImAtlas.Textures.Size];
            var heights = new int[toolkitPostBuild.NewImAtlas.Textures.Size];
            for (var i = 0; i < pixels8Array.Length; i++)
                toolkitPostBuild.NewImAtlas.GetTexDataAsAlpha8(i, out pixels8Array[i], out widths[i], out heights[i]);

            foreach (var (style, font) in this.fonts)
            {
                try
                {
                    var fas = GameFontStyle.GetRecommendedFamilyAndSize(
                        style.Family,
                        style.SizePt * toolkitPostBuild.Scale);
                    var attr = fas.GetAttribute<GameFontFamilyAndSizeAttribute>();
                    var horizontalOffset = attr?.HorizontalOffset ?? 0;
                    var texCount = this.handleManager.GameFontTextureProvider.GetFontTextureCount(attr.TexPathFormat);
                    using var handle = this.handleManager.GameFontTextureProvider.CreateFdtFileView(fas, out var fdt);
                    ref var fdtFontHeader = ref fdt.FontHeader;
                    var fdtGlyphs = fdt.Glyphs;
                    var fontPtr = font.NativePtr;

                    var glyphs = font.GlyphsWrapped();
                    var scale = toolkitPostBuild.Scale * (style.SizePt / fdtFontHeader.Size);

                    fontPtr->FontSize = toolkitPostBuild.Scale * style.SizePx;
                    if (fontPtr->ConfigData != null)
                        fontPtr->ConfigData->SizePixels = fontPtr->FontSize;
                    fontPtr->Ascent = fdtFontHeader.Ascent * scale;
                    fontPtr->Descent = fdtFontHeader.Descent * scale;
                    fontPtr->EllipsisChar = '…';

                    if (!allTexFiles.TryGetValue(attr.TexPathFormat, out var texFiles))
                        allTexFiles.Add(attr.TexPathFormat, texFiles = ArrayPool<TexFile>.Shared.Rent(texCount));
                    
                    if (this.glyphRectIds.TryGetValue(style, out var rectIdToGlyphs))
                    {
                        foreach (var (rectId, fdtGlyphIndex) in rectIdToGlyphs.Values)
                        {
                            ref var glyph = ref fdtGlyphs[fdtGlyphIndex];
                            var rc = (ImGuiHelpers.ImFontAtlasCustomRectReal*)toolkitPostBuild.NewImAtlas
                                .GetCustomRectByIndex(rectId)
                                .NativePtr;
                            var pixels8 = pixels8Array[rc->TextureIndex];
                            var width = widths[rc->TextureIndex];
                            texFiles[glyph.TextureFileIndex] ??=
                                this.handleManager
                                    .GameFontTextureProvider
                                    .GetTexFile(attr.TexPathFormat, glyph.TextureFileIndex);
                            var sourceBuffer = texFiles[glyph.TextureFileIndex].ImageData;
                            var sourceBufferDelta = glyph.TextureChannelByteIndex;
                            var widthAdjustment = style.CalculateBaseWidthAdjustment(fdtFontHeader, glyph);
                            if (widthAdjustment == 0)
                            {
                                for (var y = 0; y < glyph.BoundingHeight; y++)
                                {
                                    for (var x = 0; x < glyph.BoundingWidth; x++)
                                    {
                                        var a = sourceBuffer[
                                            sourceBufferDelta +
                                            (4 * (((glyph.TextureOffsetY + y) * fdtFontHeader.TextureWidth) +
                                                  glyph.TextureOffsetX + x))];
                                        pixels8[((rc->Y + y) * width) + rc->X + x] = a;
                                    }
                                }
                            }
                            else
                            {
                                for (var y = 0; y < glyph.BoundingHeight; y++)
                                {
                                    for (var x = 0; x < glyph.BoundingWidth + widthAdjustment; x++)
                                        pixels8[((rc->Y + y) * width) + rc->X + x] = 0;
                                }

                                for (int xbold = 0, xboldTo = Math.Max(1, (int)Math.Ceiling(style.Weight + 1));
                                     xbold < xboldTo;
                                     xbold++)
                                {
                                    var boldStrength = Math.Min(1f, style.Weight + 1 - xbold);
                                    for (var y = 0; y < glyph.BoundingHeight; y++)
                                    {
                                        float xDelta = xbold;
                                        if (style.BaseSkewStrength > 0)
                                        {
                                            xDelta += style.BaseSkewStrength *
                                                      (fdtFontHeader.LineHeight - glyph.CurrentOffsetY - y) /
                                                      fdtFontHeader.LineHeight;
                                        }
                                        else if (style.BaseSkewStrength < 0)
                                        {
                                            xDelta -= style.BaseSkewStrength * (glyph.CurrentOffsetY + y) /
                                                      fdtFontHeader.LineHeight;
                                        }

                                        var xDeltaInt = (int)Math.Floor(xDelta);
                                        var xness = xDelta - xDeltaInt;
                                        for (var x = 0; x < glyph.BoundingWidth; x++)
                                        {
                                            var sourcePixelIndex =
                                                ((glyph.TextureOffsetY + y) * fdtFontHeader.TextureWidth) +
                                                glyph.TextureOffsetX + x;
                                            var a1 = sourceBuffer[sourceBufferDelta + (4 * sourcePixelIndex)];
                                            var a2 = x == glyph.BoundingWidth - 1
                                                         ? 0
                                                         : sourceBuffer[sourceBufferDelta
                                                                        + (4 * (sourcePixelIndex + 1))];
                                            var n = (a1 * xness) + (a2 * (1 - xness));
                                            var targetOffset = ((rc->Y + y) * width) + rc->X + x + xDeltaInt;
                                            pixels8[targetOffset] =
                                                Math.Max(pixels8[targetOffset], (byte)(boldStrength * n));
                                        }
                                    }
                                }
                            }

                            glyphs[rc->GlyphId].XY *= scale;
                            glyphs[rc->GlyphId].AdvanceX *= scale;
                        }
                    }
                    else if (this.lateBuildRanges.TryGetValue(font, out var buildRanges))
                    {
                        buildRanges.Sort();
                        for (var i = 0; i < buildRanges.Count; i++)
                        {
                            var current = buildRanges[i];
                            if (current.From > current.To)
                                buildRanges[i] = (From: current.To, To: current.From);
                        }

                        for (var i = 0; i < buildRanges.Count - 1; i++)
                        {
                            var current = buildRanges[i];
                            var next = buildRanges[i + 1];
                            if (next.From <= current.To)
                            {
                                buildRanges[i] = current with { To = next.To };
                                buildRanges.RemoveAt(i + 1);
                                i--;
                            }
                        }

                        var fdtTexSize = new Vector4(
                            fdtFontHeader.TextureWidth,
                            fdtFontHeader.TextureHeight,
                            fdtFontHeader.TextureWidth,
                            fdtFontHeader.TextureHeight);

                        if (!allTextureIndices.TryGetValue(attr.TexPathFormat, out var textureIndices))
                        {
                            allTextureIndices.Add(
                                attr.TexPathFormat,
                                textureIndices = ArrayPool<int>.Shared.Rent(texCount));
                            textureIndices.AsSpan(0, texCount).Fill(-1);
                        }

                        glyphs.EnsureCapacity(glyphs.Length + buildRanges.Sum(x => (x.To - x.From) + 1));
                        foreach (var (rangeMin, rangeMax) in buildRanges)
                        {
                            var glyphIndex = fdt.FindGlyphIndex(rangeMin);
                            if (glyphIndex < 0)
                                glyphIndex = ~glyphIndex;
                            var endIndex = fdt.FindGlyphIndex(rangeMax);
                            if (endIndex < 0)
                                endIndex = ~endIndex - 1;
                            for (; glyphIndex <= endIndex; glyphIndex++)
                            {
                                var fdtg = fdtGlyphs[glyphIndex];

                                // If the glyph already exists in the target font, we do not overwrite.
                                if (
                                    !(fdtg.Char == ' ' && this.templatedFonts.Contains(font))
                                    && font.FindGlyphNoFallback(fdtg.Char).NativePtr is not null)
                                {
                                    continue;
                                }

                                ref var textureIndex = ref textureIndices[fdtg.TextureIndex];
                                if (textureIndex == -1)
                                {
                                    textureIndex = toolkitPostBuild.StoreTexture(
                                        this.handleManager
                                            .GameFontTextureProvider
                                            .NewFontTextureRef(attr.TexPathFormat, fdtg.TextureIndex),
                                        true);
                                }

                                var glyph = new ImGuiHelpers.ImFontGlyphReal
                                {
                                    AdvanceX = fdtg.AdvanceWidth,
                                    Codepoint = fdtg.Char,
                                    Colored = false,
                                    TextureIndex = textureIndex,
                                    Visible = true,
                                    X0 = horizontalOffset,
                                    Y0 = fdtg.CurrentOffsetY,
                                    U0 = fdtg.TextureOffsetX,
                                    V0 = fdtg.TextureOffsetY,
                                    U1 = fdtg.BoundingWidth,
                                    V1 = fdtg.BoundingHeight,
                                };

                                glyph.XY1 = glyph.XY0 + glyph.UV1;
                                glyph.UV1 += glyph.UV0;
                                glyph.UV /= fdtTexSize;
                                glyph.XY *= scale;
                                glyph.AdvanceX *= scale;

                                glyphs.Add(glyph);
                            }
                        }

                        font.NativePtr->FallbackGlyph = null;

                        font.BuildLookupTable();
                    }

                    foreach (var fallbackCharCandidate in FontAtlasFactory.FallbackCodepoints)
                    {
                        var glyph = font.FindGlyphNoFallback(fallbackCharCandidate);
                        if ((IntPtr)glyph.NativePtr != IntPtr.Zero)
                        {
                            var ptr = font.NativePtr;
                            ptr->FallbackChar = fallbackCharCandidate;
                            ptr->FallbackGlyph = glyph.NativePtr;
                            ptr->FallbackHotData =
                                (ImFontGlyphHotData*)ptr->IndexedHotData.Address<ImGuiHelpers.ImFontGlyphHotDataReal>(
                                    fallbackCharCandidate);
                            break;
                        }
                    }

                    font.AdjustGlyphMetrics(1 / toolkitPostBuild.Scale, toolkitPostBuild.Scale);
                }
                catch (Exception e)
                {
                    this.buildExceptions[style] = e;
                    this.fonts[style] = default;
                }
            }

            foreach (var (source, targets) in this.fontCopyTargets)
            {
                foreach (var target in targets)
                {
                    if (target.Ranges is null)
                    {
                        ImGuiHelpers.CopyGlyphsAcrossFonts(source, target.Font, missingOnly: true);
                    }
                    else
                    {
                        for (var i = 0; i < target.Ranges.Length; i += 2)
                        {
                            if (target.Ranges[i] == 0)
                                break;
                            ImGuiHelpers.CopyGlyphsAcrossFonts(
                                source,
                                target.Font,
                                true,
                                true,
                                target.Ranges[i],
                                target.Ranges[i + 1]);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnPostPromotion(IFontAtlasBuildToolkitPostPromotion toolkitPostPromotion)
        {
            // Irrelevant
        }

        /// <summary>
        /// Creates a relevant <see cref="ImFontPtr"/> for the given <see cref="GameFontStyle"/>.
        /// </summary>
        /// <param name="style">The game font style.</param>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <param name="minRange">Min range.</param>
        /// <param name="maxRange">Max range.</param>
        /// <param name="addExtraLanguageGlyphs">Add extra language glyphs.</param>
        /// <returns>The font.</returns>
        private ImFontPtr CreateFontPrivate(
            GameFontStyle style,
            IFontAtlasBuildToolkitPreBuild toolkitPreBuild,
            char minRange,
            char maxRange,
            bool addExtraLanguageGlyphs)
        {
            var font = toolkitPreBuild.IgnoreGlobalScale(this.CreateTemplateFont(style, toolkitPreBuild));

            if (addExtraLanguageGlyphs)
            {
                var cfg = toolkitPreBuild.FindConfigPtr(font);
                toolkitPreBuild.AddExtraGlyphsForDalamudLanguage(new()
                {
                    MergeFont = cfg.DstFont,
                    SizePx = cfg.SizePixels,
                });
            }

            var fas = GameFontStyle.GetRecommendedFamilyAndSize(style.Family, style.SizePt * toolkitPreBuild.Scale);
            var horizontalOffset = fas.GetAttribute<GameFontFamilyAndSizeAttribute>()?.HorizontalOffset ?? 0;
            using var handle = this.handleManager.GameFontTextureProvider.CreateFdtFileView(fas, out var fdt);
            ref var fdtFontHeader = ref fdt.FontHeader;
            var existing = new SortedSet<char>();

            if (style is { Bold: false, Italic: false })
            {
                if (!this.lateBuildRanges.TryGetValue(font, out var ranges))
                    this.lateBuildRanges[font] = ranges = new();

                ranges.Add((minRange, maxRange));
            }
            else
            {
                if (this.glyphRectIds.TryGetValue(style, out var rectIds))
                    existing.UnionWith(rectIds.Keys);
                else
                    rectIds = this.glyphRectIds[style] = new();

                var glyphs = fdt.Glyphs;
                for (var fdtGlyphIndex = 0; fdtGlyphIndex < glyphs.Length; fdtGlyphIndex++)
                {
                    ref var glyph = ref glyphs[fdtGlyphIndex];
                    var cint = glyph.CharInt;
                    if (cint < minRange || cint > maxRange)
                        continue;

                    var c = (char)cint;
                    if (existing.Contains(c))
                        continue;

                    var widthAdjustment = style.CalculateBaseWidthAdjustment(fdtFontHeader, glyph);
                    rectIds[c] = (
                                     toolkitPreBuild.NewImAtlas.AddCustomRectFontGlyph(
                                         font,
                                         c,
                                         glyph.BoundingWidth + widthAdjustment,
                                         glyph.BoundingHeight,
                                         glyph.AdvanceWidth,
                                         new(horizontalOffset, glyph.CurrentOffsetY)),
                                     fdtGlyphIndex);
                }
            }
            
            var scale = toolkitPreBuild.Scale * (style.SizePt / fdt.FontHeader.Size);
            foreach (ref var kernPair in fdt.PairAdjustments)
                font.AddKerningPair(kernPair.Left, kernPair.Right, kernPair.RightOffset * scale);

            return font;
        }

        /// <summary>
        /// Creates a new template font.
        /// </summary>
        /// <param name="style">The game font style.</param>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <returns>The font.</returns>
        private ImFontPtr CreateTemplateFont(GameFontStyle style, IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            var font = toolkitPreBuild.AddDalamudAssetFont(
                DalamudAsset.NotoSansJpMedium,
                new()
                {
                    GlyphRanges = new ushort[] { ' ', ' ', '\0' },
                    SizePx = style.SizePx * toolkitPreBuild.Scale,
                });
            this.templatedFonts.Add(font);
            return font;
        }
    }
}

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;

using Dalamud.Game.Text;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Data.Files;

using Vector4 = System.Numerics.Vector4;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// A font handle that uses the game's built-in fonts, optionally with some styling.
/// </summary>
internal class GamePrebakedFontHandle : FontHandle
{
    /// <summary>
    /// The smallest value of <see cref="SeIconChar"/>.
    /// </summary>
    public static readonly char SeIconCharMin = (char)Enum.GetValues<SeIconChar>().Min();

    /// <summary>
    /// The largest value of <see cref="SeIconChar"/>.
    /// </summary>
    public static readonly char SeIconCharMax = (char)Enum.GetValues<SeIconChar>().Max();

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePrebakedFontHandle"/> class.
    /// </summary>
    /// <param name="manager">An instance of <see cref="IFontHandleManager"/>.</param>
    /// <param name="style">Font to use.</param>
    public GamePrebakedFontHandle(IFontHandleManager manager, GameFontStyle style)
        : base(manager)
    {
        if (!Enum.IsDefined(style.FamilyAndSize) || style.FamilyAndSize == GameFontFamilyAndSize.Undefined)
            throw new ArgumentOutOfRangeException(nameof(style), style, null);

        if (style.SizePt <= 0)
            throw new ArgumentException($"{nameof(style.SizePt)} must be a positive number.", nameof(style));

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
    public override string ToString() => $"{nameof(GamePrebakedFontHandle)}({this.FontStyle})";

    /// <summary>
    /// Manager for <see cref="GamePrebakedFontHandle"/>s.
    /// </summary>
    internal sealed class HandleManager : IFontHandleManager
    {
        private readonly Dictionary<GameFontStyle, int> gameFontsRc = new();
        private readonly HashSet<GamePrebakedFontHandle> handles = new();
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
            // empty
        }

        /// <inheritdoc cref="IFontAtlas.NewGameFontHandle"/>
        public IFontHandle NewFontHandle(GameFontStyle style)
        {
            var handle = new GamePrebakedFontHandle(this, style);
            bool suggestRebuild;
            lock (this.syncRoot)
            {
                this.handles.Add(handle);
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
                this.handles.Remove(ggfh);
                if (!this.gameFontsRc.ContainsKey(ggfh.FontStyle))
                    return;

                if ((this.gameFontsRc[ggfh.FontStyle] -= 1) == 0)
                    this.gameFontsRc.Remove(ggfh.FontStyle);
            }
        }

        /// <inheritdoc/>
        public IFontHandleSubstance NewSubstance(IRefCountable dataRoot)
        {
            lock (this.syncRoot)
                return new HandleSubstance(this, dataRoot, this.handles.ToArray(), this.gameFontsRc.Keys);
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
        private readonly Dictionary<GameFontStyle, FontDrawPlan> fonts = new();
        private readonly Dictionary<GameFontStyle, Exception?> buildExceptions = new();
        private readonly List<(ImFontPtr Font, GameFontStyle Style, ushort[]? Ranges)> attachments = new();

        private readonly HashSet<ImFontPtr> templatedFonts = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleSubstance"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="dataRoot">The data root.</param>
        /// <param name="relevantHandles">The relevant handles.</param>
        /// <param name="gameFontStyles">The game font styles.</param>
        public HandleSubstance(
            HandleManager manager,
            IRefCountable dataRoot,
            GamePrebakedFontHandle[] relevantHandles,
            IEnumerable<GameFontStyle> gameFontStyles)
        {
            // We do not call dataRoot.AddRef; this object is dependant on lifetime of dataRoot.

            this.handleManager = manager;
            this.DataRoot = dataRoot;
            this.RelevantHandles = relevantHandles;
            this.gameFontStyles = new(gameFontStyles);
        }

        /// <summary>
        /// Gets the relevant handles.
        /// </summary>
        // Not owned by this class. Do not dispose.
        public GamePrebakedFontHandle[] RelevantHandles { get; }

        /// <inheritdoc/>
        ICollection<FontHandle> IFontHandleSubstance.RelevantHandles => this.RelevantHandles;

        /// <inheritdoc/>
        public IRefCountable DataRoot { get; }

        /// <inheritdoc/>
        public IFontHandleManager Manager => this.handleManager;

        /// <inheritdoc/>
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public IFontAtlasBuildToolkitPreBuild? PreBuildToolkitForApi9Compat { get; set; }

        /// <inheritdoc/>
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public bool CreateFontOnAccess { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            // empty
        }

        /// <summary>
        /// Attaches game symbols to the given font. If font is null, it will be created.
        /// </summary>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <param name="font">The font to attach to.</param>
        /// <param name="style">The game font style.</param>
        /// <param name="glyphRanges">The intended glyph ranges.</param>
        /// <returns><paramref name="font"/> if it is not empty; otherwise a new font.</returns>
        public ImFontPtr AttachGameGlyphs(
            IFontAtlasBuildToolkitPreBuild toolkitPreBuild,
            ImFontPtr font,
            GameFontStyle style,
            ushort[]? glyphRanges = null)
        {
            if (font.IsNull())
                font = this.CreateTemplateFont(toolkitPreBuild, style.SizePx);
            this.attachments.Add((font, style, glyphRanges));
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
            try
            {
                if (!this.fonts.TryGetValue(style, out var plan))
                {
                    plan = new(
                        style,
                        toolkitPreBuild.Scale,
                        this.handleManager.GameFontTextureProvider,
                        this.CreateTemplateFont(toolkitPreBuild, style.SizePx));
                    this.fonts[style] = plan;
                }

                plan.AttachFont(plan.FullRangeFont);
                return plan.FullRangeFont;
            }
            catch (Exception e)
            {
                this.buildExceptions[style] = e;
                throw;
            }
        }

        // Use this on API 10.
        // /// <inheritdoc/>
        // public ImFontPtr GetFontPtr(IFontHandle handle) =>
        //     handle is GamePrebakedFontHandle ggfh
        //         ? this.fonts.GetValueOrDefault(ggfh.FontStyle)?.FullRangeFont ?? default
        //         : default;

        /// <inheritdoc/>
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public ImFontPtr GetFontPtr(IFontHandle handle)
        {
            if (handle is not GamePrebakedFontHandle ggfh)
                return default;
            if (this.fonts.GetValueOrDefault(ggfh.FontStyle)?.FullRangeFont is { } font)
                return font;
            if (!this.CreateFontOnAccess)
                return default;
            if (this.PreBuildToolkitForApi9Compat is not { } tk)
                return default;
            return this.GetOrCreateFont(ggfh.FontStyle, tk);
        }

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
        public void OnPreBuildCleanup(IFontAtlasBuildToolkitPreBuild toolkitPreBuild)
        {
            foreach (var (font, style, ranges) in this.attachments)
            {
                if (!this.fonts.TryGetValue(style, out var plan))
                {
                    switch (toolkitPreBuild.GetFontScaleMode(font))
                    {
                        case FontScaleMode.Default:
                        default:
                            plan = new(
                                style,
                                toolkitPreBuild.Scale,
                                this.handleManager.GameFontTextureProvider,
                                this.CreateTemplateFont(toolkitPreBuild, style.SizePx));
                            break;

                        case FontScaleMode.SkipHandling:
                            plan = new(
                                style,
                                1f,
                                this.handleManager.GameFontTextureProvider,
                                this.CreateTemplateFont(toolkitPreBuild, style.SizePx));
                            break;

                        case FontScaleMode.UndoGlobalScale:
                            plan = new(
                                style.Scale(1 / toolkitPreBuild.Scale),
                                toolkitPreBuild.Scale,
                                this.handleManager.GameFontTextureProvider,
                                this.CreateTemplateFont(toolkitPreBuild, style.SizePx));
                            break;
                    }

                    this.fonts[style] = plan;
                }

                plan.AttachFont(font, ranges);
            }

            foreach (var plan in this.fonts.Values)
            {
                plan.EnsureGlyphs(toolkitPreBuild.NewImAtlas);
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
            for (var i = 0; i < pixels8Array.Length; i++)
                toolkitPostBuild.NewImAtlas.GetTexDataAsAlpha8(i, out pixels8Array[i], out widths[i], out _);

            foreach (var (style, plan) in this.fonts)
            {
                try
                {
                    foreach (var font in plan.Ranges.Keys)
                        this.PatchFontMetricsIfNecessary(style, font, toolkitPostBuild.Scale);

                    plan.SetFullRangeFontGlyphs(toolkitPostBuild, allTexFiles, allTextureIndices, pixels8Array, widths);
                    plan.CopyGlyphsToRanges(toolkitPostBuild);
                    plan.PostProcessFullRangeFont(toolkitPostBuild.Scale);
                }
                catch (Exception e)
                {
                    this.buildExceptions[style] = e;
                    this.fonts[style] = default;
                }
            }
        }

        /// <summary>
        /// Creates a new template font.
        /// </summary>
        /// <param name="toolkitPreBuild">The toolkitPostBuild.</param>
        /// <param name="sizePx">The size of the font.</param>
        /// <returns>The font.</returns>
        private ImFontPtr CreateTemplateFont(IFontAtlasBuildToolkitPreBuild toolkitPreBuild, float sizePx)
        {
            var font = toolkitPreBuild.AddDalamudAssetFont(
                DalamudAsset.NotoSansJpMedium,
                new()
                {
                    GlyphRanges = new ushort[] { ' ', ' ', '\0' },
                    SizePx = sizePx,
                });
            this.templatedFonts.Add(font);
            return font;
        }

        private unsafe void PatchFontMetricsIfNecessary(GameFontStyle style, ImFontPtr font, float atlasScale)
        {
            if (!this.templatedFonts.Contains(font))
                return;

            var fas = style.Scale(atlasScale).FamilyAndSize;
            using var handle = this.handleManager.GameFontTextureProvider.CreateFdtFileView(fas, out var fdt);
            ref var fdtFontHeader = ref fdt.FontHeader;
            var fontPtr = font.NativePtr;

            var scale = style.SizePt / fdtFontHeader.Size;
            fontPtr->Ascent = fdtFontHeader.Ascent * scale;
            fontPtr->Descent = fdtFontHeader.Descent * scale;
            fontPtr->EllipsisChar = '…';
        }
    }

    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1401:Fields should be private",
        Justification = "Internal")]
    private sealed class FontDrawPlan : IDisposable
    {
        public readonly GameFontStyle Style;
        public readonly GameFontStyle BaseStyle;
        public readonly GameFontFamilyAndSizeAttribute BaseAttr;
        public readonly int TexCount;
        public readonly Dictionary<ImFontPtr, BitArray> Ranges = new();
        public readonly List<(int RectId, int FdtGlyphIndex)> Rects = new();
        public readonly ushort[] RectLookup = new ushort[0x10000];
        public readonly FdtFileView Fdt;
        public readonly ImFontPtr FullRangeFont;

        private readonly IDisposable fdtHandle;
        private readonly IGameFontTextureProvider gftp;

        public FontDrawPlan(
            GameFontStyle style,
            float scale,
            IGameFontTextureProvider gameFontTextureProvider,
            ImFontPtr fullRangeFont)
        {
            this.Style = style;
            this.BaseStyle = style.Scale(scale);
            this.BaseAttr = this.BaseStyle.FamilyAndSize.GetAttribute<GameFontFamilyAndSizeAttribute>()!;
            this.gftp = gameFontTextureProvider;
            this.TexCount = this.gftp.GetFontTextureCount(this.BaseAttr.TexPathFormat);
            this.fdtHandle = this.gftp.CreateFdtFileView(this.BaseStyle.FamilyAndSize, out this.Fdt);
            this.RectLookup.AsSpan().Fill(ushort.MaxValue);
            this.FullRangeFont = fullRangeFont;
            this.Ranges[fullRangeFont] = new(0x10000);
        }

        public void Dispose()
        {
            this.fdtHandle.Dispose();
        }

        public void AttachFont(ImFontPtr font, ushort[]? glyphRanges = null)
        {
            if (!this.Ranges.TryGetValue(font, out var rangeBitArray))
                rangeBitArray = this.Ranges[font] = new(0x10000);

            if (glyphRanges is null)
            {
                foreach (ref var g in this.Fdt.Glyphs)
                {
                    var c = g.CharInt;
                    if (c is >= 0x20 and <= 0xFFFE)
                        rangeBitArray[c] = true;
                }

                return;
            }

            for (var i = 0; i < glyphRanges.Length - 1; i += 2)
            {
                if (glyphRanges[i] == 0)
                    break;
                var from = (int)glyphRanges[i];
                var to = (int)glyphRanges[i + 1];
                for (var j = from; j <= to; j++)
                    rangeBitArray[j] = true;
            }
        }

        public unsafe void EnsureGlyphs(ImFontAtlasPtr atlas)
        {
            var glyphs = this.Fdt.Glyphs;
            var ranges = this.Ranges[this.FullRangeFont];
            foreach (var (font, extraRange) in this.Ranges)
            {
                if (font.NativePtr != this.FullRangeFont.NativePtr)
                    ranges.Or(extraRange);
            }

            if (this.Style is not { Weight: 0, SkewStrength: 0 })
            {
                for (var fdtGlyphIndex = 0; fdtGlyphIndex < glyphs.Length; fdtGlyphIndex++)
                {
                    ref var glyph = ref glyphs[fdtGlyphIndex];
                    var cint = glyph.CharInt;
                    if (cint > char.MaxValue)
                        continue;
                    if (!ranges[cint] || this.RectLookup[cint] != ushort.MaxValue)
                        continue;

                    var widthAdjustment = this.BaseStyle.CalculateBaseWidthAdjustment(this.Fdt.FontHeader, glyph);
                    this.RectLookup[cint] = (ushort)this.Rects.Count;
                    this.Rects.Add(
                        (
                            atlas.AddCustomRectFontGlyph(
                                this.FullRangeFont,
                                (char)cint,
                                glyph.BoundingWidth + widthAdjustment,
                                glyph.BoundingHeight,
                                glyph.AdvanceWidth,
                                new(this.BaseAttr.HorizontalOffset, glyph.CurrentOffsetY)),
                            fdtGlyphIndex));
                }
            }
            else
            {
                for (var fdtGlyphIndex = 0; fdtGlyphIndex < glyphs.Length; fdtGlyphIndex++)
                {
                    ref var glyph = ref glyphs[fdtGlyphIndex];
                    var cint = glyph.CharInt;
                    if (cint > char.MaxValue)
                        continue;
                    if (!ranges[cint] || this.RectLookup[cint] != ushort.MaxValue)
                        continue;

                    this.RectLookup[cint] = (ushort)this.Rects.Count;
                    this.Rects.Add((-1, fdtGlyphIndex));
                }
            }
        }

        public unsafe void PostProcessFullRangeFont(float atlasScale)
        {
            var round = 1 / atlasScale;
            var pfrf = this.FullRangeFont.NativePtr;
            ref var frf = ref *pfrf;

            frf.FontSize = MathF.Round(frf.FontSize / round) * round;
            frf.Ascent = MathF.Round(frf.Ascent / round) * round;
            frf.Descent = MathF.Round(frf.Descent / round) * round;

            var scale = this.Style.SizePt / this.Fdt.FontHeader.Size;
            foreach (ref var g in this.FullRangeFont.GlyphsWrapped().DataSpan)
            {
                var w = (g.X1 - g.X0) * scale;
                var h = (g.Y1 - g.Y0) * scale;
                g.X0 = MathF.Round((g.X0 * scale) / round) * round;
                g.Y0 = MathF.Round((g.Y0 * scale) / round) * round;
                g.X1 = g.X0 + w;
                g.Y1 = g.Y0 + h;
                g.AdvanceX = MathF.Round((g.AdvanceX * scale) / round) * round;
            }

            var fullRange = this.Ranges[this.FullRangeFont];
            foreach (ref var k in this.Fdt.PairAdjustments)
            {
                var (leftInt, rightInt) = (k.LeftInt, k.RightInt);
                if (leftInt > char.MaxValue || rightInt > char.MaxValue)
                    continue;
                if (!fullRange[leftInt] || !fullRange[rightInt])
                    continue;
                ImGuiNative.ImFont_AddKerningPair(
                    pfrf,
                    (ushort)leftInt,
                    (ushort)rightInt,
                    MathF.Round((k.RightOffset * scale) / round) * round);
            }

            pfrf->FallbackGlyph = null;
            ImGuiNative.ImFont_BuildLookupTable(pfrf);

            foreach (var fallbackCharCandidate in FontAtlasFactory.FallbackCodepoints)
            {
                var glyph = ImGuiNative.ImFont_FindGlyphNoFallback(pfrf, fallbackCharCandidate);
                if ((nint)glyph == IntPtr.Zero)
                    continue;
                frf.FallbackChar = fallbackCharCandidate;
                frf.FallbackGlyph = glyph;
                frf.FallbackHotData =
                    (ImFontGlyphHotData*)frf.IndexedHotData.Address<ImGuiHelpers.ImFontGlyphHotDataReal>(
                        fallbackCharCandidate);
                break;
            }
        }

        public unsafe void CopyGlyphsToRanges(IFontAtlasBuildToolkitPostBuild toolkitPostBuild)
        {
            var scale = this.Style.SizePt / this.Fdt.FontHeader.Size;

            foreach (var (font, rangeBits) in this.Ranges)
            {
                if (font.NativePtr == this.FullRangeFont.NativePtr)
                    continue;

                var fontScaleMode = toolkitPostBuild.GetFontScaleMode(font);
                var round = fontScaleMode == FontScaleMode.SkipHandling ? 1 : 1 / toolkitPostBuild.Scale;

                var lookup = font.IndexLookupWrapped();
                var glyphs = font.GlyphsWrapped();
                foreach (ref var sourceGlyph in this.FullRangeFont.GlyphsWrapped().DataSpan)
                {
                    if (!rangeBits[sourceGlyph.Codepoint])
                        continue;

                    var glyphIndex = ushort.MaxValue;
                    if (sourceGlyph.Codepoint < lookup.Length)
                        glyphIndex = lookup[sourceGlyph.Codepoint];

                    if (glyphIndex == ushort.MaxValue)
                    {
                        glyphIndex = (ushort)glyphs.Length;
                        glyphs.Add(default);
                    }
                    
                    ref var g = ref glyphs[glyphIndex];
                    g = sourceGlyph;
                    if (fontScaleMode == FontScaleMode.SkipHandling)
                    {
                        g.XY *= scale;
                        g.AdvanceX *= scale;
                    }
                    else
                    {
                        var w = (g.X1 - g.X0) * scale;
                        var h = (g.Y1 - g.Y0) * scale;
                        g.X0 = MathF.Round((g.X0 * scale) / round) * round;
                        g.Y0 = MathF.Round((g.Y0 * scale) / round) * round;
                        g.X1 = g.X0 + w;
                        g.Y1 = g.Y0 + h;
                        g.AdvanceX = MathF.Round((g.AdvanceX * scale) / round) * round;
                    }
                }

                foreach (ref var k in this.Fdt.PairAdjustments)
                {
                    var (leftInt, rightInt) = (k.LeftInt, k.RightInt);
                    if (leftInt > char.MaxValue || rightInt > char.MaxValue)
                        continue;
                    if (!rangeBits[leftInt] || !rangeBits[rightInt])
                        continue;
                    if (fontScaleMode == FontScaleMode.SkipHandling)
                    {
                        font.AddKerningPair((ushort)leftInt, (ushort)rightInt, k.RightOffset * scale);
                    }
                    else
                    {
                        font.AddKerningPair(
                            (ushort)leftInt,
                            (ushort)rightInt,
                            MathF.Round((k.RightOffset * scale) / round) * round);
                    }
                }

                font.NativePtr->FallbackGlyph = null;
                font.BuildLookupTable();

                foreach (var fallbackCharCandidate in FontAtlasFactory.FallbackCodepoints)
                {
                    var glyph = font.FindGlyphNoFallback(fallbackCharCandidate).NativePtr;
                    if ((nint)glyph == IntPtr.Zero)
                        continue;

                    ref var frf = ref *font.NativePtr;
                    frf.FallbackChar = fallbackCharCandidate;
                    frf.FallbackGlyph = glyph;
                    frf.FallbackHotData =
                        (ImFontGlyphHotData*)frf.IndexedHotData.Address<ImGuiHelpers.ImFontGlyphHotDataReal>(
                            fallbackCharCandidate);
                    break;
                }
            }
        }

        public unsafe void SetFullRangeFontGlyphs(
            IFontAtlasBuildToolkitPostBuild toolkitPostBuild,
            Dictionary<string, TexFile[]> allTexFiles,
            Dictionary<string, int[]> allTextureIndices,
            byte*[] pixels8Array,
            int[] widths)
        {
            var glyphs = this.FullRangeFont.GlyphsWrapped();
            var lookups = this.FullRangeFont.IndexLookupWrapped();

            ref var fdtFontHeader = ref this.Fdt.FontHeader;
            var fdtGlyphs = this.Fdt.Glyphs;
            var fdtTexSize = new Vector4(
                this.Fdt.FontHeader.TextureWidth,
                this.Fdt.FontHeader.TextureHeight,
                this.Fdt.FontHeader.TextureWidth,
                this.Fdt.FontHeader.TextureHeight);

            if (!allTexFiles.TryGetValue(this.BaseAttr.TexPathFormat, out var texFiles))
            {
                allTexFiles.Add(
                    this.BaseAttr.TexPathFormat,
                    texFiles = ArrayPool<TexFile>.Shared.Rent(this.TexCount));
            }

            if (!allTextureIndices.TryGetValue(this.BaseAttr.TexPathFormat, out var textureIndices))
            {
                allTextureIndices.Add(
                    this.BaseAttr.TexPathFormat,
                    textureIndices = ArrayPool<int>.Shared.Rent(this.TexCount));
                textureIndices.AsSpan(0, this.TexCount).Fill(-1);
            }

            var pixelWidth = Math.Max(1, (int)MathF.Ceiling(this.BaseStyle.Weight + 1));
            var pixelStrength = stackalloc byte[pixelWidth];
            for (var i = 0; i < pixelWidth; i++)
                pixelStrength[i] = (byte)(255 * Math.Min(1f, (this.BaseStyle.Weight + 1) - i));

            var minGlyphY = 0;
            var maxGlyphY = 0;
            foreach (ref var g in fdtGlyphs)
            {
                minGlyphY = Math.Min(g.CurrentOffsetY, minGlyphY);
                maxGlyphY = Math.Max(g.BoundingHeight + g.CurrentOffsetY, maxGlyphY);
            }

            var horzShift = stackalloc int[maxGlyphY - minGlyphY];
            var horzBlend = stackalloc byte[maxGlyphY - minGlyphY];
            horzShift -= minGlyphY;
            horzBlend -= minGlyphY;
            if (this.BaseStyle.BaseSkewStrength != 0)
            {
                for (var i = minGlyphY; i < maxGlyphY; i++)
                {
                    float blend = this.BaseStyle.BaseSkewStrength switch
                    {
                        > 0 => fdtFontHeader.LineHeight - i,
                        < 0 => -i,
                        _ => throw new InvalidOperationException(),
                    };
                    blend *= this.BaseStyle.BaseSkewStrength / fdtFontHeader.LineHeight;
                    horzShift[i] = (int)MathF.Floor(blend);
                    horzBlend[i] = (byte)(255 * (blend - horzShift[i]));
                }
            }

            foreach (var (rectId, fdtGlyphIndex) in this.Rects)
            {
                ref var fdtGlyph = ref fdtGlyphs[fdtGlyphIndex];
                if (rectId == -1)
                {
                    ref var textureIndex = ref textureIndices[fdtGlyph.TextureIndex];
                    if (textureIndex == -1)
                    {
                        textureIndex = toolkitPostBuild.StoreTexture(
                            this.gftp.NewFontTextureRef(this.BaseAttr.TexPathFormat, fdtGlyph.TextureIndex),
                            true);
                    }

                    var glyph = new ImGuiHelpers.ImFontGlyphReal
                    {
                        AdvanceX = fdtGlyph.AdvanceWidth,
                        Codepoint = fdtGlyph.Char,
                        Colored = false,
                        TextureIndex = textureIndex,
                        Visible = true,
                        X0 = this.BaseAttr.HorizontalOffset,
                        Y0 = fdtGlyph.CurrentOffsetY,
                        U0 = fdtGlyph.TextureOffsetX,
                        V0 = fdtGlyph.TextureOffsetY,
                        U1 = fdtGlyph.BoundingWidth,
                        V1 = fdtGlyph.BoundingHeight,
                    };

                    glyph.XY1 = glyph.XY0 + glyph.UV1;
                    glyph.UV1 += glyph.UV0;
                    glyph.UV /= fdtTexSize;

                    glyphs.Add(glyph);
                }
                else
                {
                    ref var rc = ref *(ImGuiHelpers.ImFontAtlasCustomRectReal*)toolkitPostBuild.NewImAtlas
                                         .GetCustomRectByIndex(rectId)
                                         .NativePtr;
                    var widthAdjustment = this.BaseStyle.CalculateBaseWidthAdjustment(fdtFontHeader, fdtGlyph);
                    
                    // Glyph is scaled at this point; undo that.
                    ref var glyph = ref glyphs[lookups[rc.GlyphId]];
                    glyph.X0 = this.BaseAttr.HorizontalOffset;
                    glyph.Y0 = fdtGlyph.CurrentOffsetY;
                    glyph.X1 = glyph.X0 + fdtGlyph.BoundingWidth + widthAdjustment;
                    glyph.Y1 = glyph.Y0 + fdtGlyph.BoundingHeight;
                    glyph.AdvanceX = fdtGlyph.AdvanceWidth;

                    var pixels8 = pixels8Array[rc.TextureIndex];
                    var width = widths[rc.TextureIndex];
                    texFiles[fdtGlyph.TextureFileIndex] ??=
                        this.gftp.GetTexFile(this.BaseAttr.TexPathFormat, fdtGlyph.TextureFileIndex);
                    var sourceBuffer = texFiles[fdtGlyph.TextureFileIndex].ImageData;
                    var sourceBufferDelta = fdtGlyph.TextureChannelByteIndex;
                    
                    for (var y = 0; y < fdtGlyph.BoundingHeight; y++)
                    {
                        var sourcePixelIndex =
                            ((fdtGlyph.TextureOffsetY + y) * fdtFontHeader.TextureWidth) + fdtGlyph.TextureOffsetX;
                        sourcePixelIndex *= 4;
                        sourcePixelIndex += sourceBufferDelta;
                        var blend1 = horzBlend[fdtGlyph.CurrentOffsetY + y];
                        
                        var targetOffset = ((rc.Y + y) * width) + rc.X;
                        for (var x = 0; x < rc.Width; x++)
                            pixels8[targetOffset + x] = 0;
                        
                        targetOffset += horzShift[fdtGlyph.CurrentOffsetY + y];
                        if (blend1 == 0)
                        {
                            for (var x = 0; x < fdtGlyph.BoundingWidth; x++, sourcePixelIndex += 4, targetOffset++)
                            {
                                var n = sourceBuffer[sourcePixelIndex + 4];
                                for (var boldOffset = 0; boldOffset < pixelWidth; boldOffset++)
                                {
                                    ref var p = ref pixels8[targetOffset + boldOffset];
                                    p = Math.Max(p, (byte)((pixelStrength[boldOffset] * n) / 255));
                                }
                            }
                        }
                        else
                        {
                            var blend2 = 255 - blend1;
                            for (var x = 0; x < fdtGlyph.BoundingWidth; x++, sourcePixelIndex += 4, targetOffset++)
                            {
                                var a1 = sourceBuffer[sourcePixelIndex];
                                var a2 = x == fdtGlyph.BoundingWidth - 1 ? 0 : sourceBuffer[sourcePixelIndex + 4];
                                var n = (a1 * blend1) + (a2 * blend2);

                                for (var boldOffset = 0; boldOffset < pixelWidth; boldOffset++)
                                {
                                    ref var p = ref pixels8[targetOffset + boldOffset];
                                    p = Math.Max(p, (byte)((pixelStrength[boldOffset] * n) / 255 / 255));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

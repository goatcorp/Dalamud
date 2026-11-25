using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>A <see cref="Font"/> backed by a shared <see cref="DirectWriteFontFace"/> with per-font locale, OpenType
/// features, hinting mode, and optional synthetic slant.</summary>
/// <remarks>Metrics and rasterized glyph images live on the face, so fonts sharing a face share cached glyphs.
/// </remarks>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal sealed unsafe partial class DirectWriteFont : Font
{
    private static readonly ModuleLog Log = new(typeof(DirectWriteFont));

    private readonly DirectWriteFontFace face;
    private readonly string locale;
    private readonly float slant;
    private readonly int slantId;
    private readonly ImmutableArray<FontFeature> features;
    private readonly DWRITE_FONT_FEATURE[]? nativeFeatures;
    private readonly bool noHinting;
    private ushort[] indexBuffer = [];
    private DWRITE_GLYPH_METRICS[] metricsBuffer = [];

    /// <summary>Initializes a new instance of the <see cref="DirectWriteFont"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="face">The face, shared with other fonts.</param>
    /// <param name="locale">The locale to shape text in, such as "ja-JP".</param>
    /// <param name="slant">Synthetic italic shear as horizontal shift per pixel above the baseline.</param>
    /// <param name="features">OpenType features for shaping, or empty for defaults.</param>
    /// <param name="noHinting">Whether to disable pixel-grid fitting when rasterizing.</param>
    public DirectWriteFont(
        string name, DirectWriteFontFace face, string locale, float slant = 0,
        ImmutableArray<FontFeature> features = default, bool noHinting = false)
        : base(name)
    {
        this.face = face;
        this.locale = locale;
        this.slant = slant;
        this.slantId = face.GetOrAddSlantId(slant);
        this.features = features.IsDefault ? [] : features;
        this.nativeFeatures = ToNative(this.features);
        this.noHinting = noHinting;
    }

    /// <inheritdoc/>
    public override bool HasColorGlyphs => this.face.HasColorGlyphs;

    /// <inheritdoc/>
    /// <remarks><see cref="DirectWriteFontFace"/> sizes represent line height, so
    /// <see cref="DirectWriteFontFace.EmSize"/> at <c>1</c> is <c>em / lineHeight</c>.</remarks>
    public override float LineHeightRatioAtEm(float em) => 1 / this.face.EmSize(1);

    /// <inheritdoc/>
    public override float LineHeightRatioAtLineHeight(float lineHeight) => 1 / this.face.EmSize(1);

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) =>
        this.face.GetMetrics(this.SnapSize(size, density, out _));

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        var pooledIndices = PooledSpan<ushort>.FromBuffer(ref this.indexBuffer, codepoints.Length);
        var indices = pooledIndices.Span;
        fixed (uint* pCodepoints = codepoints)
        fixed (ushort* pIndices = indices)
            this.face.Native->GetGlyphIndices(pCodepoints, (uint)codepoints.Length, pIndices).ThrowOnError();

        for (var i = 0; i < codepoints.Length; i++)
            glyphs[i] = indices[i];
        pooledIndices.Recycle();
    }

    /// <inheritdoc/>
    public override void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics)
    {
        var pooledIndices = PooledSpan<ushort>.FromBuffer(ref this.indexBuffer, glyphs.Length);
        var indices = pooledIndices.Span;
        for (var i = 0; i < glyphs.Length; i++)
            indices[i] = (ushort)glyphs[i];
        var pooledDesign = PooledSpan<DWRITE_GLYPH_METRICS>.FromBuffer(ref this.metricsBuffer, glyphs.Length);
        var design = pooledDesign.Span;
        size = this.SnapSize(size, density, out var gdi);
        fixed (ushort* pIndices = indices)
        fixed (DWRITE_GLYPH_METRICS* pDesign = design)
        {
            (gdi
                 ? this.face.Native->GetGdiCompatibleGlyphMetrics(
                     this.face.EmSize(size), density, null, false, pIndices, (uint)glyphs.Length, pDesign, false)
                 : this.face.Native->GetDesignGlyphMetrics(pIndices, (uint)glyphs.Length, pDesign, false))
                .ThrowOnError();
        }

        var scale = this.face.Scale(size);
        for (var i = 0; i < glyphs.Length; i++)
        {
            ref readonly var m = ref design[i];
            var advance = gdi ? MathF.Round(m.advanceWidth * scale * density) / density : m.advanceWidth * scale;
            var x0 = m.leftSideBearing * scale;
            var x1 = ((int)m.advanceWidth - m.rightSideBearing) * scale;
            var y0 = (m.topSideBearing - m.verticalOriginY) * scale;
            var y1 = ((int)m.advanceHeight - m.bottomSideBearing - m.verticalOriginY) * scale;
            // Positive slant (y down) shifts points above the baseline right and below it left.
            metrics[i] = x1 > x0 && y1 > y0
                             ? new()
                             {
                                 Advance = advance,
                                 Bounds = new(
                                     x0 - (Math.Max(y1, 0) * this.slant),
                                     y0,
                                     x1 - (Math.Min(y0, 0) * this.slant),
                                     y1),
                             }
                             : new() { Advance = advance };
        }

        pooledIndices.Recycle();
        pooledDesign.Recycle();
    }

    /// <inheritdoc/>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images) =>
        this.face.GetImages(
            glyphs,
            subpixelSteps,
            this.SnapSize(size, density, out _),
            density,
            embolden,
            subpixel,
            this.slant,
            this.slantId,
            this.noHinting,
            images);

    /// <inheritdoc/>
    /// <remarks>Resolves variable axes or matching installed family faces when available, and falls back to simulated bold
    /// or synthetic slant via <see cref="DirectWriteFontFace.GetStyled"/>.</remarks>
    protected override Font CreateStyled(in TextStyle key)
    {
        try
        {
            var styled = this.face.GetStyled(key, this.slant != 0, out var slanted, out var italicFace);
            return new DirectWriteFont(
                $"{this.Name} {key.On(default)}",
                styled,
                key.Locale ?? this.locale,
                slanted ? SyntheticStyleFont.Slant : italicFace ? 0 : this.slant,
                key.Features.IsEmpty ? this.features : key.Features,
                key.NoHinting || this.noHinting);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "[{Font}] Could not satisfy style request.", this);
            return base.CreateStyled(key);
        }
    }

    // Allocates on the pinned object heap so Shape can pass the pointer directly without fixing per call.
    private static DWRITE_FONT_FEATURE[]? ToNative(ImmutableArray<FontFeature> features)
    {
        if (features.IsDefaultOrEmpty)
            return null;
        var native = GC.AllocateArray<DWRITE_FONT_FEATURE>(features.Length, true);
        for (var i = 0; i < native.Length; i++)
            native[i] = new() { nameTag = (DWRITE_FONT_FEATURE_TAG)features[i].Tag, parameter = features[i].Value };
        return native;
    }

    // Snaps size so the device-pixel em is an integer when the face uses an embedded bitmap strike there.
    // DirectWrite selects the nearest integer-em strike and only yields whole-pixel GDI-compatible advances at that em.
    private float SnapSize(float size, float density, out bool drawsBitmaps)
    {
        var em = this.face.EmSize(size) * density;
        var whole = MathF.Round(em);
        drawsBitmaps = this.face.DrawsBitmaps(whole, this.slant, this.noHinting);
        return whole != em && drawsBitmaps ? size * whole / em : size;
    }
}

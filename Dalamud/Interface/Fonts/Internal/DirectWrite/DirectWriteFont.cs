using System.Collections.Immutable;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>A <see cref="DirectWriteFontFace"/> shaped in a locale, and maybe slanted.</summary>
/// <remarks>The face has the metrics and the glyph images. So fonts of the same face share their glyph images.
/// </remarks>
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
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the outlines by, for synthetic
    /// italic.</param>
    /// <param name="features">The OpenType features to shape text with, or empty for the default features.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    public DirectWriteFont(
        string name, DirectWriteFontFace face, string locale, float slant = 0,
        ImmutableArray<FontFeature> features = default, bool noHinting = false)
        : base(name)
    {
        this.face = face;
        this.locale = locale;
        this.slant = slant;
        this.slantId = face.SlantIdOf(slant);
        this.features = features.IsDefault ? [] : features;
        this.nativeFeatures = ToNative(this.features);
        this.noHinting = noHinting;
    }

    /// <inheritdoc/>
    public override float NaturalRatio(float size, bool isEm) => 1 / this.face.EmSize(1);

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) =>
        this.face.GetMetrics(this.SnapSize(size, density));

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        var pooledIndices = PooledSpan<ushort>.FromBuffer(ref this.indexBuffer, codepoints.Length);
        var indices = pooledIndices.Span;
        fixed (uint* pCodepoints = codepoints)
        {
            fixed (ushort* pIndices = indices)
                this.face.Native->GetGlyphIndices(pCodepoints, (uint)codepoints.Length, pIndices).ThrowOnError();
        }

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
        size = this.SnapSize(size, density);
        var gdi = this.DrawsBitmaps(size, density);
        fixed (ushort* pIndices = indices)
        {
            fixed (DWRITE_GLYPH_METRICS* pDesign = design)
            {
                (gdi
                     ? this.face.Native->GetGdiCompatibleGlyphMetrics(
                         this.face.EmSize(size), density, null, false, pIndices, (uint)glyphs.Length, pDesign, false)
                     : this.face.Native->GetDesignGlyphMetrics(pIndices, (uint)glyphs.Length, pDesign, false))
                    .ThrowOnError();
            }
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
            // Slanting moves what is below the baseline left, and what is above it right.
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
    /// <remarks>The face rasterizes and caches them, with this font's slant.</remarks>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images) =>
        this.face.GetImages(
            glyphs,
            subpixelSteps,
            this.SnapSize(size, density),
            density,
            embolden,
            subpixel,
            this.slant,
            this.slantId,
            this.noHinting,
            images);

    /// <inheritdoc/>
    /// <remarks>Takes the faces of the installed family of the same name if it has them, and has DirectWrite simulate
    /// the rest.</remarks>
    protected override Font CreateStyled(in TextStyle key)
    {
        try
        {
            var styled = this.face.GetStyled(key, this.slant != 0, out var slanted);
            return new DirectWriteFont(
                $"{this.Name} {key.On(default)}",
                styled,
                key.Locale ?? this.locale,
                slanted ? SyntheticStyleFont.Slant : this.slant,
                key.Features.IsEmpty ? this.features : key.Features,
                key.NoHinting || this.noHinting);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "[{Font}] Could not satisfy style request.", this);
            return base.CreateStyled(key);
        }
    }

    // Gets features as DirectWrite takes them, as a pinned array, or null for none.
    private static DWRITE_FONT_FEATURE[]? ToNative(ImmutableArray<FontFeature> features)
    {
        if (features.IsDefaultOrEmpty)
            return null;
        var native = GC.AllocateArray<DWRITE_FONT_FEATURE>(features.Length, true);
        for (var i = 0; i < native.Length; i++)
            native[i] = new() { nameTag = (DWRITE_FONT_FEATURE_TAG)features[i].Tag, parameter = features[i].Value };
        return native;
    }

    // Gets whether the face draws embedded bitmaps at a size, and so takes GDI-compatible metrics. Their advances
    // are whole device pixels, as the bitmaps are placed. They are rounded, as design units are a little off them.
    private bool DrawsBitmaps(float size, float density) =>
        this.face.DrawsBitmaps(this.face.EmSize(size) * density, this.slant, this.noHinting);

    // Gets the size to draw at, with the em moved to the nearest whole device pixel if the face draws embedded
    // bitmaps there. DirectWrite draws the strike of the nearest whole em, and its advances are whole pixels only
    // at that em.
    private float SnapSize(float size, float density)
    {
        var em = this.face.EmSize(size) * density;
        var whole = MathF.Round(em);
        return whole != em && this.face.DrawsBitmaps(whole, this.slant, this.noHinting) ? size * whole / em : size;
    }
}

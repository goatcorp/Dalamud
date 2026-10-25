using Dalamud.Interface.Fonts.Internal.Glyphs;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font emboldened or slanted, for fonts without bold or italic faces.</summary>
internal sealed class SyntheticStyleFont : Font
{
    /// <summary>Horizontal shift per pixel above the baseline. It is cos(75 degrees), as the game slants italic text.
    /// </summary>
    /// <remarks>The game shifts the top of each glyph quad by the font's line height times this
    /// (<c>AtkFontAnalyzerRenderer</c>), and its glyph quads span the line.</remarks>
    public const float Slant = 0.25881904f;

    private readonly Font inner;
    private readonly FontStyle style;
    private readonly bool emboldens;
    private readonly bool slants;

    /// <summary>Initializes a new instance of the <see cref="SyntheticStyleFont"/> class.</summary>
    /// <param name="inner">The font, taken as regular.</param>
    /// <param name="style">The style. Weights from 600 are emboldened, and italic is slanted.</param>
    public SyntheticStyleFont(Font inner, FontStyle style)
        : base($"{inner.Name} {style}")
    {
        this.inner = inner;
        this.style = style;
        this.emboldens = style.EffectiveWeight >= 600;
        this.slants = style.Italicized;
    }

    /// <inheritdoc/>
    public override Font GetPrimary(ref float sizeRatio) => this.inner.GetPrimary(ref sizeRatio);

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) => this.inner.GetMetrics(size, density);

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs) =>
        this.inner.GetGlyphIndices(codepoints, glyphs);

    /// <inheritdoc/>
    public override void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics)
    {
        this.inner.GetGlyphMetrics(glyphs, size, density, metrics);
        var widen = this.Widen(size, density) / density;
        foreach (ref var m in metrics[..glyphs.Length])
        {
            // Zero-advance glyphs such as marks stay zero, as in Shape.
            if (m.Advance != 0)
                m.Advance += widen;
            if (m.Bounds == default)
                continue;
            m.Bounds.Z += widen;
            if (this.slants)
            {
                m.Bounds.X -= Math.Max(m.Bounds.W, 0) * Slant;
                m.Bounds.Z -= Math.Min(m.Bounds.Y, 0) * Slant;
            }
        }
    }

    /// <inheritdoc/>
    public override void Shape(ShapeRun run, ShapedGlyphList output)
    {
        var first = output.Count;
        this.inner.Shape(run, output);
        var widen = this.Widen(run.Size, run.Density) / run.Density;
        if (widen == 0)
            return;
        foreach (ref var g in output.Span[first..])
        {
            if (g.Advance != 0)
                g.Advance += widen;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Slanted glyphs are not drawn with subpixel coverage, as slanting resamples them.</remarks>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images)
    {
        this.inner.GetGlyphImages(
            glyphs,
            subpixelSteps,
            size,
            density,
            embolden + this.Widen(size, density),
            subpixel && !this.slants,
            images);
        if (!this.slants)
            return;
        foreach (ref var image in images[..glyphs.Length])
            image.Slant += Slant;
    }

    /// <inheritdoc/>
    protected override Font CreateStyled(in TextStyle key)
    {
        var on = key.On(this.style);
        return this.inner.WithStyle(
            key with { Weight = on.Weight, Italic = on.Italicized, Stretch = on.Stretch, Bold = false });
    }

    // In device pixels. None at size 0, where nothing is drawn.
    private float Widen(float size, float density) =>
        this.emboldens && size * density > 0 ? Math.Max(1, MathF.Round((size * density) / 16)) : 0;
}

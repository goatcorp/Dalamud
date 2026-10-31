using System.Numerics;

using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>Elements, and how they change the glyphs of their fonts.</summary>
internal sealed partial class CompositeFont
{
    // Applies an element's advance rules to glyphs its font shaped. Offsets are applied to the images instead.
    private void AdjustShaped(Element e, float size, float density, Span<ShapedGlyph> glyphs)
    {
        if (!e.FitToSquare && !e.HasAdvanceAdjustments)
            return;
        var pooledLeaf = PooledSpan<uint>.FromBuffer(ref this.shapeLeafBuffer, glyphs.Length);
        var pooledNominal = PooledSpan<GlyphMetrics>.FromBuffer(ref this.shapeMetricsBuffer, glyphs.Length);
        var pooledAdjusted = PooledSpan<GlyphMetrics>.FromBuffer(ref this.shapeAdjustedBuffer, glyphs.Length);
        var pooledShift = PooledSpan<Vector2>.FromBuffer(ref this.shapeShiftBuffer, glyphs.Length);
        var leaf = pooledLeaf.Span;
        var nominal = pooledNominal.Span;
        var adjusted = pooledAdjusted.Span;
        var shift = pooledShift.Span;
        for (var i = 0; i < glyphs.Length; i++)
            leaf[i] = glyphs[i].Glyph;
        e.Font.GetGlyphMetrics(leaf, this.SizeOf(e, size, density), density, nominal);
        nominal.CopyTo(adjusted);
        this.Adjust(e, leaf, size, density, adjusted, shift, default);
        for (var i = 0; i < glyphs.Length; i++)
        {
            ref var g = ref glyphs[i];
            if (e.FitToSquare)
            {
                g.Advance = adjusted[i].Advance;
                g.Offset = default;
            }
            else if (g.Advance != 0)
            {
                g.Advance += adjusted[i].Advance - nominal[i].Advance;
            }
        }

        pooledLeaf.Recycle();
        pooledNominal.Recycle();
        pooledAdjusted.Recycle();
        pooledShift.Recycle();
    }

    // Applies an element's offsets and advance rules to metrics from its font. Also computes the shift of the glyph
    // images in logical pixels, and with sizes, the size to draw each image at.
    private void Adjust(
        Element e,
        ReadOnlySpan<uint> leaf,
        float size,
        float density,
        Span<GlyphMetrics> metrics,
        Span<Vector2> shift,
        Span<float> sizes)
    {
        var em = this.EmOf(e, size, density);
        var offset = Snap(e.OffsetAt(em), density);
        var globalScale = FontSizes.GlobalScale;
        var minAdvance = StyleLength.Resolve(e.MinAdvanceX, em, globalScale, 0);
        var maxAdvance = Math.Max(minAdvance, StyleLength.Resolve(e.MaxAdvanceX, em, globalScale, float.MaxValue));
        if (e.FitToSquare)
        {
            var fontMetrics = this.GlyphFontMetrics(size, density);
            var elementSize = this.SizeOf(e, size, density);
            Span<GlyphMetrics> one = stackalloc GlyphMetrics[1];
            for (var i = 0; i < metrics.Length; i++)
            {
                var b = metrics[i].Bounds;
                var w = b.Z - b.X;
                var h = b.W - b.Y;
                var ratio = Math.Max(1f, Math.Max(w / em, h / em));
                var glyphSize = elementSize / ratio;
                if (ratio > 1f)
                {
                    glyphSize = MathF.Floor(glyphSize * density * Font.SizeStepsPerPixel)
                                / (density * Font.SizeStepsPerPixel);
                    e.Font.GetGlyphMetrics(leaf.Slice(i, 1), glyphSize, density, one);
                    b = one[0].Bounds;
                    w = b.Z - b.X;
                    h = b.W - b.Y;
                }

                // Shifted by whole device pixels, so that the image is not resampled.
                var x0 = (em - w) / 2f;
                var y0 = ((fontMetrics.LineHeight - h) / 2f) - fontMetrics.Ascent;
                var s = Snap(new(x0 - b.X, y0 - b.Y), density) + offset;
                shift[i] = s;
                metrics[i] = new()
                {
                    Advance = em,
                    Bounds = w > 0 && h > 0 ? b + new Vector4(s.X, s.Y, s.X, s.Y) : default,
                };
                if (!sizes.IsEmpty)
                    sizes[i] = glyphSize;
            }

            return;
        }

        for (var i = 0; i < metrics.Length; i++)
        {
            ref var m = ref metrics[i];
            var advance = m.Advance;
            var dx = 0f;
            if (minAdvance > 0 || maxAdvance < float.MaxValue)
            {
                var clamped = Math.Clamp(advance, minAdvance, maxAdvance);
                dx = (clamped - advance) / 2f;
                advance = clamped;
            }

            advance += (e.ExtraAdvanceX * em) + (e.ExtraAdvanceXPx * globalScale);
            var s = offset + new Vector2(dx, 0);
            shift[i] = s;
            m.Advance = advance;
            if (m.Bounds != default)
                m.Bounds += new Vector4(s.X, s.Y, s.X, s.Y);
        }
    }

    /// <summary>A font in a composite, with the adjustments from its <see cref="ChainFamilyId.Element"/>.</summary>
    /// <param name="Font">The font.</param>
    /// <param name="Ranges">Codepoint ranges taken from the font, sorted, or null for all.</param>
    /// <param name="SizeRatio">The em of the font is the em of the composite times this.</param>
    /// <param name="Offset">Glyph offset in ems of the font.</param>
    /// <param name="OffsetPx">Glyph offset in pixels before global scaling.</param>
    /// <param name="ExtraAdvanceX">Added to advances, in ems of the font.</param>
    /// <param name="ExtraAdvanceXPx">Added to advances, in pixels before global scaling.</param>
    /// <param name="MinAdvanceX">Minimum advance, with glyphs centered.</param>
    /// <param name="MaxAdvanceX">Maximum advance, or <see cref="StyleUnit.Default"/> for none.</param>
    /// <param name="FitToSquare">Scale glyphs down to fit a square of the font's em, centered.</param>
    /// <param name="Supplement">Only fills in characters, such as the game symbols. The composite takes its metrics
    /// and its .notdef glyph from the first element that is not a supplement.</param>
    /// <param name="Terminal">Ends the font, such as the default chain. It goes after the other elements, and only
    /// the first terminal element is taken.</param>
    public sealed record Element(
        Font Font,
        (uint Low, uint High)[]? Ranges = null,
        float SizeRatio = 1f,
        Vector2 Offset = default,
        Vector2 OffsetPx = default,
        float ExtraAdvanceX = 0f,
        float ExtraAdvanceXPx = 0f,
        StyleLength MinAdvanceX = default,
        StyleLength MaxAdvanceX = default,
        bool FitToSquare = false,
        bool Supplement = false,
        bool Terminal = false)
    {
        /// <summary>Gets a value indicating whether advances change.</summary>
        public bool HasAdvanceAdjustments =>
            this.HasAdvanceLimits || this.ExtraAdvanceX != 0 || this.ExtraAdvanceXPx != 0;

        /// <summary>Gets a value indicating whether where glyph images go or their size depends on the glyphs'
        /// metrics.</summary>
        /// <remarks>Clamped advances center the images, and fitting to a square scales them. Extra advances
        /// leave them where <see cref="Offset"/> puts them.</remarks>
        public bool PlacesImagesByMetrics => this.FitToSquare || this.HasAdvanceLimits;

        private bool HasAdvanceLimits =>
            this.MinAdvanceX.Unit != StyleUnit.Default || this.MaxAdvanceX.Unit != StyleUnit.Default;

        /// <summary>Gets the glyph offset in pixels.</summary>
        /// <param name="em">The em of the font.</param>
        /// <returns>The offset.</returns>
        public Vector2 OffsetAt(float em) => (this.Offset * em) + (this.OffsetPx * FontSizes.GlobalScale);

        /// <summary>Tests whether a codepoint is in <see cref="Ranges"/>.</summary>
        /// <param name="codepoint">The codepoint.</param>
        /// <returns>Whether the element takes it.</returns>
        public bool Contains(uint codepoint)
        {
            if (this.Ranges is null)
                return true;
            foreach (var (low, high) in this.Ranges)
            {
                if (codepoint < low)
                    return false;
                if (codepoint <= high)
                    return true;
            }

            return false;
        }
    }
}

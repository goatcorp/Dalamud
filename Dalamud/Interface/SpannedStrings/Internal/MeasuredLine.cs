using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>Represents a measured line.</summary>
internal struct MeasuredLine
{
    /// <summary>The offset in a <see cref="SpannedString"/>.</summary>
    public SpannedOffset Offset;

    /// <summary>The horizontal cursor offset at the end of the line.</summary>
    public float X;

    /// <summary>The left(-) and right(+) bounds of the line.</summary>
    public Vector2 BBoxHorizontal;

    /// <summary>The ascent(-) and descent(+) of the line.</summary>
    public Vector2 BBoxVertical;

    /// <summary>The effective glyph codepoint of the last character processed.</summary>
    /// <remarks>Used only for calculating kerning distances.</remarks>
    public int LastGlyphCodepoint;

    /// <summary>Whether the line ends because it was too long and got wrapped/truncated.</summary>
    public bool IsWrapped;

    /// <summary>Whether the line ends because there is a line break character at the end.</summary>
    public bool HasNewLineAtEnd;

    /// <summary>Initializes a new instance of the <see cref="MeasuredLine"/> struct.</summary>
    /// <param name="offset">The span cursor offset.</param>
    /// <param name="x">The horizontal cursor offset.</param>
    /// <param name="bBoxHorizontal">The left and right bounds of the line.</param>
    /// <param name="bBoxVertical">The ascent and descent of the line.</param>
    /// <param name="lastGlyphCodepoint">The last glyph codepoint.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MeasuredLine(
        SpannedOffset offset,
        float x,
        Vector2 bBoxHorizontal,
        Vector2 bBoxVertical,
        int lastGlyphCodepoint)
    {
        this.Offset = offset;
        this.LastGlyphCodepoint = lastGlyphCodepoint;
        this.X = x;
        this.BBoxHorizontal = bBoxHorizontal;
        this.BBoxVertical = bBoxVertical;
        this.IsWrapped = false;
        this.HasNewLineAtEnd = false;
    }

    /// <summary>Gets an empty value.</summary>
    public static MeasuredLine Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new()
        {
            Offset = SpannedOffset.Empty,
            BBoxHorizontal = new(float.MaxValue, float.MinValue),
            BBoxVertical = new(float.MaxValue, float.MinValue),
            LastGlyphCodepoint = -1,
        };
    }

    /// <summary>Gets the width.</summary>
    public readonly float Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.BBoxHorizontal.X < this.BBoxHorizontal.Y ? this.BBoxHorizontal.Y - this.BBoxHorizontal.X : 0;
    }

    /// <summary>Gets the height.</summary>
    public readonly float Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.BBoxVertical.X < this.BBoxVertical.Y ? this.BBoxVertical.Y - this.BBoxVertical.X : 0;
    }

    /// <summary>Gets a value indicating whether nothing has been measured yet.</summary>
    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Offset == SpannedOffset.Empty;
    }

    /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
    /// <param name="si1">The first value.</param>
    /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static MeasuredLine FirstNonEmpty(in MeasuredLine si1)
    {
        if (!si1.IsEmpty)
            return si1;
        return Empty;
    }

    /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
    /// <param name="si1">The first value.</param>
    /// <param name="si2">The second value.</param>
    /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static MeasuredLine FirstNonEmpty(in MeasuredLine si1, in MeasuredLine si2)
    {
        if (!si1.IsEmpty)
            return si1;
        if (!si2.IsEmpty)
            return si2;
        return Empty;
    }

    /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
    /// <param name="si1">The first value.</param>
    /// <param name="si2">The second value.</param>
    /// <param name="si3">The third value.</param>
    /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static MeasuredLine FirstNonEmpty(in MeasuredLine si1, in MeasuredLine si2, in MeasuredLine si3)
    {
        if (!si1.IsEmpty)
            return si1;
        if (!si2.IsEmpty)
            return si2;
        if (!si3.IsEmpty)
            return si3;
        return Empty;
    }
    
    /// <summary>Unions the given horizontal boundary box to this instance.</summary>
    /// <param name="x0">The left boundary.</param>
    /// <param name="x1">The right boundary.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void UnionBBoxHorizontal(float x0, float x1)
    {
        this.BBoxHorizontal.X = MathF.Round(Math.Min(this.BBoxHorizontal.X, x0));
        this.BBoxHorizontal.Y = MathF.Round(Math.Max(this.BBoxHorizontal.Y, x1));
    }

    /// <summary>Unions the given vertical boundary box to this instance.</summary>
    /// <param name="ascent">The top boundary (=ascent for this implementation).</param>
    /// <param name="descent">The bottom boundary (=descent for this implementation).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void UnionBBoxVertical(float ascent, float descent)
    {
        this.BBoxVertical.X = MathF.Round(Math.Min(this.BBoxVertical.X, ascent));
        this.BBoxVertical.Y = MathF.Round(Math.Max(this.BBoxVertical.Y, descent));
    }

    /// <summary>Returns a new instance of this struct after calling <see cref="AddObject"/>.</summary>
    /// <param name="font">The font data.</param>
    /// <param name="x0">The X0.</param>
    /// <param name="x1">The X1.</param>
    /// <returns>The new instance.</returns>
    public readonly MeasuredLine WithObject(in SpanStyleFontData font, float x0, float x1)
    {
        var test = this;
        test.AddObject(font, x0, x1);
        return test;
    }

    /// <summary>Returns a new instance of this struct after setting <see cref="IsWrapped"/>.</summary>
    /// <returns>The new instance.</returns>
    public readonly MeasuredLine WithWrapped() => this with { IsWrapped = true };

    /// <summary>Sets the offset.</summary>
    /// <param name="offset">The offset.</param>
    /// <param name="pad">The extra padding, if any.</param>
    public void SetOffset(SpannedOffset offset, float pad = 0f)
    {
        this.Offset = offset;
        if (pad != 0f)
        {
            this.LastGlyphCodepoint = -1;
            this.X += MathF.Round(pad);
            this.UnionBBoxHorizontal(this.X, this.X);
        }
    }

    /// <summary>Adds an object.</summary>
    /// <param name="font">The font data.</param>
    /// <param name="x0">The X0.</param>
    /// <param name="x1">The X1.</param>
    public void AddObject(in SpanStyleFontData font, float x0, float x1)
    {
        this.LastGlyphCodepoint = -1;
        this.UnionBBoxHorizontal(this.X + x0, this.X + x1);
        this.X += MathF.Round(x1);
    }

    /// <summary>Adds a character.</summary>
    /// <param name="font">The font.</param>
    /// <param name="codepoint">The codepoint.</param>
    /// <param name="xy0">The scaled XY0.</param>
    /// <param name="xy1">The scaled XY1.</param>
    /// <param name="advance">The scaled advance width.</param>
    public void AddCharacter(
        in SpanStyleFontData font,
        int codepoint,
        Vector2 xy0,
        Vector2 xy1,
        float advance)
    {
        var xoff = this.X + font.ScaledHorizontalOffset;
        this.UnionBBoxHorizontal(
            MathF.Round(xoff + xy0.X),
            MathF.Round(xoff + xy1.X + font.GetScaledTopSkew(xy0) + font.BoldExtraWidth));
        this.UnionBBoxVertical(font.BBoxVertical.X, font.BBoxVertical.Y);
        this.X += MathF.Round(advance);
        this.LastGlyphCodepoint = codepoint;
    }

    /// <summary>Adds a standard character.</summary>
    /// <param name="font">The font.</param>
    /// <param name="codepoint">The codepoint.</param>
    public void AddStandardCharacter(in SpanStyleFontData font, int codepoint)
    {
        codepoint = font.GetEffeciveCodepoint(codepoint);
        ref var glyph = ref font.Glyphs[font.Lookup[codepoint]];
        var adjust = font.GetScaledGap(this.LastGlyphCodepoint, codepoint);
        this.AddCharacter(
            font,
            codepoint,
            (glyph.XY0 * font.Scale) + new Vector2(adjust + font.ScaledHorizontalOffset, 0),
            (glyph.XY1 * font.Scale) + new Vector2(adjust + font.ScaledHorizontalOffset, 0),
            (glyph.AdvanceX * font.Scale) + adjust);
    }

    /// <summary>Adds a tab character, by aligning to the specified tab width.</summary>
    /// <param name="font">The font.</param>
    /// <param name="tabWidth">The width.</param>
    public void AddTabCharacter(in SpanStyleFontData font, float tabWidth)
    {
        this.X = MathF.Floor((this.X + tabWidth) / tabWidth) * tabWidth;
        this.UnionBBoxHorizontal(this.X, this.X);
        this.UnionBBoxVertical(font.BBoxVertical.X, font.BBoxVertical.Y);
        this.LastGlyphCodepoint = '\t';
    }

    /// <summary>Adds a soft hyphen character.</summary>
    /// <param name="font">The font.</param>
    public void AddSoftHyphenCharacter(in SpanStyleFontData font)
    {
        var codepoint = font.GetEffeciveCodepoint(SpannedStringRenderer.SoftHyphenReplacementChar);
        ref var glyph = ref font.Glyphs[font.Lookup[codepoint]];
        var adjust = font.GetScaledGap(this.LastGlyphCodepoint, codepoint) + font.ScaledHorizontalOffset;
        this.AddCharacter(
            font,
            codepoint,
            (glyph.XY0 * font.Scale) + new Vector2(adjust, 0),
            (glyph.XY1 * font.Scale) + new Vector2(adjust, 0),
            0);
    }
}

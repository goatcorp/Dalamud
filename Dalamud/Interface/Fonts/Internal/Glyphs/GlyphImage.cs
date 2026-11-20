using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Where a glyph image is in the <see cref="GlyphAtlas"/>.</summary>
/// <remarks>Valid while its slices remain current. <see cref="GlyphAtlas.TryUse"/> checks validity and marks them used
/// for the current frame.</remarks>
internal struct GlyphImage
{
    /// <summary>Top-left texture coordinates in the slice.</summary>
    public Vector2 Uv0;

    /// <summary>Bottom-right texture coordinates in the slice.</summary>
    public Vector2 Uv1;

    /// <summary>Box (x0, y0, x1, y1) in device pixels, relative to the pen on the baseline.</summary>
    /// <remarks>Relative to the integer-snapped pen before adding the subpixel offset.</remarks>
    public Vector4 Rect;

    /// <summary>Top-left texel, <c>x | (y &lt;&lt; 16)</c>.</summary>
    public uint TexelMin;

    /// <summary>Bottom-right texel, exclusive, <c>x | (y &lt;&lt; 16)</c>.</summary>
    public uint TexelMax;

    /// <summary>Glyph pixel format, which determines its atlas plane.</summary>
    public GlyphKind Kind;

    /// <summary>The slice in its plane, or <c>0</c> for an image with a <see cref="LargeHandle"/>.</summary>
    public int Slice;

    /// <summary>The handle of an image too large for the slices, in <see cref="GlyphAtlas.Large"/>, or <c>0</c>.
    /// </summary>
    /// <remarks>Its own views are bound at <c>t1</c> and <c>t2</c> instead of the atlas's.</remarks>
    /// <seealso cref="GlyphAtlas.BindTextures"/>
    public nint LargeHandle;

    /// <summary>The slice in <see cref="GlyphAtlas.Coverage"/> of the text color mask of a color glyph, or <c>-1</c>
    /// if none.</summary>
    public int MaskSlice;

    /// <summary>Top-left texel of the mask, <c>x | (y &lt;&lt; 16)</c>.</summary>
    public uint MaskTexelMin;

    /// <summary>Horizontal shift per pixel above the baseline, for synthetic italic.</summary>
    public float Slant;

    /// <summary>Whether grayscale coverage is corrected for contrast and gamma when drawn, as DirectWrite does with its
    /// own glyphs.</summary>
    /// <remarks>Pre-baked glyph textures such as game fonts leave this <see langword="false"/> because contrast and
    /// gamma are already baked in.</remarks>
    public bool CorrectCoverage;

    /// <summary>Whether the image is drawn at the pen rounded to a whole device pixel, ignoring the subpixel step.
    /// </summary>
    /// <remarks>Embedded bitmaps cannot be shifted by a subpixel. Their advances are whole pixels.</remarks>
    public bool WholePixel;

    /// <summary>The generation of <see cref="Slice"/> when the image was put there.</summary>
    /// <seealso cref="GlyphAtlas.TryUse"/>
    public int Generation;

    /// <summary>The generation of <see cref="MaskSlice"/> when the mask was put there.</summary>
    public int MaskGeneration;

    /// <summary>Whether this image is final and cacheable by layouts while its slices remain current.</summary>
    /// <remarks><see langword="false"/> for deferred glyphs or when the atlas had no room, signaling callers to retry
    /// on a later frame.</remarks>
    public bool IsFinal;

    /// <summary>Gets a value indicating whether there is nothing to draw.</summary>
    public readonly bool IsEmpty => this.TexelMin == this.TexelMax;
}

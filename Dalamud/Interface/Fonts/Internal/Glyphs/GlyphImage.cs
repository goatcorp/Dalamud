using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Where a glyph image is in the <see cref="GlyphAtlas"/>.</summary>
/// <remarks>Valid while its slices are current. <see cref="GlyphAtlas.TryUse"/> tells, and keeps them so for the
/// frame.</remarks>
internal struct GlyphImage
{
    /// <summary>Top-left texture coordinates in the slice.</summary>
    public Vector2 Uv0;

    /// <summary>Bottom-right texture coordinates in the slice.</summary>
    public Vector2 Uv1;

    /// <summary>Box (x0, y0, x1, y1) in device pixels, relative to the pen on the baseline.</summary>
    /// <remarks>The pen excludes the subpixel offset asked for.</remarks>
    public Vector4 Rect;

    /// <summary>Top-left texel, (x | y &lt;&lt; 16).</summary>
    public uint TexelMin;

    /// <summary>Bottom-right texel, exclusive, (x | y &lt;&lt; 16).</summary>
    public uint TexelMax;

    /// <summary>How it stores its coverage, which tells its plane of the atlas.</summary>
    public GlyphKind Kind;

    /// <summary>The slice in its plane, or 0 for an image with <see cref="Textures"/>.</summary>
    public int Slice;

    /// <summary>The views to bind at <c>t1</c> and <c>t2</c> instead of the atlas's, for an image too large for its
    /// slices, or null.</summary>
    /// <seealso cref="GlyphAtlasTextResources.BindTextures"/>
    public nint Textures;

    /// <summary>The slice in <see cref="GlyphAtlas.Coverage"/> of the text color mask of a color glyph, or -1 if none.
    /// </summary>
    public int MaskSlice;

    /// <summary>Top-left texel of the mask, (x | y &lt;&lt; 16).</summary>
    public uint MaskTexelMin;

    /// <summary>Horizontal shift per pixel above the baseline, for synthetic italic.</summary>
    public float Slant;

    /// <summary>Whether grayscale coverage is corrected for contrast and gamma when drawn, as DirectWrite does with its
    /// own glyphs.</summary>
    /// <remarks>Glyphs drawn ahead of time, such as the game's, look as they were drawn without it.</remarks>
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

    /// <summary>Whether the image stays as it is while its slices are current.</summary>
    /// <remarks>Not for glyphs still being rasterized, or that found no room in the atlas. Those are asked for again.
    /// </remarks>
    public bool IsFinal;

    /// <summary>Gets a value indicating whether there is anything to draw.</summary>
    public readonly bool IsEmpty => this.TexelMin == this.TexelMax;
}

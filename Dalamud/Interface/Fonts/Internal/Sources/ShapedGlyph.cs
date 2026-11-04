using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A glyph produced by <see cref="Font.Shape"/>.</summary>
internal struct ShapedGlyph
{
    /// <summary>The glyph id, or 0 if missing.</summary>
    public uint Glyph;

    /// <summary>Index into <see cref="ShapeRun.Text"/> of the first codepoint of the glyph's cluster.</summary>
    /// <remarks>Glyphs of a cluster are consecutive, and clusters ascend.</remarks>
    public int Cluster;

    /// <summary>Advance in logical pixels, including kerning within the run.</summary>
    public float Advance;

    /// <summary>Offset from the pen in logical pixels, y down (mark positioning).</summary>
    public Vector2 Offset;

    /// <summary>Whether ending a line before this glyph needs both sides shaped again.</summary>
    public bool UnsafeToBreak;
}

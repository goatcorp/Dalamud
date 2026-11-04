using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>Metrics of a glyph, in logical pixels.</summary>
internal struct GlyphMetrics
{
    /// <summary>Ink box relative to the pen on the baseline (x0, y0, x1, y1), y down. Zero for blanks.</summary>
    public Vector4 Bounds;

    /// <summary>Horizontal advance.</summary>
    public float Advance;

    /// <summary>Gets the size of the glyph.</summary>
    public readonly Vector2 Size => new(this.Bounds.Z - this.Bounds.X, this.Bounds.W - this.Bounds.Y);
}

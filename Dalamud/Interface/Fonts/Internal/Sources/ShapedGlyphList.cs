using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>Glyphs produced by <see cref="Font.Shape"/>, which appends to it.</summary>
internal sealed class ShapedGlyphList
{
    private ShapedGlyph[] items = new ShapedGlyph[64];

    /// <summary>Gets the number of glyphs.</summary>
    public int Count { get; private set; }

    /// <summary>Gets the glyphs.</summary>
    public Span<ShapedGlyph> Span => this.items.AsSpan(0, this.Count);

    /// <summary>Adds a glyph.</summary>
    /// <param name="glyph">The glyph id.</param>
    /// <param name="cluster">Index into <see cref="ShapeRun.Text"/> of the first codepoint of its cluster.</param>
    /// <param name="advance">Advance in logical pixels.</param>
    /// <param name="offset">Offset from the pen in logical pixels, y down.</param>
    /// <param name="unsafeToBreak">Whether a line may not end before the glyph without shaping again.</param>
    public void Add(uint glyph, int cluster, float advance, Vector2 offset = default, bool unsafeToBreak = false)
    {
        if (this.Count == this.items.Length)
            Array.Resize(ref this.items, this.items.Length * 2);
        this.items[this.Count++] =
            new()
            {
                Glyph = glyph, Cluster = cluster, Advance = advance, Offset = offset, UnsafeToBreak = unsafeToBreak,
            };
    }

    /// <summary>Removes all glyphs.</summary>
    public void Clear() => this.Count = 0;
}

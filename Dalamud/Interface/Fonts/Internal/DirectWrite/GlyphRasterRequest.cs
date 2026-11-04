namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>A glyph to rasterize, maybe on the thread pool.</summary>
/// <param name="Face">The face.</param>
/// <param name="Key">The key of the glyph image in the face's cache.</param>
/// <param name="Glyph">The glyph.</param>
/// <param name="DeviceSize">The size in device pixels.</param>
/// <param name="SubpixelX">Fraction of a device pixel the pen is right of a whole pixel.</param>
/// <param name="Subpixel">Whether subpixel coverage is wanted.</param>
/// <param name="Embolden">Synthetic bold in device pixels, applied when rasterizing.</param>
/// <param name="Slant">Horizontal shift per pixel above the baseline to slant the outlines by.</param>
/// <param name="NoHinting">Whether to rasterize without fitting the outline to the pixel grid.</param>
internal readonly record struct GlyphRasterRequest(
    DirectWriteFontFace Face,
    ulong Key,
    uint Glyph,
    float DeviceSize,
    float SubpixelX,
    bool Subpixel,
    float Embolden,
    float Slant,
    bool NoHinting);

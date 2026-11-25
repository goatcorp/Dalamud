namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Parameters for rasterizing a single glyph on <see cref="DirectWriteFontFace"/>.</summary>
/// <param name="Face">The face.</param>
/// <param name="Key">The key of the glyph image in the face's cache.</param>
/// <param name="Glyph">The glyph index.</param>
/// <param name="DeviceSize">The size in device pixels.</param>
/// <param name="SubpixelX">Subpixel horizontal pen offset in <c>[0, 1)</c>.</param>
/// <param name="Subpixel">Whether subpixel coverage is requested.</param>
/// <param name="Embolden">Synthetic bold in device pixels, applied when rasterizing.</param>
/// <param name="Slant">Synthetic italic shear as horizontal shift per pixel above the baseline.</param>
/// <param name="NoHinting">Whether to disable pixel-grid fitting.</param>
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

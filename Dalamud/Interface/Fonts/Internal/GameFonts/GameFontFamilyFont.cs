using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Fonts.Internal.GameFonts.Formats;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using Lumina.Data.Files;

namespace Dalamud.Interface.Fonts.Internal.GameFonts;

/// <summary>A game font family, such as AXIS, drawn from the game's font textures.</summary>
/// <remarks><ul>
/// <li>Each <c>.fdt</c> file is a bitmap strike whose pixel em size is <see cref="FdtGlyphTableHeader.Size"/> times
/// <c>4 / 3</c>.</li>
/// <li>Requests use the smallest strike at least as large as the target device size, or the largest strike, scaled.
/// Glyphs missing from that strike fall back to the next larger strike that has them, or else the largest smaller
/// one.</li>
/// <li>The natural line box scales the chosen strike's line-height-to-em ratio to the requested em.</li>
/// <li>Glyph IDs are Unicode codepoints.</li>
/// </ul></remarks>
internal sealed class GameFontFamilyFont : Font
{
    private readonly Strike[] strikes;
    private readonly ulong[] codepointBits;
    private readonly uint[] astralCodepoints;
    private readonly Texture?[] textures;
    private readonly GlyphImageCache cache = new();

    /// <summary>Initializes a new instance of the <see cref="GameFontFamilyFont"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="strikes">The .fdt files of the family and their horizontal glyph offsets.</param>
    /// <param name="textures">The font textures by file index, where font1.tex is 0. An entry is null if the texture
    /// failed to load.</param>
    public GameFontFamilyFont(
        string name,
        IEnumerable<(FdtFileView Fdt, int HorizontalOffset)> strikes,
        Texture?[] textures)
        : base(name)
    {
        this.textures = textures;
        this.strikes = strikes.Select(x => new Strike(x.Fdt, x.HorizontalOffset)).OrderBy(x => x.Px).ToArray();
        if (this.strikes.Length == 0)
            throw new ArgumentException("A font family needs at least one strike.", nameof(strikes));
        (this.codepointBits, this.astralCodepoints) = CollectCodepoints(this.strikes);
    }

    /// <inheritdoc/>
    public override bool SizeIsEm => true;

    /// <inheritdoc/>
    public override float LineHeightRatioAtEm(float em)
    {
        var strike = this.strikes[this.StrikeIndexFor(em)];
        return strike.LineHeight / strike.Px;
    }

    /// <inheritdoc/>
    /// <remarks>Uses the smallest strike whose line height is at least <paramref name="lineHeight"/>, or the largest
    /// strike.</remarks>
    public override float LineHeightRatioAtLineHeight(float lineHeight)
    {
        var i = 0;
        while (i < this.strikes.Length - 1 && this.strikes[i].LineHeight < lineHeight - 0.01f)
            i++;
        return this.strikes[i].LineHeight / this.strikes[i].Px;
    }

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density)
    {
        var strike = this.StrikeFor(size * density, out var k);
        k /= density;
        var em = strike.Px * k;
        var thickness = Math.Max(1f, MathF.Round(em / 14f));

        // The files have no strikethrough metrics. The strikethrough goes through the middle of lowercase letters.
        return new(
            strike.Ascent * k,
            (strike.LineHeight - strike.Ascent) * k,
            strike.LineHeight * k,
            Math.Max(1f, MathF.Round((strike.LineHeight - strike.Ascent) * k * 0.4f)),
            thickness,
            MathF.Round((em * 0.28f) + (thickness * 0.5f)),
            thickness);
    }

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        for (var i = 0; i < codepoints.Length; i++)
            glyphs[i] = codepoints[i] != 0 && this.HasCodepoint(codepoints[i]) ? codepoints[i] : 0;
    }

    /// <inheritdoc/>
    public override void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics)
    {
        var deviceSize = size * density;
        var preferred = this.StrikeIndexFor(deviceSize);
        for (var i = 0; i < glyphs.Length; i++)
        {
            var strike = this.strikes[this.StrikeIndexWith(preferred, glyphs[i])];
            if (!strike.Fdt.TryGetGlyph(glyphs[i], out var g))
            {
                metrics[i] = default;
                continue;
            }

            var k = ScaleOf(strike, deviceSize) / density;
            metrics[i] = new()
            {
                Advance = g.AdvanceWidth * k,
                Bounds = g.BoundingWidth == 0 || g.BoundingHeight == 0
                             ? default
                             : new(
                                 strike.HorizontalOffset * k,
                                 (g.CurrentOffsetY - strike.Ascent) * k,
                                 (strike.HorizontalOffset + g.BoundingWidth) * k,
                                 ((g.CurrentOffsetY + g.BoundingHeight) - strike.Ascent) * k),
            };
        }
    }

    /// <inheritdoc/>
    /// <remarks><ul>
    /// <li>Maps each codepoint to its glyph, omitting missing non-initial codepoints within a grapheme cluster.</li>
    /// <li>Each grapheme is a cluster.</li>
    /// <li>Pairs are kerned using the strike's kerning table.</li>
    /// </ul></remarks>
    public override void Shape(ShapeRun run, ShapedGlyphList output)
    {
        var n = run.End - run.Begin;
        if (n <= 0)
            return;
        var graphemes = this.FindGraphemes(run);
        var pooledGlyphs = new PooledSpan<uint>(n <= 256 ? stackalloc uint[n] : default, n);
        var glyphs = pooledGlyphs.Span;
        this.GetGlyphIndices(run.Text[run.Begin..run.End], glyphs);
        var first = output.Count;
        var grapheme = run.Begin;
        for (int i = 0, g = 0; i < n; i++)
        {
            var index = run.Begin + i;
            if (g < graphemes.Count && graphemes[g] == index)
            {
                grapheme = index;
                g++;
            }
            else if (glyphs[i] == 0)
            {
                continue;
            }

            output.Add(glyphs[i], grapheme, 0);
        }

        var added = output.Span[first..];
        var ids = glyphs[..added.Length];
        for (var i = 0; i < added.Length; i++)
            ids[i] = added[i].Glyph;
        var pooledMetrics = new PooledSpan<GlyphMetrics>(
            added.Length <= 64 ? stackalloc GlyphMetrics[added.Length] : default,
            added.Length);
        var metrics = pooledMetrics.Span;
        this.GetGlyphMetrics(ids, run.Size, run.Density, metrics);
        var strike = this.StrikeFor(run.Size * run.Density, out var k);
        k /= run.Density;
        for (var i = 0; i < added.Length; i++)
        {
            added[i].Advance = metrics[i].Advance;
            if (i + 1 < added.Length)
                added[i].Advance += strike.Fdt.GetKerning(ids[i], ids[i + 1]) * k;
        }

        pooledGlyphs.Recycle();
        pooledMetrics.Recycle();
    }

    /// <inheritdoc/>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images)
    {
        var deviceSize = size * density;
        var preferred = this.StrikeIndexFor(deviceSize);
        for (var i = 0; i < glyphs.Length; i++)
        {
            var strikeIndex = this.StrikeIndexWith(preferred, glyphs[i]);
            var strike = this.strikes[strikeIndex];
            var k = ScaleOf(strike, deviceSize);

            // Convert embolden to strike pixels. Zero scale means size 0, where nothing is drawn.
            var strikeEmbolden = k > 0 ? embolden / k : 0;
            var key = glyphs[i]
                      | ((ulong)strikeIndex << 24)
                      | (GlyphImageCache.Quantize8(strikeEmbolden) << 32);
            if (!this.cache.TryGet(key, out var image))
            {
                var start = FontProfiler.Begin(FontProfiler.Section.Rasterize);
                var bitmap = this.Rasterize(strike, glyphs[i], strikeEmbolden);
                FontProfiler.End(FontProfiler.Section.Rasterize, start);
                image = this.cache.Add(key, bitmap);
            }

            // Game font bitmaps have no baked subpixel offset, so pass the subpixel step as a quad shift.
            images[i] = GlyphAtlas.Shared.ToImage(image, k, new((float)subpixelSteps[i] / SubpixelStepCount, 0));
        }
    }

    // Builds a BMP bitset and sorted astral array across all strikes, and marks strikes that contain every codepoint.
    private static (ulong[] Bits, uint[] Astral) CollectCodepoints(Strike[] strikes)
    {
        var bits = new ulong[0x10000 / 64];
        var words = 0;
        var astral = new SortedSet<uint>();
        var counts = new int[strikes.Length];
        for (var i = 0; i < strikes.Length; i++)
        {
            // The glyph table is sorted, so a repeated codepoint follows its first entry.
            var previous = uint.MaxValue;
            foreach (ref readonly var g in strikes[i].Fdt.Glyphs)
            {
                var c = g.Rune.Codepoint;
                if (c != previous)
                    counts[i]++;
                previous = c;
                if (c >= 0x10000)
                {
                    astral.Add(c);
                    continue;
                }

                bits[c >> 6] |= 1ul << (int)(c & 63);
                words = Math.Max(words, (int)(c >> 6) + 1);
            }
        }

        var total = astral.Count;
        foreach (var word in bits)
            total += BitOperations.PopCount(word);
        for (var i = 0; i < strikes.Length; i++)
            strikes[i].HasAll = counts[i] == total;
        return (bits[..words], [.. astral]);
    }

    // Scale from strike pixels to device pixels, snapping to 1 within StrikeIndexFor's 0.01px tolerance.
    private static float ScaleOf(Strike strike, float deviceSize) =>
        MathF.Abs(deviceSize - strike.Px) <= 0.01f ? 1 : deviceSize / strike.Px;

    private bool HasCodepoint(uint codepoint)
    {
        if (codepoint >= 0x10000)
            return Array.BinarySearch(this.astralCodepoints, codepoint) >= 0;
        var word = codepoint >> 6;
        return word < (uint)this.codepointBits.Length
               && (this.codepointBits[word] & (1ul << (int)(codepoint & 63))) != 0;
    }

    // Prefer the target strike, then the next larger strike with the glyph, then the largest smaller one.
    private int StrikeIndexWith(int preferred, uint glyph) =>
        this.strikes[preferred].HasAll ? preferred : this.FindStrikeWith(preferred, glyph);

    private int FindStrikeWith(int preferred, uint glyph)
    {
        for (var i = preferred; i < this.strikes.Length; i++)
        {
            if (this.strikes[i].Fdt.TryGetGlyph(glyph, out _))
                return i;
        }

        for (var i = preferred - 1; i >= 0; i--)
        {
            if (this.strikes[i].Fdt.TryGetGlyph(glyph, out _))
                return i;
        }

        return preferred;
    }

    private Strike StrikeFor(float deviceSize, out float scale)
    {
        var strike = this.strikes[this.StrikeIndexFor(deviceSize)];
        scale = ScaleOf(strike, deviceSize);
        return strike;
    }

    private int StrikeIndexFor(float deviceSize)
    {
        for (var i = 0; i < this.strikes.Length; i++)
        {
            if (this.strikes[i].Px >= deviceSize - 0.01f)
                return i;
        }

        return this.strikes.Length - 1;
    }

    // Extracts a glyph bitmap from the strike's texture channel and applies synthetic bold, or returns null if unavailable.
    private GlyphBitmap? Rasterize(Strike strike, uint codepoint, float embolden)
    {
        if (!strike.Fdt.TryGetGlyph(codepoint, out var g) || g.BoundingWidth == 0 || g.BoundingHeight == 0)
            return null;

        var tex = g.TextureFileIndex < this.textures.Length ? this.textures[g.TextureFileIndex] : null;
        if (tex is null)
            return null;

        var width = tex.Width;
        var height = tex.Height;
        if (g.TextureOffsetX + g.BoundingWidth > width || g.TextureOffsetY + g.BoundingHeight > height)
            return null;

        var channel = g.TextureChannelByteIndex;
        var alpha = new byte[g.BoundingWidth * g.BoundingHeight];
        var source = tex.Pixels.Span;
        var is4444 = tex.Format == TexFile.TextureFormat.B4G4R4A4;
        for (var y = 0; y < g.BoundingHeight; y++)
        {
            for (var x = 0; x < g.BoundingWidth; x++)
            {
                var pixel = ((g.TextureOffsetY + y) * width) + g.TextureOffsetX + x;
                // B4G4R4A4 packs two 4-bit channels per byte, and * 17 expands 0..15 to 0..255.
                alpha[(y * g.BoundingWidth) + x] = is4444
                                                       ? (byte)(((source[(pixel * 2) + (channel / 2)] >>
                                                                  ((channel & 1) * 4)) & 0xF) * 17)
                                                       : source[(pixel * 4) + channel];
            }
        }

        var bitmap = new GlyphBitmap
        {
            Width = g.BoundingWidth,
            Height = g.BoundingHeight,
            OffsetX = strike.HorizontalOffset,
            OffsetY = g.CurrentOffsetY - strike.Ascent,
            Pixels = alpha,
        };
        bitmap.Embolden(embolden);
        return bitmap;
    }

    /// <summary>A game font texture, such as <c>common/font/fontN.tex</c>.</summary>
    /// <remarks>Every glyph lives in one channel of one texture.</remarks>
    /// <param name="Width">Width.</param>
    /// <param name="Height">Height.</param>
    /// <param name="Format">The format of the pixels, <see cref="TexFile.TextureFormat.B4G4R4A4"/> (as shipped) or
    /// <see cref="TexFile.TextureFormat.B8G8R8A8"/>.</param>
    /// <param name="Pixels">The pixels, row by row.</param>
    public sealed record Texture(int Width, int Height, TexFile.TextureFormat Format, ReadOnlyMemory<byte> Pixels);

    private sealed record Strike(FdtFileView Fdt, int HorizontalOffset)
    {
        // Converts 72-DPI points to 96-DPI pixels.
        public float Px { get; } = (Fdt.GlyphTableHeader.Size * 4) / 3;

        public int LineHeight { get; } = Fdt.GlyphTableHeader.LineHeight;

        public int Ascent { get; } = Fdt.GlyphTableHeader.Ascent;

        public bool HasAll { get; set; }
    }
}

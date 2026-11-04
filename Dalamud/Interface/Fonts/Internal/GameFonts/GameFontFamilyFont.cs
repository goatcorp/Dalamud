using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Fonts.Internal.GameFonts.Formats;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.GameFonts;

/// <summary>A game font family, such as AXIS, drawn from the game's font textures.</summary>
/// <remarks><ul>
/// <li>Each .fdt file is a bitmap strike.</li>
/// <li>A size is the em, which is the size of the .fdt file in pixels. Each size is drawn from the smallest strike at
/// least as large, scaled. A glyph that strike lacks comes from the nearest strike that has it.</li>
/// <li>The natural line box is the em times the line height of that strike over its em.</li>
/// <li>Glyph ids are the codepoints.</li>
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
    /// <remarks>That of the strike the em is drawn from. A natural line box takes the smallest strike whose line
    /// height is at least as large, or the largest strike.</remarks>
    public override float NaturalRatio(float size, bool isEm)
    {
        var i = isEm ? this.StrikeIndexFor(size) : 0;
        while (!isEm && i < this.strikes.Length - 1 && this.strikes[i].LineHeight < size - 0.01f)
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
            glyphs[i] = codepoints[i] != 0 && this.HasGlyph(codepoints[i]) ? codepoints[i] : 0;
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
    /// <li>Each codepoint is a glyph, except marks and joiners the family lacks.</li>
    /// <li>Each grapheme is a cluster.</li>
    /// <li>Pairs are kerned by the kerning table of the strike.</li>
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
            var k = ScaleOf(strike, deviceSize); // device pixels per strike pixel

            // Synthetic bold is applied at the strike's resolution. Nothing is drawn at size 0.
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

            // Placed at the exact pen, as the bitmap is not shifted.
            images[i] = GlyphAtlas.Shared.ToImage(image, k, new((float)subpixelSteps[i] / SubpixelStepCount, 0));
        }
    }

    // A bit per BMP codepoint up to the highest one any strike has, and the codepoints above the BMP, sorted.
    // Also marks the strikes that have every codepoint of the family.
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

    // Gets the device pixels per strike pixel. It is exactly 1 at the size of the strike, within the tolerance of
    // StrikeIndexFor.
    private static float ScaleOf(Strike strike, float deviceSize) =>
        MathF.Abs(deviceSize - strike.Px) <= 0.01f ? 1 : deviceSize / strike.Px;

    private bool HasGlyph(uint codepoint)
    {
        if (codepoint >= 0x10000)
            return Array.BinarySearch(this.astralCodepoints, codepoint) >= 0;
        var word = codepoint >> 6;
        return word < (uint)this.codepointBits.Length
               && (this.codepointBits[word] & (1ul << (int)(codepoint & 63))) != 0;
    }

    // The strike to take a glyph from. That is the preferred strike if it has the glyph, else the nearest larger one
    // that has it, else the nearest smaller one.
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

    // A glyph of a strike with synthetic bold in strike pixels, or null if it has no image.
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
        var is4444 = tex.IsB4G4R4A4;
        for (var y = 0; y < g.BoundingHeight; y++)
        {
            for (var x = 0; x < g.BoundingWidth; x++)
            {
                var pixel = ((g.TextureOffsetY + y) * width) + g.TextureOffsetX + x;
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

    /// <summary>A game font texture (common/font/fontN.tex).</summary>
    /// <remarks>Every glyph lives in one channel of one texture.</remarks>
    /// <param name="Width">Width.</param>
    /// <param name="Height">Height.</param>
    /// <param name="IsB4G4R4A4">Whether the pixels are B4G4R4A4 (as shipped) rather than B8G8R8A8.</param>
    /// <param name="Pixels">The pixels, row by row.</param>
    public sealed record Texture(int Width, int Height, bool IsB4G4R4A4, ReadOnlyMemory<byte> Pixels);

    private sealed record Strike(FdtFileView Fdt, int HorizontalOffset)
    {
        // Points to pixels at 96 DPI.
        public float Px { get; } = (Fdt.GlyphTableHeader.Size * 4) / 3;

        public int LineHeight { get; } = Fdt.GlyphTableHeader.LineHeight;

        public int Ascent { get; } = Fdt.GlyphTableHeader.Ascent;

        // Whether the strike has every codepoint of the family.
        public bool HasAll { get; set; }
    }
}

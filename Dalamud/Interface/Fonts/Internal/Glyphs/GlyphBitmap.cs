using System.Diagnostics;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>A rasterized glyph.</summary>
internal struct GlyphBitmap
{
    /// <summary>Width.</summary>
    public int Width;

    /// <summary>Height.</summary>
    public int Height;

    /// <summary>Left relative to the pen.</summary>
    public int OffsetX;

    /// <summary>Top relative to the baseline (y down).</summary>
    public int OffsetY;

    /// <summary>How <see cref="Pixels"/> stores the glyph.</summary>
    public GlyphKind Kind;

    /// <summary>The pixels, row by row, with <see cref="Channels"/> bytes per pixel.</summary>
    /// <remarks><ul>
    /// <li>Coverage for grayscale glyphs.</li>
    /// <li>Red, green and blue coverage for subpixel glyphs.</li>
    /// <li>Premultiplied red, green, blue and alpha for color glyphs. Layers in the text color are black here, and in
    /// <see cref="Mask"/>.</li>
    /// </ul></remarks>
    public byte[] Pixels;

    /// <summary>How much of the text color to add per pixel of a color glyph, row by row, or null if none.</summary>
    /// <remarks>The coverage of the layers in the text color, less what layers above them cover.</remarks>
    public byte[]? Mask;

    /// <summary>Gets the bytes per pixel of <see cref="Pixels"/>.</summary>
    public readonly int Channels => this.Kind switch
    {
        GlyphKind.Subpixel => 3,
        GlyphKind.Color => 4,
        _ => 1,
    };

    /// <summary>Widens the glyph to the right, as synthetic bold.</summary>
    /// <param name="amount">Pixels, up to <see cref="GlyphImageCache.MaxEmbolden"/>.</param>
    public void Embolden(float amount)
    {
        if (!(amount > 0) || this.Width == 0)
            return;

        // The cost grows with the amount, and the cache key does not tell larger ones apart.
        amount = MathF.Min(amount, GlyphImageCache.MaxEmbolden);
        var w = this.Width + (int)MathF.Ceiling(amount);
        this.Pixels = Embolden(this.Pixels, this.Width, w, this.Height, this.Channels, amount);
        if (this.Mask is not null)
            this.Mask = Embolden(this.Mask, this.Width, w, this.Height, 1, amount);
        this.Width = w;
    }

    // A pixel takes the most of the pixels up to the whole amount left of it, and of the one left of those, the
    // fraction. The most is kept as the row is walked, so that the cost does not grow with the amount.
    private static byte[] Embolden(byte[] src, int srcWidth, int dstWidth, int height, int channels, float amount)
    {
        // A ring of the pixels in the window that may become its most, oldest first. Their values decrease.
        const int ringMask = 31;
        Debug.Assert(amount < ringMask + 1, "The window does not fit the ring.");
        Span<int> ringX = stackalloc int[ringMask + 1];
        Span<byte> ringValue = stackalloc byte[ringMask + 1];

        var whole = (int)amount;
        var frac = amount - whole;
        var dst = new byte[dstWidth * height * channels];
        for (var y = 0; y < height; y++)
        {
            var srcRow = src.AsSpan(y * srcWidth * channels, srcWidth * channels);
            var dstRow = dst.AsSpan(y * dstWidth * channels, dstWidth * channels);
            for (var c = 0; c < channels; c++)
            {
                var first = 0;
                var count = 0;
                for (var x = 0; x < dstWidth; x++)
                {
                    if (count > 0 && ringX[first] < x - whole)
                    {
                        first = (first + 1) & ringMask;
                        count--;
                    }

                    var v = x < srcWidth ? srcRow[(x * channels) + c] : (byte)0;
                    while (count > 0 && ringValue[((first + count) - 1) & ringMask] <= v)
                        count--;
                    ringX[(first + count) & ringMask] = x;
                    ringValue[(first + count) & ringMask] = v;
                    count++;

                    int most = ringValue[first];
                    var beyond = x - whole - 1;
                    if (frac > 0 && beyond >= 0 && beyond < srcWidth)
                        most = Math.Max(most, (int)(srcRow[(beyond * channels) + c] * frac));
                    dstRow[(x * channels) + c] = (byte)most;
                }
            }
        }

        return dst;
    }
}

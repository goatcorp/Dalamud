using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Glyph images of one font or font face in the <see cref="GlyphAtlas"/>, by a key it makes.</summary>
/// <remarks>Images whose pixels are gone are dropped whenever the count doubles. That costs little per image added.
/// </remarks>
internal sealed class GlyphImageCache
{
    /// <summary>The most synthetic bold a key tells apart, in pixels.</summary>
    /// <seealso cref="Quantize8"/>
    public const float MaxEmbolden = 255 / 8f;

    // The count below which nothing is dropped.
    private const int MinSweepCount = 4096;

    private readonly Dictionary<ulong, GlyphAtlas.CachedImage> images = [];
    private int sweepCount = MinSweepCount;

    /// <summary>Gets the number of cached images, empty ones included.</summary>
    public int Count => this.images.Count;

    /// <summary>Quantizes an effect amount for a cache key, in 1/8 pixels.</summary>
    /// <param name="value">The amount.</param>
    /// <returns>The quantized amount, 0 to 255.</returns>
    public static ulong Quantize8(float value) => (ulong)Math.Clamp((int)((value * 8f) + 0.5f), 0, 255);

    /// <summary>Gets a cached image, copying it back into the atlas if its slice was cleared lately.</summary>
    /// <param name="key">The key.</param>
    /// <param name="image">The image.</param>
    /// <returns>Whether it is cached, and in the atlas.</returns>
    public bool TryGet(ulong key, out GlyphAtlas.CachedImage image)
    {
        ref var cached = ref CollectionsMarshal.GetValueRefOrNullRef(this.images, key);
        if (Unsafe.IsNullRef(ref cached) || !GlyphAtlas.Shared.TryRestore(ref cached))
        {
            image = default;
            return false;
        }

        image = cached;
        return true;
    }

    /// <summary>Packs a freshly rasterized bitmap and caches the result.</summary>
    /// <param name="key">The key.</param>
    /// <param name="bitmap">The bitmap, with synthetic bold applied, or null for an empty glyph.</param>
    /// <returns>The image.</returns>
    public GlyphAtlas.CachedImage Add(ulong key, GlyphBitmap? bitmap)
    {
        if (this.images.Count >= this.sweepCount)
            this.Sweep();

        if (bitmap is not { } b)
            return this.images[key] = GlyphAtlas.CachedImage.Empty;

        var start = FontProfiler.Begin(FontProfiler.Section.Pack);
        var image = this.images[key] = GlyphAtlas.Shared.Add(b);
        FontProfiler.End(FontProfiler.Section.Pack, start);
        return image;
    }

    // Drops the images that can only be rasterized again. Empty ones go too, as they are not bounded by the atlas.
    private void Sweep()
    {
        var atlas = GlyphAtlas.Shared;
        foreach (var (key, image) in this.images)
        {
            if (!atlas.HasPixels(image))
                this.images.Remove(key);
        }

        this.sweepCount = Math.Max(MinSweepCount, this.images.Count * 2);
    }
}

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Per-font cache mapping caller-defined keys to <see cref="GlyphAtlas.CachedImage"/> entries.</summary>
/// <remarks>Evicted entries are swept whenever the cache grows to twice its post-sweep size above a 4096-entry floor.
/// </remarks>
internal sealed class GlyphImageCache
{
    /// <summary>Maximum synthetic bold representable by <see cref="Quantize8"/>, in pixels.</summary>
    /// <seealso cref="Quantize8"/>
    public const float MaxEmbolden = 255 / 8f;

    private const int MinSweepCount = 4096;

    private readonly Dictionary<ulong, GlyphAtlas.CachedImage> images = [];
    private int sweepCount = MinSweepCount;

    /// <summary>Quantizes an effect amount for a cache key, in 1/8 pixels.</summary>
    /// <param name="value">The amount.</param>
    /// <returns>The quantized amount, <c>0</c> to <c>255</c>.</returns>
    public static ulong Quantize8(float value) => (ulong)Math.Clamp((int)((value * 8f) + 0.5f), 0, 255);

    /// <summary>Looks up a cached image, restoring it from a retired atlas slice if needed.</summary>
    /// <param name="key">The key.</param>
    /// <param name="image">The image.</param>
    /// <returns>Whether it is cached and resident in the atlas.</returns>
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
    /// <param name="bitmap">The bitmap, with synthetic bold applied, or <see langword="null"/> for an empty glyph.
    /// </param>
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

    // Prunes evicted entries as well as empty glyphs, which are not bounded by atlas capacity.
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

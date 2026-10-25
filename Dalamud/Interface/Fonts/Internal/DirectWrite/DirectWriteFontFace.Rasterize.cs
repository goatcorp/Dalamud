using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <content>Glyph rasterization via DirectWrite and Direct2D.</content>
/// <seealso cref="ConcurrentGlyphQueue"/>
internal sealed unsafe partial class DirectWriteFontFace
{
    // Maximum distinct slant values encoded in the 6-bit slant ID field of glyph cache keys.
    private const int MaxSlants = 64;

    // Maximum font line height in device pixels rasterized directly. Larger sizes stretch a MaxRasterSize bitmap.
    private const int MaxRasterSize = 2048;

    private readonly GlyphImageCache cache = new();

    // Integer device-pixel em sizes where DirectWrite renders embedded bitmaps, or null if none.
    private HashSet<int>? bitmapEms;

    /// <summary>Tests whether DirectWrite renders an embedded bitmap strike at <paramref name="deviceEm"/>.</summary>
    /// <param name="deviceEm">The em in device pixels.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the outlines by.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <returns>Whether <paramref name="deviceEm"/> is an integer matching an embedded bitmap strike.</returns>
    /// <remarks>DirectWrite only guarantees integer GDI-compatible advances at integer device-pixel em sizes.</remarks>
    public bool DrawsBitmaps(float deviceEm, float slant, bool noHinting) =>
        !noHinting && slant == 0 && this.bitmapEms is { } ems &&
        MathF.Abs(deviceEm - MathF.Round(deviceEm)) <= 0.01f && ems.Contains((int)MathF.Round(deviceEm));

    /// <summary>Produces glyph images. Called on the main thread.</summary>
    /// <param name="glyphs">The glyphs.</param>
    /// <param name="subpixelSteps">Per glyph, how far the pen is right of a whole device pixel, in
    /// 1/<see cref="Font.SubpixelStepCount"/> pixel steps.</param>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="embolden">Synthetic bold, in device pixels to widen the strokes by horizontally.</param>
    /// <param name="subpixel">Whether subpixel coverage is wanted.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the outlines by.</param>
    /// <param name="slantId">The id of <paramref name="slant"/>, from <see cref="GetOrAddSlantId"/>.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <param name="images">Receives one entry per glyph.</param>
    public void GetImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, float slant, int slantId, bool noHinting, Span<GlyphImage> images)
    {
        this.Deferral.DrainOncePerFrame();
        var sizeQ = (int)((size * density * Font.SizeStepsPerPixel) + 0.5f);
        var bitmaps = this.DrawsBitmaps(this.EmSize(size * density), slant, noHinting);
        subpixel &= SubpixelSettings.Enabled;

        // Oversized glyphs are rasterized at MaxRasterSize without subpixel positioning or ClearType coverage, then scaled up.
        var rasterQ = Math.Min(sizeQ, MaxRasterSize * Font.SizeStepsPerPixel);
        var stretch = (float)sizeQ / rasterQ;
        var stretched = rasterQ != sizeQ;
        if (stretched)
        {
            (sizeQ, subpixel) = (rasterQ, false);

            // Scale embolden down to rasterized pixel space so stretching restores the requested width.
            embolden /= stretch;
        }

        // Color fonts are treated as costly because checking per-glyph color layers is nearly as expensive as rendering them.
        var costly = this.HasColorGlyphs ||
                     sizeQ > GlyphRasterBudget.MaxInlineSize * Font.SizeStepsPerPixel;

        // Cache key bits are glyph 0..15, quarter-pixel size 16..29, subpixel step 30..31, embolden 32..39, subpixel 40, noHinting 41, and slantId 58..63.
        var common = ((ulong)sizeQ << 16)
                     | (GlyphImageCache.Quantize8(embolden) << 32)
                     | (subpixel ? 1ul << 40 : 0)
                     | (noHinting ? 1ul << 41 : 0)
                     | ((ulong)slantId << 58);
        for (var i = 0; i < glyphs.Length; i++)
        {
            var step = subpixelSteps[i] & (Font.SubpixelStepCount - 1);
            var subX = stretched || bitmaps ? 0 : step;
            var key = glyphs[i] | ((ulong)subX << 30) | common;
            if (!this.cache.TryGet(key, out var image))
            {
                var request = new GlyphRasterRequest(
                    this,
                    key,
                    glyphs[i],
                    (float)sizeQ / Font.SizeStepsPerPixel,
                    (float)subX / Font.SubpixelStepCount,
                    subpixel,
                    embolden,
                    slant,
                    noHinting);
                if (this.Deferral.TryDefer(request, costly))
                {
                    images[i] = default;
                    continue;
                }

                var start = FontProfiler.Begin(FontProfiler.Section.Rasterize);
                var measured = Stopwatch.GetTimestamp();
                var bitmap = this.Rasterize(request);
                GlyphRasterBudget.CountRasterize(Stopwatch.GetTimestamp() - measured);
                FontProfiler.End(FontProfiler.Section.Rasterize, start);
                image = this.cache.Add(key, bitmap);
            }

            // Unstretched bitmaps bake in the subpixel shift. Stretched bitmaps are rasterized at step 0 and shifted in the quad.
            images[i] = stretched
                            ? GlyphAtlas.Shared.ToImage(image, stretch, new((float)step / Font.SubpixelStepCount, 0))
                            : GlyphAtlas.Shared.ToImage(image, 1f, default);
            images[i].CorrectCoverage = true;
            images[i].WholePixel = bitmaps;
        }
    }

    /// <summary>Rasterizes a glyph, with its synthetic bold. Called on any thread, concurrently.</summary>
    /// <param name="request">What to rasterize.</param>
    /// <returns>The bitmap, or null if the glyph has no image.</returns>
    public GlyphBitmap? Rasterize(in GlyphRasterRequest request)
    {
        if (!this.Rasterize(
                (ushort)request.Glyph,
                request.DeviceSize,
                request.SubpixelX,
                request.Subpixel,
                request.Slant,
                request.NoHinting,
                out var bitmap))
        {
            return null;
        }

        bitmap.Embolden(request.Embolden);
        return bitmap;
    }

    /// <summary>Caches a glyph rasterized on the thread pool. Called on the main thread.</summary>
    /// <param name="key">The key of the glyph image.</param>
    /// <param name="bitmap">The bitmap, or null if the glyph has no image.</param>
    public void AddRasterized(ulong key, GlyphBitmap? bitmap)
    {
        // Text drawn synchronously in the meantime may have rasterized it.
        if (!this.cache.TryGet(key, out _))
            _ = this.cache.Add(key, bitmap);
    }

    // Probes EBLC strikes to find integer em sizes that DirectWrite renders as embedded bitmaps.
    // Embedded bitmaps ignore a 0.5px subpixel shift, whereas outlines resample.
    private HashSet<int>? FindBitmapEms()
    {
        void* table;
        uint length;
        void* context;
        BOOL exists;
        if (this.HasColorGlyphs ||
            this.face->TryGetFontTable(
                    BinaryPrimitives.ReverseEndianness(0x45424C43u /* EBLC */), &table, &length, &context, &exists)
                .FAILED || !exists)
        {
            return null;
        }

        HashSet<int>? ems = null;
        try
        {
            var eblc = new ReadOnlySpan<byte>(table, (int)length);
            var count = eblc.Length >= 8 ? BinaryPrimitives.ReadUInt32BigEndian(eblc[4..]) : 0;
            for (var i = 0; i < count && 8 + ((i + 1) * 48) <= eblc.Length; i++)
            {
                // 48-byte BitmapSize record with indexSubTableArrayOffset at +0, numberOfIndexSubTables at +8, and ppemY at +45.
                var strike = eblc.Slice(8 + (i * 48), 48);
                var array = BinaryPrimitives.ReadUInt32BigEndian(strike);
                var subtables = BinaryPrimitives.ReadUInt32BigEndian(strike[8..]);
                var size = strike[45] / this.EmSize(1);

                for (var k = 0L; k < subtables && array + (k * 8) + 2 <= eblc.Length; k++)
                {
                    var glyph = BinaryPrimitives.ReadUInt16BigEndian(eblc[(int)(array + (k * 8))..]);
                    if (!this.Rasterize(glyph, size, 0, false, 0, false, out var a))
                        continue;
                    if (this.Rasterize(glyph, size, 0.5f, false, 0, false, out var b) &&
                        a.Width == b.Width && a.Height == b.Height && a.Pixels.AsSpan().SequenceEqual(b.Pixels))
                    {
                        (ems ??= []).Add(strike[45]);
                    }

                    break;
                }
            }
        }
        finally
        {
            this.face->ReleaseFontTable(context);
        }

        return ems;
    }

    // Conservative render canvas around the pen for color glyphs (line box by advance plus 0.25em margin), since color layers lack outline bounds.
    private RECT ColorCanvas(ushort glyph, float deviceSize, float slant)
    {
        DWRITE_GLYPH_METRICS m;
        if (this.face->GetDesignGlyphMetrics(&glyph, 1, &m, false).FAILED)
            m = default;
        var scale = this.Scale(deviceSize);
        var margin = (this.unitsPerEm * scale) / 4;
        var top = (this.designAscent * scale) + margin;
        var bottom = (this.designDescent * scale) + margin;
        return new(
            -(int)MathF.Ceiling(margin + (bottom * slant)),
            -(int)MathF.Ceiling(top),
            (int)MathF.Ceiling((m.advanceWidth * scale) + margin + (top * slant)),
            (int)MathF.Ceiling(bottom));
    }

    // Render canvas from design glyph metrics plus a 0.125em margin for the Direct2D outline fallback.
    private RECT OutlineCanvas(ushort glyph, float deviceSize, float slant)
    {
        DWRITE_GLYPH_METRICS m;
        if (this.face->GetDesignGlyphMetrics(&glyph, 1, &m, false).FAILED ||
            m.advanceWidth - m.leftSideBearing - m.rightSideBearing <= 0 ||
            m.advanceHeight - m.topSideBearing - m.bottomSideBearing <= 0)
        {
            return default;
        }

        var scale = this.Scale(deviceSize);
        var margin = (this.unitsPerEm * scale) / 8;
        var left = (m.leftSideBearing * scale) - margin;
        var right = ((m.advanceWidth - m.rightSideBearing) * scale) + margin;
        var top = ((m.topSideBearing - m.verticalOriginY) * scale) - margin;
        var bottom = ((m.advanceHeight - m.bottomSideBearing - m.verticalOriginY) * scale) + margin;
        return new(
            (int)MathF.Floor(left - Math.Max(top * slant, bottom * slant)),
            (int)MathF.Floor(top),
            (int)MathF.Ceiling(right + 1 - Math.Min(top * slant, bottom * slant)),
            (int)MathF.Ceiling(bottom));
    }

    private bool Rasterize(
        ushort glyph, float deviceSize, float subpixelX, bool subpixel, float slant, bool noHinting,
        out GlyphBitmap bitmap)
    {
        bitmap = default;
        var emSize = this.EmSize(deviceSize);
        if (this.HasColorGlyphs &&
            this.ColorGlyphs.TryRasterize(
                this.face,
                glyph,
                emSize,
                subpixelX,
                slant,
                this.ColorCanvas(glyph, deviceSize, slant),
                out bitmap))
        {
            return true;
        }

        var transform = new DWRITE_MATRIX { m11 = 1, m21 = -slant, m22 = 1 };

        var advance = 0f;
        var run = new DWRITE_GLYPH_RUN
        {
            fontFace = this.face,
            fontEmSize = emSize,
            glyphCount = 1,
            glyphIndices = &glyph,
            glyphAdvances = &advance,
        };

        var mode = DWRITE_RENDERING_MODE1.DWRITE_RENDERING_MODE1_NATURAL_SYMMETRIC;
        var fit = DWRITE_GRID_FIT_MODE.DWRITE_GRID_FIT_MODE_DISABLED;
        using var face3 = default(ComPtr<IDWriteFontFace3>);
        if (!noHinting && this.face->QueryInterface(__uuidof<IDWriteFontFace3>(), (void**)face3.GetAddressOf()).SUCCEEDED)
        {
            face3.Get()->GetRecommendedRenderingMode(
                emSize,
                96,
                96,
                slant == 0 ? null : &transform,
                false,
                DWRITE_OUTLINE_THRESHOLD.DWRITE_OUTLINE_THRESHOLD_ANTIALIASED,
                DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL,
                DirectWriteUtilities.RenderingParams,
                &mode,
                &fit);
        }

        using var analysis = default(ComPtr<IDWriteGlyphRunAnalysis>);
        if (DirectWriteUtilities.Factory->CreateGlyphRunAnalysis(
                &run,
                slant == 0 ? null : &transform,
                mode is >= DWRITE_RENDERING_MODE1.DWRITE_RENDERING_MODE1_ALIASED
                    and <= DWRITE_RENDERING_MODE1.DWRITE_RENDERING_MODE1_NATURAL_SYMMETRIC
                    ? (DWRITE_RENDERING_MODE)mode
                    : DWRITE_RENDERING_MODE.DWRITE_RENDERING_MODE_NATURAL_SYMMETRIC,
                DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL,
                fit,
                subpixel
                    ? DWRITE_TEXT_ANTIALIAS_MODE.DWRITE_TEXT_ANTIALIAS_MODE_CLEARTYPE
                    : DWRITE_TEXT_ANTIALIAS_MODE.DWRITE_TEXT_ANTIALIAS_MODE_GRAYSCALE,
                subpixelX,
                0,
                analysis.GetAddressOf()).FAILED)
        {
            return false;
        }

        // With grayscale antialiasing, the "aliased" texture holds 8-bit coverage. ClearType's holds it per subpixel,
        // in red, green, blue order.
        var type = subpixel
                       ? DWRITE_TEXTURE_TYPE.DWRITE_TEXTURE_CLEARTYPE_3x1
                       : DWRITE_TEXTURE_TYPE.DWRITE_TEXTURE_ALIASED_1x1;
        RECT bounds;
        if (analysis.Get()->GetAlphaTextureBounds(type, &bounds).FAILED)
            return false;
        var w = bounds.right - bounds.left;
        var h = bounds.bottom - bounds.top;

        // Subpixel glyphs take 3 texels per pixel. Fall back to grayscale if DirectWrite returns empty bounds or the bitmap exceeds MaxPackedSize.
        if (subpixel && (w <= 0 || h <= 0 || !GlyphAtlas.Packs(GlyphKind.Subpixel, w, h)))
            return this.Rasterize(glyph, deviceSize, subpixelX, false, slant, noHinting, out bitmap);

        // Empty DirectWrite bounds mean either a blank glyph or one too large for DirectWrite, so try Direct2D when design metrics have ink.
        if (w <= 0 || h <= 0)
        {
            var canvas = this.OutlineCanvas(glyph, deviceSize, slant);
            return canvas.right > canvas.left &&
                   this.ColorGlyphs.TryRasterizeOutline(this.face, glyph, emSize, subpixelX, slant, canvas, out bitmap);
        }

        var data = new byte[w * h * (subpixel ? 3 : 1)];
        fixed (byte* pData = data)
        {
            if (analysis.Get()->CreateAlphaTexture(type, &bounds, pData, (uint)data.Length).FAILED)
                return false;
        }

        // Subpixel coverage is red first. The shader reverses it for monitors in blue, green, red order.
        bitmap = new()
        {
            Width = w,
            Height = h,
            OffsetX = bounds.left,
            OffsetY = bounds.top,
            Kind = subpixel ? GlyphKind.Subpixel : GlyphKind.Grayscale,
            Pixels = data,
        };
        return true;
    }
}

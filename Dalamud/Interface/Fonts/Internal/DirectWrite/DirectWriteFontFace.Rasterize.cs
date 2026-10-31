using System.Buffers.Binary;
using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Sources;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Glyph images from DirectWrite or Direct2D.</summary>
/// <seealso cref="CreateDeferral"/>
internal sealed unsafe partial class DirectWriteFontFace
{
    // The most slants the image keys of a face tell apart.
    private const int MaxSlants = 64;

    // The largest em size in device pixels rasterized. Glyphs of it fit GlyphAtlas.MaxImageSize, but for the widest.
    private const int MaxRasterSize = 2048;

    private readonly GlyphImageCache cache = new();

    // The whole ems in device pixels at which glyphs are drawn from embedded bitmaps, or null if at none.
    private HashSet<int>? bitmapEms;

    /// <summary>Makes a queue that rasterizes glyphs of DirectWrite faces on the thread pool.</summary>
    /// <returns>The queue, for the faces of one owner to share.</returns>
    public static ConcurrentGlyphQueue<GlyphRasterRequest> CreateDeferral() =>
        new(static r => r.Face.Rasterize(r), static (r, bitmap) => r.Face.AddRasterized(r.Key, bitmap));

    /// <summary>Gets whether glyphs are drawn from embedded bitmaps, which cannot be placed between pixels.</summary>
    /// <param name="deviceEm">The em in device pixels.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the outlines by.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <returns>Whether the em is whole, and DirectWrite draws a strike of bitmaps there.</returns>
    /// <remarks>GDI-compatible advances are whole pixels only at whole ems. So only those are told.</remarks>
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
    /// <param name="slantId">The id of <paramref name="slant"/>, from <see cref="SlantIdOf"/>.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <param name="images">Receives one entry per glyph.</param>
    public void GetImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, float slant, int slantId, bool noHinting, Span<GlyphImage> images)
    {
        this.Deferral?.DrainOncePerFrame();
        var sizeQ = (int)((size * density * Font.SizeStepsPerPixel) + 0.5f);
        var bitmaps = this.DrawsBitmaps(this.EmSize(size * density), slant, noHinting);
        subpixel &= SubpixelSettings.Enabled;

        // Larger glyphs are stretched from images of MaxRasterSize, at whole pixels. Subpixel coverage would not survive
        // the stretch.
        var rasterQ = Math.Min(sizeQ, MaxRasterSize * Font.SizeStepsPerPixel);
        var stretch = (float)sizeQ / rasterQ;
        var stretched = rasterQ != sizeQ;
        if (stretched)
        {
            (sizeQ, subpixel) = (rasterQ, false);
            embolden /= stretch;
        }

        // Glyphs of a color font may have many layers. Telling which have color takes as long as drawing them.
        var costly = (this.HasColorGlyphs && this.ColorGlyphs is not null) ||
                     sizeQ > GlyphRasterBudget.MaxInlineSize * Font.SizeStepsPerPixel;

        // The bits are glyph 0-15, size in quarter pixels 16-29, step 30-31, embolden 32-39, subpixel 40, no hinting
        // 41, slant 58-63. Sizes are at most MaxRasterSize, so 14 bits hold them.
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
                if (this.Deferral?.TryDefer(this, key, request, costly) == true)
                {
                    images[i] = default;
                    continue;
                }

                var start = FontProfiler.Begin(FontProfiler.Section.Rasterize);
                var measured = GlyphRasterBudget.Measure();
                var bitmap = this.Rasterize(request);
                GlyphRasterBudget.EndMeasure(measured);
                FontProfiler.End(FontProfiler.Section.Rasterize, start);
                image = this.cache.Add(key, bitmap);
            }

            // The subpixel shift is in the bitmap, which is relative to the pen minus SubpixelX, as asked. A stretched
            // image is at the whole pixel, and is shifted instead.
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

    // Caches a glyph rasterized on the thread pool.
    private void AddRasterized(ulong key, GlyphBitmap? bitmap)
    {
        // Text drawn synchronously in the meantime may have rasterized it.
        if (!this.cache.TryGet(key, out _))
            _ = this.cache.Add(key, bitmap);
    }

    // Finds the strikes of the EBLC table that DirectWrite draws. A strike is drawn if the first glyph with an image
    // of its index subtables looks the same half a pixel right. Outlines would be resampled.
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
        var eblc = new ReadOnlySpan<byte>(table, (int)length);
        var count = eblc.Length >= 8 ? BinaryPrimitives.ReadUInt32BigEndian(eblc[4..]) : 0;
        for (var i = 0; i < count && 8 + ((i + 1) * 48) <= eblc.Length; i++)
        {
            var strike = eblc.Slice(8 + (i * 48), 48);
            var array = BinaryPrimitives.ReadUInt32BigEndian(strike);
            var subtables = BinaryPrimitives.ReadUInt32BigEndian(strike[8..]);
            var size = strike[45] / this.EmSize(1);
            for (var k = 0u; k < subtables && array + (k * 8) + 2 <= eblc.Length; k++)
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

        this.face->ReleaseFontTable(context);
        return ems;
    }

    // Where a color glyph may draw, relative to the pen. It is the line and the advance, with a quarter em around them.
    // Color glyphs have no outline to take bounds from.
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

    // Where the outline of a glyph draws, relative to the pen, by its design metrics.
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
        // Without a color rasterizer, or if it fails, color glyphs fall back to their outlines.
        if (this.HasColorGlyphs &&
            this.ColorGlyphs is not null &&
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

        // Subpixel glyphs take three texels per pixel. Ones too large to be packed are in grayscale, as they would take
        // a texture of their own. DirectWrite has no subpixel image of large ones at all.
        if (subpixel && (w <= 0 || h <= 0 || !GlyphAtlas.Packs(GlyphKind.Subpixel, w, h)))
            return this.Rasterize(glyph, deviceSize, subpixelX, false, slant, noHinting, out bitmap);

        // Too large for DirectWrite to rasterize directly, so use Direct2D.
        if (w <= 0 || h <= 0)
        {
            var canvas = this.OutlineCanvas(glyph, deviceSize, slant);
            return canvas.right > canvas.left && this.ColorGlyphs is { } rasterizer &&
                   rasterizer.TryRasterizeOutline(this.face, glyph, emSize, subpixelX, slant, canvas, out bitmap);
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

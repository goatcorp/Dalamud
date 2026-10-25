using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Rasterizes color glyphs (COLR, SVG, bitmap) and large grayscale outlines with Direct2D.</summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows 10+")]
internal sealed unsafe class ColorGlyphRasterizer : IDisposable
{
    private const DWRITE_GLYPH_IMAGE_FORMATS ColorFormats =
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_SVG |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PNG |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_JPEG |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TIFF |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PREMULTIPLIED_B8G8R8A8;

    private const DWRITE_GLYPH_IMAGE_FORMATS OutlineFormats =
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TRUETYPE |
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_CFF;

    // Kept separate from ColorFormats because COLRv1 paint trees require Windows 11.
    private const DWRITE_GLYPH_IMAGE_FORMATS PaintTreeFormat =
        DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR_PAINT_TREE;

    private const int MaxIdleFactories = 4;

    private static readonly ModuleLog Log = new(typeof(ColorGlyphRasterizer));

    private readonly ReaderWriterLockSlim disposeLock = new();
    private readonly Lock syncRoot = new();
    private readonly Stack<nint> idleFactories = new();

    private ComPtr<IWICImagingFactory> wicFactory;

    private ComPtr<IDWriteFactory8> paintTreeFactory;
    private volatile bool initialized;
    private volatile bool noPaintTrees;
    private bool disposed;

    /// <summary>Tests whether a font has color glyphs.</summary>
    /// <param name="face">The font face.</param>
    /// <returns>Whether it has a color format.</returns>
    public static bool HasColorGlyphs(IDWriteFontFace* face)
    {
        using var face4 = default(ComPtr<IDWriteFontFace4>);
        return face->QueryInterface(__uuidof<IDWriteFontFace4>(), (void**)face4.GetAddressOf()).SUCCEEDED &&
               (face4.Get()->GetGlyphImageFormats() & (ColorFormats | PaintTreeFormat)) != 0;
    }

    /// <summary>Draws a glyph if it is a color glyph.</summary>
    /// <param name="face">The font face.</param>
    /// <param name="glyph">The glyph index.</param>
    /// <param name="emSize">The em size in pixels.</param>
    /// <param name="subpixelX">Subpixel horizontal pen offset in <c>[0, 1)</c>.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the glyph by.</param>
    /// <param name="canvas">Target rectangle relative to the baseline pen. Pixels outside are clipped.</param>
    /// <param name="bitmap">The glyph, trimmed to its ink.</param>
    /// <returns>Whether the glyph is a color glyph with visible pixels, or <see langword="false"/> once disposed.</returns>
    public bool TryRasterize(
        IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant, RECT canvas,
        out GlyphBitmap bitmap)
    {
        bitmap = default;
        var w = canvas.right - canvas.left;
        var h = canvas.bottom - canvas.top;
        if (w <= 0 || h <= 0)
            return false;

        byte[]? black, white;
        this.disposeLock.EnterReadLock();
        ID2D1Factory* factory = null;
        try
        {
            if (!this.EnsureInitialized())
                return false;

            factory = this.RentFactory();
            black = this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, 0f, out var usesText);

            // If no layer uses the foreground brush, black and white passes are identical and no mask is needed.
            white = black is null || !usesText
                        ? black
                        : this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, 1f, out _);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "A color glyph could not be drawn. Its outline is drawn instead");
            return false;
        }
        finally
        {
            this.ReturnFactory(factory);
            this.disposeLock.ExitReadLock();
        }

        if (black is null || white is null)
            return false;

        // The black pass yields premultiplied fixed-color layers in BGRA order.
        // The white pass adds the foreground mask equally to R, G, and B, so white.G - black.G recovers the mask.
        var hasMask = false;
        int x0 = w, y0 = h, x1 = 0, y1 = 0;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var i = ((y * w) + x) * 4;
                var mask = white[i + 1] - black[i + 1];
                hasMask |= mask > 0;
                if (black[i + 3] != 0 || mask > 0)
                    (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x + 1), Math.Max(y1, y + 1));
            }
        }

        if (x0 >= x1 || y0 >= y1)
            return false;

        var tw = x1 - x0;
        var th = y1 - y0;
        var rgba = new byte[tw * th * 4];
        var maskPixels = hasMask ? new byte[tw * th] : null;
        for (var y = 0; y < th; y++)
        {
            for (var x = 0; x < tw; x++)
            {
                var i = (((y + y0) * w) + x + x0) * 4;
                var o = (y * tw) + x;
                rgba[(o * 4) + 0] = black[i + 2];
                rgba[(o * 4) + 1] = black[i + 1];
                rgba[(o * 4) + 2] = black[i + 0];
                rgba[(o * 4) + 3] = black[i + 3];
                maskPixels?[o] = (byte)Math.Max(0, white[i + 1] - black[i + 1]);
            }
        }

        bitmap = new()
        {
            Width = tw,
            Height = th,
            OffsetX = canvas.left + x0,
            OffsetY = canvas.top + y0,
            Kind = GlyphKind.Color,
            Pixels = rgba,
            Mask = maskPixels,
        };
        return true;
    }

    /// <summary>Fills the outline of a glyph in grayscale.</summary>
    /// <param name="face">The font face.</param>
    /// <param name="glyph">The glyph index.</param>
    /// <param name="emSize">The em size in pixels.</param>
    /// <param name="subpixelX">Subpixel horizontal pen offset in <c>[0, 1)</c>.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the glyph by.</param>
    /// <param name="canvas">Target rectangle relative to the baseline pen. Pixels outside are clipped.</param>
    /// <param name="bitmap">The glyph, trimmed to its ink.</param>
    /// <returns>Whether the glyph produced visible pixels, or <see langword="false"/> once disposed.</returns>
    /// <remarks>For glyphs too large for DirectWrite's glyph run analysis. Direct2D fills outlines of any size.
    /// </remarks>
    public bool TryRasterizeOutline(
        IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant, RECT canvas,
        out GlyphBitmap bitmap)
    {
        bitmap = default;
        var w = canvas.right - canvas.left;
        var h = canvas.bottom - canvas.top;
        if (w <= 0 || h <= 0)
            return false;

        byte[]? coverage;
        this.disposeLock.EnterReadLock();
        ID2D1Factory* factory = null;
        try
        {
            if (!this.EnsureInitialized())
                return false;

            factory = this.RentFactory();
            coverage = this.RenderOutline(factory, face, glyph, emSize, subpixelX, slant, canvas);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "The outline of a large glyph could not be drawn. It is not drawn");
            return false;
        }
        finally
        {
            this.ReturnFactory(factory);
            this.disposeLock.ExitReadLock();
        }

        if (coverage is null)
            return false;

        int x0 = w, y0 = h, x1 = 0, y1 = 0;
        for (var y = 0; y < h; y++)
        {
            var row = coverage.AsSpan(y * w, w);
            var first = row.IndexOfAnyExcept((byte)0);
            if (first < 0)
                continue;
            x0 = Math.Min(x0, first);
            x1 = Math.Max(x1, row.LastIndexOfAnyExcept((byte)0) + 1);
            y0 = Math.Min(y0, y);
            y1 = y + 1;
        }

        if (x0 >= x1 || y0 >= y1)
            return false;

        var tw = x1 - x0;
        var th = y1 - y0;
        var pixels = new byte[tw * th];
        for (var y = 0; y < th; y++)
            coverage.AsSpan(((y + y0) * w) + x0, tw).CopyTo(pixels.AsSpan(y * tw));

        bitmap = new()
        {
            Width = tw,
            Height = th,
            OffsetX = canvas.left + x0,
            OffsetY = canvas.top + y0,
            Kind = GlyphKind.Grayscale,
            Pixels = pixels,
        };
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.disposeLock.EnterWriteLock();
        try
        {
            this.disposed = true;
            while (this.idleFactories.TryPop(out var factory))
                ((ID2D1Factory*)factory)->Release();
            this.wicFactory.Reset();
            this.paintTreeFactory.Reset();
        }
        finally
        {
            this.disposeLock.ExitWriteLock();
        }
    }

    // Draws one color glyph layer and returns whether it uses the caller's text brush.
    [SupportedOSPlatform("windows10.0")]
    private static bool DrawLayer(
        ID2D1DeviceContext4* dc, ID2D1DeviceContext7* paintTreeContext, DWRITE_COLOR_GLYPH_RUN1* layer,
        ID2D1Brush* textBrush, ID2D1SolidColorBrush* layerBrush)
    {
        var b = &layer->Base;
        var origin = new D2D_POINT_2F { x = b->baselineOriginX, y = b->baselineOriginY };
        switch (layer->glyphImageFormat)
        {
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR_PAINT_TREE:
                paintTreeContext->DrawPaintGlyphRun(origin, &b->glyphRun, textBrush, 0, layer->measuringMode);
                return true;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_SVG:
                dc->DrawSvgGlyphRun(origin, &b->glyphRun, textBrush, null, 0, layer->measuringMode);
                return true;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PNG:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_JPEG:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TIFF:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PREMULTIPLIED_B8G8R8A8:
                dc->DrawColorBitmapGlyphRun(layer->glyphImageFormat, origin, &b->glyphRun, layer->measuringMode);
                return false;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_NONE:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TRUETYPE:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_CFF:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR:
            default:
                ID2D1Brush* brush;
                var usesText = b->paletteIndex == 0xFFFF;
                if (usesText)
                {
                    brush = textBrush;
                }
                else
                {
                    var color = b->runColor;
                    layerBrush->SetColor(&color);
                    brush = (ID2D1Brush*)layerBrush;
                }

                dc->DrawGlyphRun(origin, &b->glyphRun, brush, layer->measuringMode);
                return usesText;
        }
    }

    private static ComPtr<IWICImagingFactory> CreateWicFactory()
    {
        const uint context = (uint)CLSCTX.CLSCTX_INPROC_SERVER;
        var clsid = CLSID.CLSID_WICImagingFactory;
        var iid = IID.IID_IWICImagingFactory;
        var factory = default(ComPtr<IWICImagingFactory>);
        var hr = CoCreateInstance(&clsid, null, context, &iid, (void**)factory.GetAddressOf());
        if (hr == CO.CO_E_NOTINITIALIZED)
        {
            // Keep process-wide COM MTA initialized when called from a thread-pool worker.
            CO_MTA_USAGE_COOKIE cookie;
            _ = CoIncrementMTAUsage(&cookie);
            hr = CoCreateInstance(&clsid, null, context, &iid, (void**)factory.GetAddressOf());
        }

        hr.ThrowOnError();
        return factory;
    }

    private static byte[]? ReadPixels(IWICBitmap* bitmap, RECT canvas, int bytesPerPixel)
    {
        var stride = (canvas.right - canvas.left) * bytesPerPixel;
        var pixels = new byte[stride * (canvas.bottom - canvas.top)];
        fixed (byte* p = pixels)
            return bitmap->CopyPixels(null, (uint)stride, (uint)pixels.Length, p).SUCCEEDED ? pixels : null;
    }

    private bool EnsureInitialized()
    {
        if (this.disposed)
            return false;
        if (this.initialized)
            return true;

        using (this.syncRoot.EnterScope())
        {
            if (this.initialized)
                return true;
            if (this.wicFactory.IsEmpty())
                this.wicFactory = CreateWicFactory();

            // Paint trees (COLRv1) come with Windows 11.
            IDWriteFactory8* factory8;
            if (DirectWriteUtilities.Factory->QueryInterface(__uuidof<IDWriteFactory8>(), (void**)&factory8).SUCCEEDED)
                this.paintTreeFactory.Attach(factory8);

            this.initialized = true;
            return true;
        }
    }

    // Rents a pooled single-threaded Direct2D factory so concurrent workers do not contend on a shared factory lock.
    private ID2D1Factory* RentFactory()
    {
        using (this.syncRoot.EnterScope())
        {
            if (this.idleFactories.TryPop(out var idle))
                return (ID2D1Factory*)idle;
        }

        var options = new D2D1_FACTORY_OPTIONS { debugLevel = D2D1_DEBUG_LEVEL.D2D1_DEBUG_LEVEL_NONE };
        ID2D1Factory* factory;
        fixed (Guid* iid = &IID.IID_ID2D1Factory)
        {
            DirectX.D2D1CreateFactory(
                D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED,
                iid,
                &options,
                (void**)&factory).ThrowOnError();
        }

        return factory;
    }

    private void ReturnFactory([AllowNull] ID2D1Factory* factory)
    {
        if (factory is null)
            return;

        using (this.syncRoot.EnterScope())
        {
            if (this.idleFactories.Count < MaxIdleFactories)
            {
                this.idleFactories.Push((nint)factory);
                return;
            }
        }

        factory->Release();
    }

    // Renders color layers with foreground intensity text (0 to 1) into premultiplied BGRA, or returns null if not a color glyph.
    // Sets usesText if any layer may use the foreground brush.
    private byte[]? Render(
        ID2D1Factory* factory, IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant,
        RECT canvas, float text, out bool usesText)
    {
        usesText = false;

        // Per-glyph GetGlyphImageFormats reports NONE for COLRv0 glyphs, so let TranslateColorGlyphRun detect color glyphs.
        var advance = 0f;
        var run = new DWRITE_GLYPH_RUN
        {
            fontFace = face,
            fontEmSize = emSize,
            glyphCount = 1,
            glyphIndices = &glyph,
            glyphAdvances = &advance,
        };
        var origin = new D2D_POINT_2F { x = subpixelX - canvas.left, y = -canvas.top };
        using var layers = default(ComPtr<IDWriteColorGlyphRunEnumerator1>);
        var paintTrees = !this.noPaintTrees && !this.paintTreeFactory.IsEmpty();
        var translated = paintTrees
                             ? this.paintTreeFactory.Get()->TranslateColorGlyphRun(
                                 origin,
                                 &run,
                                 null,
                                 ColorFormats | OutlineFormats | PaintTreeFormat,
                                 DWRITE_PAINT_FEATURE_LEVEL.DWRITE_PAINT_FEATURE_LEVEL_COLR_V1,
                                 DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL,
                                 null,
                                 0,
                                 layers.GetAddressOf())
                             : DirectWriteUtilities.Factory->TranslateColorGlyphRun(
                                 origin,
                                 &run,
                                 null,
                                 ColorFormats | OutlineFormats,
                                 DWRITE_MEASURING_MODE.DWRITE_MEASURING_MODE_NATURAL,
                                 null,
                                 0,
                                 layers.GetAddressOf());
        if (translated.FAILED)
            return null;

        using var wicBitmap = default(ComPtr<IWICBitmap>);
        using var target = default(ComPtr<ID2D1RenderTarget>);
        using var context = default(ComPtr<ID2D1DeviceContext4>);
        using var paintTreeContext = default(ComPtr<ID2D1DeviceContext7>);
        if (!this.CreateTarget(
                factory,
                canvas,
                GUID.GUID_WICPixelFormat32bppPBGRA,
                DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                wicBitmap.GetAddressOf(),
                target.GetAddressOf()) ||
            target.As(&context).FAILED)
        {
            return null;
        }

        // IDWriteFactory8 can exist without ID2D1DeviceContext7 on the WIC render target, so disable COLRv1 paint trees and retry.
        if (paintTrees && target.As(&paintTreeContext).FAILED)
        {
            this.noPaintTrees = true;
            return this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, text, out usesText);
        }

        var dc = context.Get();
        dc->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE.D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE);

        // Shear horizontally around the baseline (y down).
        var shear = new D2D_MATRIX_3X2_F { _11 = 1, _21 = -slant, _22 = 1, _31 = slant * origin.y };
        dc->SetTransform(&shear);
        var textColor = new DXGI_RGBA { r = text, g = text, b = text, a = 1 };
        using var textBrush = default(ComPtr<ID2D1SolidColorBrush>);
        using var layerBrush = default(ComPtr<ID2D1SolidColorBrush>);
        dc->CreateSolidColorBrush(&textColor, textBrush.GetAddressOf()).ThrowOnError();
        dc->CreateSolidColorBrush(&textColor, layerBrush.GetAddressOf()).ThrowOnError();

        var transparent = default(DXGI_RGBA);
        dc->BeginDraw();
        dc->Clear(&transparent);
        while (true)
        {
            BOOL hasRun;
            if (layers.Get()->MoveNext(&hasRun).FAILED || !hasRun)
                break;
            DWRITE_COLOR_GLYPH_RUN1* layer;
            if (layers.Get()->GetCurrentRun(&layer).FAILED)
                break;
            usesText |= DrawLayer(dc, paintTreeContext.Get(), layer, (ID2D1Brush*)textBrush.Get(), layerBrush.Get());
        }

        return dc->EndDraw(null, null).SUCCEEDED ? ReadPixels(wicBitmap.Get(), canvas, 4) : null;
    }

    // Rasterizes the glyph outline into an 8-bit coverage buffer, or returns null on failure.
    private byte[]? RenderOutline(
        ID2D1Factory* factory, IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant,
        RECT canvas)
    {
        using var wicBitmap = default(ComPtr<IWICBitmap>);
        using var target = default(ComPtr<ID2D1RenderTarget>);
        if (!this.CreateTarget(
                factory,
                canvas,
                GUID.GUID_WICPixelFormat8bppAlpha,
                DXGI_FORMAT.DXGI_FORMAT_A8_UNORM,
                wicBitmap.GetAddressOf(),
                target.GetAddressOf()))
        {
            return null;
        }

        var rt = target.Get();
        rt->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE.D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE);

        var origin = new D2D_POINT_2F { x = subpixelX - canvas.left, y = -canvas.top };
        var shear = new D2D_MATRIX_3X2_F { _11 = 1, _21 = -slant, _22 = 1, _31 = slant * origin.y };
        rt->SetTransform(&shear);
        var white = new DXGI_RGBA { r = 1, g = 1, b = 1, a = 1 };
        using var brush = default(ComPtr<ID2D1SolidColorBrush>);
        rt->CreateSolidColorBrush(&white, null, brush.GetAddressOf()).ThrowOnError();

        var advance = 0f;
        var run = new DWRITE_GLYPH_RUN
        {
            fontFace = face,
            fontEmSize = emSize,
            glyphCount = 1,
            glyphIndices = &glyph,
            glyphAdvances = &advance,
        };
        var transparent = default(DXGI_RGBA);
        rt->BeginDraw();
        rt->Clear(&transparent);
        rt->DrawGlyphRun(origin, &run, (ID2D1Brush*)brush.Get());
        return rt->EndDraw(null, null).SUCCEEDED ? ReadPixels(wicBitmap.Get(), canvas, 1) : null;
    }

    // Creates a canvas-sized WIC bitmap and premultiplied-alpha D2D render target.
    // The caller owns any non-null output even if target creation fails.
    private bool CreateTarget(
        ID2D1Factory* factory, RECT canvas, Guid wicFormat, DXGI_FORMAT format, IWICBitmap** bitmap,
        ID2D1RenderTarget** target)
    {
        var props = new D2D1_RENDER_TARGET_PROPERTIES
        {
            type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
            pixelFormat = new() { format = format, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED },
            dpiX = 96,
            dpiY = 96,
        };
        return this.wicFactory.Get()->CreateBitmap(
                       (uint)(canvas.right - canvas.left),
                       (uint)(canvas.bottom - canvas.top),
                       &wicFormat,
                       WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
                       bitmap).SUCCEEDED &&
               factory->CreateWicBitmapRenderTarget(*bitmap, &props, target).SUCCEEDED;
    }
}

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

/// <summary>Draws color glyphs (COLR/SVG/bitmaps) and large greyscale outlines with Direct2D.</summary>
internal sealed unsafe class ColorGlyphRasterizer : IDisposable
{
    // The formats that make a glyph a color glyph.
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
        fixed (Guid* iid = &IID.IID_IDWriteFontFace4)
        {
            return face->QueryInterface(iid, (void**)face4.GetAddressOf()).SUCCEEDED &&
                   (face4.Get()->GetGlyphImageFormats() & (ColorFormats | PaintTreeFormat)) != 0;
        }
    }

    /// <summary>Draws a glyph if it is a color glyph.</summary>
    /// <param name="face">The font face.</param>
    /// <param name="glyph">The glyph.</param>
    /// <param name="emSize">The em size in pixels.</param>
    /// <param name="subpixelX">Fraction of a pixel the pen is right of a whole pixel.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the glyph by.</param>
    /// <param name="canvas">The area to draw in, relative to the pen on the baseline. Ink outside is lost.</param>
    /// <param name="bitmap">The glyph, trimmed to its ink.</param>
    /// <returns>Whether it is a color glyph and was drawn with ink. False once disposed.</returns>
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
        try
        {
            if (!this.EnsureInitialized())
                return false;

            var factory = this.RentFactory();
            try
            {
                black = this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, 0f);
                white = black is null ? null : this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, 1f);
            }
            finally
            {
                this.ReturnFactory(factory);
            }
        }
        catch (Exception e)
        {
            Log.Verbose(e, "A color glyph could not be drawn. Its outline is drawn instead");
            return false;
        }
        finally
        {
            this.disposeLock.ExitReadLock();
        }

        if (black is null || white is null)
            return false;

        // Black text gives what the glyph draws itself.
        // White text adds the text color, the same in every channel.
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
    /// <param name="glyph">The glyph.</param>
    /// <param name="emSize">The em size in pixels.</param>
    /// <param name="subpixelX">Fraction of a pixel the pen is right of a whole pixel.</param>
    /// <param name="slant">Horizontal shift per pixel above the baseline to slant the glyph by.</param>
    /// <param name="canvas">The area to draw in, relative to the pen on the baseline. Ink outside is lost.</param>
    /// <param name="bitmap">The glyph, trimmed to its ink.</param>
    /// <returns>Whether it was drawn with ink. False once disposed.</returns>
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

    [SupportedOSPlatform("windows10.0")]
    private static void DrawLayer(
        ID2D1DeviceContext4* dc, ID2D1DeviceContext7* paintTreeContext, DWRITE_COLOR_GLYPH_RUN1* layer,
        ID2D1Brush* textBrush, ID2D1SolidColorBrush* layerBrush)
    {
        var b = &layer->Base;
        var origin = new D2D_POINT_2F { x = b->baselineOriginX, y = b->baselineOriginY };
        switch (layer->glyphImageFormat)
        {
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR_PAINT_TREE:
                paintTreeContext->DrawPaintGlyphRun(origin, &b->glyphRun, textBrush, 0, layer->measuringMode);
                break;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_SVG:
                dc->DrawSvgGlyphRun(origin, &b->glyphRun, textBrush, null, 0, layer->measuringMode);
                break;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PNG:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_JPEG:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TIFF:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_PREMULTIPLIED_B8G8R8A8:
                dc->DrawColorBitmapGlyphRun(layer->glyphImageFormat, origin, &b->glyphRun, layer->measuringMode);
                break;

            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_NONE:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_TRUETYPE:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_CFF:
            case DWRITE_GLYPH_IMAGE_FORMATS.DWRITE_GLYPH_IMAGE_FORMATS_COLR:
            default:
                ID2D1Brush* brush;
                if (b->paletteIndex == 0xFFFF)
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
                break;
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
            CO_MTA_USAGE_COOKIE cookie;
            _ = CoIncrementMTAUsage(&cookie);
            hr = CoCreateInstance(&clsid, null, context, &iid, (void**)factory.GetAddressOf());
        }

        hr.ThrowOnError();
        return factory;
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

    // A single-threaded Direct2D factory for this thread to draw with, until returned.
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

    // Draws the glyph's color layers with the text in a gray level and returns premultiplied BGRA. Null if it is not a
    // color glyph.
    private byte[]? Render(
        ID2D1Factory* factory, IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant,
        RECT canvas, float text)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            return null;

        // The per-glyph GetGlyphImageFormats reports none for COLR glyphs, so DirectWrite tells them apart here.
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

        var w = canvas.right - canvas.left;
        var h = canvas.bottom - canvas.top;
        using var wicBitmap = default(ComPtr<IWICBitmap>);
        var format = GUID.GUID_WICPixelFormat32bppPBGRA;
        if (this.wicFactory.Get()->CreateBitmap(
                (uint)w,
                (uint)h,
                &format,
                WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
                wicBitmap.GetAddressOf()).FAILED)
        {
            return null;
        }

        var props = new D2D1_RENDER_TARGET_PROPERTIES
        {
            type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
            pixelFormat = new()
            {
                format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED,
            },
            dpiX = 96,
            dpiY = 96,
        };
        using var target = default(ComPtr<ID2D1RenderTarget>);
        using var context = default(ComPtr<ID2D1DeviceContext4>);
        using var paintTreeContext = default(ComPtr<ID2D1DeviceContext7>);
        if (factory->CreateWicBitmapRenderTarget(
                wicBitmap.Get(),
                &props,
                target.GetAddressOf()).FAILED ||
            target.As(&context).FAILED)
        {
            return null;
        }

        if (paintTrees && target.As(&paintTreeContext).FAILED)
        {
            this.noPaintTrees = true;
            return this.Render(factory, face, glyph, emSize, subpixelX, slant, canvas, text);
        }

        var dc = context.Get();
        dc->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE.D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE);

        // Slanted around the baseline (y is down).
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
            DrawLayer(dc, paintTreeContext.Get(), layer, (ID2D1Brush*)textBrush.Get(), layerBrush.Get());
        }

        if (dc->EndDraw(null, null).FAILED)
            return null;

        var pixels = new byte[w * h * 4];
        fixed (byte* p = pixels)
        {
            if (wicBitmap.Get()->CopyPixels(null, (uint)w * 4, (uint)pixels.Length, p).FAILED)
                return null;
        }

        return pixels;
    }

    // Fills the glyph's outline in white and returns its coverage, a byte per pixel. Null if Direct2D failed.
    private byte[]? RenderOutline(
        ID2D1Factory* factory, IDWriteFontFace* face, ushort glyph, float emSize, float subpixelX, float slant,
        RECT canvas)
    {
        var w = canvas.right - canvas.left;
        var h = canvas.bottom - canvas.top;
        using var wicBitmap = default(ComPtr<IWICBitmap>);
        var format = GUID.GUID_WICPixelFormat8bppAlpha;
        if (this.wicFactory.Get()->CreateBitmap(
                (uint)w,
                (uint)h,
                &format,
                WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
                wicBitmap.GetAddressOf()).FAILED)
        {
            return null;
        }

        var props = new D2D1_RENDER_TARGET_PROPERTIES
        {
            type = D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
            pixelFormat = new()
            {
                format = DXGI_FORMAT.DXGI_FORMAT_A8_UNORM,
                alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED,
            },
            dpiX = 96,
            dpiY = 96,
        };
        using var target = default(ComPtr<ID2D1RenderTarget>);
        if (factory->CreateWicBitmapRenderTarget(wicBitmap.Get(), &props, target.GetAddressOf()).FAILED)
            return null;

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
        if (rt->EndDraw(null, null).FAILED)
            return null;

        var pixels = new byte[w * h];
        fixed (byte* p = pixels)
        {
            if (wicBitmap.Get()->CopyPixels(null, (uint)w, (uint)pixels.Length, p).FAILED)
                return null;
        }

        return pixels;
    }
}

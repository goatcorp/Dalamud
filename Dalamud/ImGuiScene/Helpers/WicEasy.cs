using System.Diagnostics.CodeAnalysis;

using Dalamud.Utility;

using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Manager for <see cref="IWICImagingFactory"/> containing some convenience methods.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal sealed unsafe class WicEasy : IDisposable
{
    private ComPtr<IWICImagingFactory> wicFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WicEasy"/> class.
    /// </summary>
    public WicEasy()
    {
        try
        {
            fixed (Guid* clsid = &CLSID.CLSID_WICImagingFactory)
            fixed (Guid* iid = &IID.IID_IWICImagingFactory)
            fixed (IWICImagingFactory** pp = &this.wicFactory.GetPinnableReference())
            {
                TerraFX.Interop.Windows.Windows.CoCreateInstance(
                    clsid,
                    null,
                    (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                    iid,
                    (void**)pp).ThrowOnError();
            }
        }
        catch
        {
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WicEasy"/> class.
    /// </summary>
    ~WicEasy() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets the pointer to an instance of <see cref="IWICImagingFactory"/>.
    /// </summary>
    public IWICImagingFactory* Factory => this.wicFactory;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates an empty new instance of <see cref="IWICStream"/>.
    /// </summary>
    /// <returns>The stream, wrapped in a smart pointer.</returns>
    public ComPtr<IWICStream> CreateStream()
    {
        var stream = default(ComPtr<IWICStream>);
        this.Factory->CreateStream(stream.GetAddressOf()).ThrowOnError();
        return stream;
    }

    /// <inheritdoc cref="CreateBitmapSource(TerraFX.Interop.Windows.IWICStream*)"/>
    public ComPtr<IWICBitmapSource> CreateBitmapSource(IWICStream* stream)
    {
        // Note: IStream is an ancestor of IWICStream; pointer casting is well-defined. 
        return this.CreateBitmapSource((IStream*)stream);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IWICBitmapSource"/> from <paramref name="stream"/>.<br />
    /// If the image contained within has multiple frames, the first frame is returned.
    /// </summary>
    /// <param name="stream">The stream containing the image data.</param>
    /// <returns>The new bitmap source, wrapped in a smart pointer.</returns>
    public ComPtr<IWICBitmapSource> CreateBitmapSource(IStream* stream)
    {
        using var decoder = default(ComPtr<IWICBitmapDecoder>);
        this.Factory->CreateDecoderFromStream(
            stream,
            null,
            WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
            decoder.GetAddressOf()).ThrowOnError();

        var result = default(ComPtr<IWICBitmapSource>);
        // Note: IWICBitmapSource is an ancestor of IWICBitmapFrameDecode; pointer casting is well-defined.
        decoder.Get()->GetFrame(0, (IWICBitmapFrameDecode**)result.GetAddressOf()).ThrowOnError();
        return result;
    }

    /// <summary>
    /// Converts <paramref name="source"/> into the pixel format <paramref name="newPixelFormat"/>.
    /// </summary>
    /// <param name="source">The bitmap source.</param>
    /// <param name="newPixelFormat">The new WIC pixel format.</param>
    /// <returns>The converted bitmap source, wrapped in a smart pointer.</returns>
    public ComPtr<IWICBitmapSource> ConvertPixelFormat(IWICBitmapSource* source, in Guid newPixelFormat)
    {
        using var converter = default(ComPtr<IWICFormatConverter>);
        this.Factory->CreateFormatConverter(converter.GetAddressOf()).ThrowOnError();
        fixed (Guid* format = &newPixelFormat)
        {
            converter.Get()->Initialize(
                source,
                format,
                WICBitmapDitherType.WICBitmapDitherTypeNone,
                null,
                0,
                WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut).ThrowOnError();
        }

        // Avoid increasing refcount; using a constructor of ComPtr<T> will call AddRef.
        var res = default(ComPtr<IWICBitmapSource>);
        // Note: IWICBitmapSource is an ancestor of IWICFormatConverter; pointer casting is well-defined.
        res.Attach((IWICBitmapSource*)converter.Get());
        converter.Detach();
        return res;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IWICBitmap"/> from a <see cref="IWICBitmapSource"/>.
    /// </summary>
    /// <param name="source">The bitmap source.</param>
    /// <returns>The new bitmap, wrapped in a smart pointer.</returns>
    public ComPtr<IWICBitmap> CreateBitmap(IWICBitmapSource* source)
    {
        uint width, height;
        source->GetSize(&width, &height).ThrowOnError();
        var pixelFormat = source->GetPixelFormat();
        var bitmap = default(ComPtr<IWICBitmap>);
        this.Factory->CreateBitmap(
            width,
            height,
            &pixelFormat,
            WICBitmapCreateCacheOption.WICBitmapCacheOnDemand,
            bitmap.GetAddressOf()).ThrowOnError();
        try
        {
            using var targetLock = bitmap.Get()->LockBits(
                WICBitmapLockFlags.WICBitmapLockWrite,
                out var pb,
                out var nb,
                out _);
            uint stride;
            targetLock.Get()->GetStride(&stride).ThrowOnError();
            source->CopyPixels(null, stride, nb, pb).ThrowOnError();
            return bitmap;
        }
        catch
        {
            bitmap.Reset();
            throw;
        }
    }

    /// <summary>
    /// Gets the number of bits per pixel for the WIC pixel format.
    /// </summary>
    /// <param name="pixelFormatGuid">The WIC pixel format.</param>
    /// <returns>Number of bits (bpp).</returns>
    public int GetBitsPerPixel(in Guid pixelFormatGuid)
    {
        using var cinfo = default(ComPtr<IWICComponentInfo>);
        fixed (Guid* guid = &pixelFormatGuid)
            this.Factory->CreateComponentInfo(guid, cinfo.ReleaseAndGetAddressOf()).ThrowOnError();

        using var pfinfo = default(ComPtr<IWICPixelFormatInfo>);
        cinfo.As(&pfinfo).ThrowOnError();

        uint bpp;
        pfinfo.Get()->GetBitsPerPixel(&bpp).ThrowOnError();

        return (int)bpp;
    }

    private void ReleaseUnmanagedResources() => this.wicFactory.Reset();
}

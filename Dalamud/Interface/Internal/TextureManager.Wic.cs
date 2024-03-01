using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal.SharedImmediateTextures;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Data;
using Lumina.Data.Files;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private ComPtr<IWICImagingFactory> wicFactory;

    /// <inheritdoc/>
    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    public Task SaveAsImageFormatToStreamAsync(
        IDalamudTextureWrap wrap,
        string extension,
        Stream stream,
        bool leaveOpen = false,
        IReadOnlyDictionary<string, object>? props = null,
        CancellationToken cancellationToken = default)
    {
        var container = GUID.GUID_ContainerFormatPng;
        foreach (var (k, v) in this.GetSupportedContainerFormats(WICComponentType.WICEncoder))
        {
            if (v.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
                container = k;
        }

        return this.SaveToStreamUsingWicAsync(
            wrap,
            container,
            pbag =>
            {
                if (props is null)
                    return;
                unsafe
                {
                    var nprop = 0u;
                    pbag.Get()->CountProperties(&nprop).ThrowOnError();
                    for (var i = 0u; i < nprop; i++)
                    {
                        var pbag2 = default(PROPBAG2);
                        var npropread = 0u;
                        pbag.Get()->GetPropertyInfo(i, 1, &pbag2, &npropread).ThrowOnError();
                        if (npropread == 0)
                            continue;
                        var propName = new string((char*)pbag2.pstrName);
                        if (props.TryGetValue(propName, out var untypedValue))
                        {
                            VARIANT val;
                            VariantInit(&val);

                            switch (untypedValue)
                            {
                                case null:
                                    val.vt = (ushort)VARENUM.VT_EMPTY;
                                    break;
                                case bool value:
                                    val.vt = (ushort)VARENUM.VT_BOOL;
                                    val.boolVal = (short)(value ? 1 : 0);
                                    break;
                                case byte value:
                                    val.vt = (ushort)VARENUM.VT_UI1;
                                    val.bVal = value;
                                    break;
                                case ushort value:
                                    val.vt = (ushort)VARENUM.VT_UI2;
                                    val.uiVal = value;
                                    break;
                                case uint value:
                                    val.vt = (ushort)VARENUM.VT_UI4;
                                    val.uintVal = value;
                                    break;
                                case ulong value:
                                    val.vt = (ushort)VARENUM.VT_UI8;
                                    val.ullVal = value;
                                    break;
                                case sbyte value:
                                    val.vt = (ushort)VARENUM.VT_I1;
                                    val.cVal = value;
                                    break;
                                case short value:
                                    val.vt = (ushort)VARENUM.VT_I2;
                                    val.iVal = value;
                                    break;
                                case int value:
                                    val.vt = (ushort)VARENUM.VT_I4;
                                    val.intVal = value;
                                    break;
                                case long value:
                                    val.vt = (ushort)VARENUM.VT_I8;
                                    val.llVal = value;
                                    break;
                                case float value:
                                    val.vt = (ushort)VARENUM.VT_R4;
                                    val.fltVal = value;
                                    break;
                                case double value:
                                    val.vt = (ushort)VARENUM.VT_R8;
                                    val.dblVal = value;
                                    break;
                                default:
                                    VariantClear(&val);
                                    continue;
                            }

                            VariantChangeType(&val, &val, 0, pbag2.vt).ThrowOnError();
                            pbag.Get()->Write(1, &pbag2, &val).ThrowOnError();
                            VariantClear(&val);
                        }

                        CoTaskMemFree(pbag2.pstrName);
                    }
                }
            },
            stream,
            leaveOpen,
            cancellationToken);
    }

    /// <inheritdoc/>
    public IEnumerable<string[]> GetLoadSupportedImageExtensions() =>
        this.GetSupportedContainerFormats(WICComponentType.WICDecoder).Values;

    /// <inheritdoc/>
    public IEnumerable<string[]> GetSaveSupportedImageExtensions() =>
        this.GetSupportedContainerFormats(WICComponentType.WICEncoder).Values;

    /// <summary>Creates a texture from the given bytes of an image file. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="bytes">The data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded texture.</returns>
    internal unsafe IDalamudTextureWrap NoThrottleCreateFromImage(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);

        cancellationToken.ThrowIfCancellationRequested();

        if (TexFileExtensions.IsPossiblyTexFile2D(bytes.Span))
        {
            var bytesArray = bytes.ToArray();
            var tf = new TexFile();
            typeof(TexFile).GetProperty(nameof(tf.Data))!.GetSetMethod(true)!.Invoke(
                tf,
                new object?[] { bytesArray });
            typeof(TexFile).GetProperty(nameof(tf.Reader))!.GetSetMethod(true)!.Invoke(
                tf,
                new object?[] { new LuminaBinaryReader(bytesArray) });
            // Note: FileInfo and FilePath are not used from TexFile; skip it.
            try
            {
                return this.NoThrottleCreateFromTexFile(tf);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        fixed (byte* p = bytes.Span)
        {
            using var wicStream = default(ComPtr<IWICStream>);
            this.wicFactory.Get()->CreateStream(wicStream.GetAddressOf()).ThrowOnError();
            wicStream.Get()->InitializeFromMemory(p, checked((uint)bytes.Length)).ThrowOnError();
            return this.NoThrottleCreateFromWicStream((IStream*)wicStream.Get(), cancellationToken);
        }
    }

    /// <summary>Creates a texture from the given path to an image file. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="path">The path of the file..</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded texture.</returns>
    internal async Task<IDalamudTextureWrap> NoThrottleCreateFromFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            unsafe
            {
                fixed (char* pPath = path)
                {
                    using var wicStream = default(ComPtr<IWICStream>);
                    this.wicFactory.Get()->CreateStream(wicStream.GetAddressOf()).ThrowOnError();
                    wicStream.Get()->InitializeFromFilename((ushort*)pPath, GENERIC_READ).ThrowOnError();
                    return this.NoThrottleCreateFromWicStream((IStream*)wicStream.Get(), cancellationToken);
                }
            }
        }
        catch
        {
            try
            {
                await using var fp = File.OpenRead(path);
                if (fp.Length >= Unsafe.SizeOf<TexFile.TexHeader>())
                {
                    var bytesArray = new byte[fp.Length];
                    await fp.ReadExactlyAsync(bytesArray, cancellationToken);
                    if (TexFileExtensions.IsPossiblyTexFile2D(bytesArray))
                    {
                        var tf = new TexFile();
                        typeof(TexFile).GetProperty(nameof(tf.Data))!.GetSetMethod(true)!.Invoke(
                            tf,
                            new object?[] { bytesArray });
                        typeof(TexFile).GetProperty(nameof(tf.Reader))!.GetSetMethod(true)!.Invoke(
                            tf,
                            new object?[] { new LuminaBinaryReader(bytesArray) });
                        // Note: FileInfo and FilePath are not used from TexFile; skip it.
                        return this.NoThrottleCreateFromTexFile(tf);
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }

            throw;
        }
    }

    /// <summary>
    /// Gets the corresponding <see cref="DXGI_FORMAT"/> from a <see cref="Guid"/> containing a WIC pixel format.
    /// </summary>
    /// <param name="fmt">The WIC pixel format.</param>
    /// <returns>The corresponding <see cref="DXGI_FORMAT"/>, or <see cref="DXGI_FORMAT.DXGI_FORMAT_UNKNOWN"/> if
    /// unavailable.</returns>
    private static DXGI_FORMAT GetCorrespondingDxgiFormat(Guid fmt) => 0 switch
    {
        // See https://github.com/microsoft/DirectXTex/wiki/WIC-I-O-Functions#savetowicmemory-savetowicfile
        _ when fmt == GUID.GUID_WICPixelFormat128bppRGBAFloat => DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
        _ when fmt == GUID.GUID_WICPixelFormat64bppRGBAHalf => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
        _ when fmt == GUID.GUID_WICPixelFormat64bppRGBA => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppRGBA1010102XR => DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppRGBA1010102 => DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat16bppBGRA5551 => DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat16bppBGR565 => DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppGrayFloat => DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,
        _ when fmt == GUID.GUID_WICPixelFormat16bppGrayHalf => DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
        _ when fmt == GUID.GUID_WICPixelFormat16bppGray => DXGI_FORMAT.DXGI_FORMAT_R16_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat8bppGray => DXGI_FORMAT.DXGI_FORMAT_R8_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat8bppAlpha => DXGI_FORMAT.DXGI_FORMAT_A8_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppRGBA => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppBGRA => DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
        _ when fmt == GUID.GUID_WICPixelFormat32bppBGR => DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM,
        _ => DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
    };

    private unsafe IDalamudTextureWrap NoThrottleCreateFromWicStream(
        IStream* wicStream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var decoder = default(ComPtr<IWICBitmapDecoder>);
        this.wicFactory.Get()->CreateDecoderFromStream(
            wicStream,
            null,
            WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
            decoder.GetAddressOf()).ThrowOnError();

        cancellationToken.ThrowIfCancellationRequested();

        using var frame = default(ComPtr<IWICBitmapFrameDecode>);
        decoder.Get()->GetFrame(0, frame.GetAddressOf()).ThrowOnError();
        var pixelFormat = default(Guid);
        frame.Get()->GetPixelFormat(&pixelFormat).ThrowOnError();
        var dxgiFormat = GetCorrespondingDxgiFormat(pixelFormat);

        cancellationToken.ThrowIfCancellationRequested();

        using var bitmapSource = default(ComPtr<IWICBitmapSource>);
        if (dxgiFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN || !this.IsDxgiFormatSupported(dxgiFormat))
        {
            dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            pixelFormat = GUID.GUID_WICPixelFormat32bppBGRA;
            WICConvertBitmapSource(&pixelFormat, (IWICBitmapSource*)frame.Get(), bitmapSource.GetAddressOf())
                .ThrowOnError();
        }
        else
        {
            frame.As(&bitmapSource);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var bitmap = default(ComPtr<IWICBitmap>);
        using var bitmapLock = default(ComPtr<IWICBitmapLock>);
        WICRect rcLock;
        uint stride;
        uint cbBufferSize;
        byte* pbData;
        if (bitmapSource.As(&bitmap).FAILED)
        {
            bitmapSource.Get()->GetSize((uint*)&rcLock.Width, (uint*)&rcLock.Height).ThrowOnError();
            this.wicFactory.Get()->CreateBitmap(
                (uint)rcLock.Width,
                (uint)rcLock.Height,
                &pixelFormat,
                WICBitmapCreateCacheOption.WICBitmapCacheOnDemand,
                bitmap.GetAddressOf()).ThrowOnError();

            bitmap.Get()->Lock(
                    &rcLock,
                    (uint)WICBitmapLockFlags.WICBitmapLockWrite,
                    bitmapLock.ReleaseAndGetAddressOf())
                .ThrowOnError();
            bitmapLock.Get()->GetStride(&stride).ThrowOnError();
            bitmapLock.Get()->GetDataPointer(&cbBufferSize, &pbData).ThrowOnError();
            bitmapSource.Get()->CopyPixels(null, stride, cbBufferSize, pbData).ThrowOnError();
        }

        cancellationToken.ThrowIfCancellationRequested();

        bitmap.Get()->Lock(
                &rcLock,
                (uint)WICBitmapLockFlags.WICBitmapLockRead,
                bitmapLock.ReleaseAndGetAddressOf())
            .ThrowOnError();
        bitmapSource.Get()->GetSize((uint*)&rcLock.Width, (uint*)&rcLock.Height).ThrowOnError();
        bitmapLock.Get()->GetStride(&stride).ThrowOnError();
        bitmapLock.Get()->GetDataPointer(&cbBufferSize, &pbData).ThrowOnError();
        bitmapSource.Get()->CopyPixels(null, stride, cbBufferSize, pbData).ThrowOnError();
        return this.NoThrottleCreateFromRaw(
            new RawImageSpecification(rcLock.Width, rcLock.Height, (int)stride, (int)dxgiFormat),
            new(pbData, (int)cbBufferSize));
    }

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    private unsafe Dictionary<Guid, string[]> GetSupportedContainerFormats(WICComponentType componentType)
    {
        var result = new Dictionary<Guid, string[]>();
        using var enumUnknown = default(ComPtr<IEnumUnknown>);
        this.wicFactory.Get()->CreateComponentEnumerator(
            (uint)componentType,
            (uint)WICComponentEnumerateOptions.WICComponentEnumerateDefault,
            enumUnknown.GetAddressOf()).ThrowOnError();

        while (true)
        {
            using var entry = default(ComPtr<IUnknown>);
            var fetched = 0u;
            enumUnknown.Get()->Next(1, entry.GetAddressOf(), &fetched).ThrowOnError();
            if (fetched == 0)
                break;

            using var codecInfo = default(ComPtr<IWICBitmapCodecInfo>);
            if (entry.As(&codecInfo).FAILED)
                continue;

            Guid containerFormat;
            if (codecInfo.Get()->GetContainerFormat(&containerFormat).FAILED)
                continue;

            var cch = 0u;
            _ = codecInfo.Get()->GetFileExtensions(0, null, &cch);
            var buf = new char[(int)cch + 1];
            fixed (char* pBuf = buf)
            {
                if (codecInfo.Get()->GetFileExtensions(cch + 1, (ushort*)pBuf, &cch).FAILED)
                    continue;
            }

            result.Add(containerFormat, new string(buf, 0, buf.IndexOf('\0')).Split(","));
        }

        return result;
    }

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    private async Task SaveToStreamUsingWicAsync(
        IDalamudTextureWrap wrap,
        Guid containerFormat,
        Action<ComPtr<IPropertyBag2>> propertyBackSetterDelegate,
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapCopy = wrap.CreateWrapSharingLowLevelResource();
        await using var streamCloser = leaveOpen ? null : stream;

        var (specs, bytes) = await this.GetRawDataAsync(
                                 wrapCopy,
                                 Vector2.Zero,
                                 Vector2.One,
                                 DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                                 cancellationToken).ConfigureAwait(false);

        using var encoder = default(ComPtr<IWICBitmapEncoder>);
        using var encoderFrame = default(ComPtr<IWICBitmapFrameEncode>);
        using var wrappedStream = new ManagedIStream(stream);
        var guidPixelFormat = GUID.GUID_WICPixelFormat32bppBGRA;
        unsafe
        {
            this.wicFactory.Get()->CreateEncoder(&containerFormat, null, encoder.GetAddressOf()).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoder.Get()->Initialize(wrappedStream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache)
                .ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            using var propertyBag = default(ComPtr<IPropertyBag2>);
            encoder.Get()->CreateNewFrame(encoderFrame.GetAddressOf(), propertyBag.GetAddressOf()).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            propertyBackSetterDelegate.Invoke(propertyBag);
            encoderFrame.Get()->Initialize(propertyBag).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoderFrame.Get()->SetPixelFormat(&guidPixelFormat).ThrowOnError();
            encoderFrame.Get()->SetSize(checked((uint)specs.Width), checked((uint)specs.Height)).ThrowOnError();

            using var tempBitmap = default(ComPtr<IWICBitmap>);
            fixed (Guid* pGuid = &GUID.GUID_WICPixelFormat32bppBGRA)
            fixed (byte* pBytes = bytes)
            {
                this.wicFactory.Get()->CreateBitmapFromMemory(
                    (uint)specs.Width,
                    (uint)specs.Height,
                    pGuid,
                    (uint)specs.Pitch,
                    checked((uint)bytes.Length),
                    pBytes,
                    tempBitmap.GetAddressOf()).ThrowOnError();
            }

            using var tempBitmap2 = default(ComPtr<IWICBitmapSource>);
            WICConvertBitmapSource(
                &guidPixelFormat,
                (IWICBitmapSource*)tempBitmap.Get(),
                tempBitmap2.GetAddressOf()).ThrowOnError();

            encoderFrame.Get()->SetSize(checked((uint)specs.Width), checked((uint)specs.Height)).ThrowOnError();
            encoderFrame.Get()->WriteSource(tempBitmap2.Get(), null).ThrowOnError();

            cancellationToken.ThrowIfCancellationRequested();

            encoderFrame.Get()->Commit().ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoder.Get()->Commit().ThrowOnError();
        }
    }
}

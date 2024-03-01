using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private ComPtr<IWICImagingFactory> factory;

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
        foreach (var (k, v) in this.GetSupportedContainerFormats())
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
    public IEnumerable<string[]> GetSupportedImageExtensions() => this.GetSupportedContainerFormats().Values;

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    private unsafe Dictionary<Guid, string[]> GetSupportedContainerFormats()
    {
        var result = new Dictionary<Guid, string[]>();
        using var enumUnknown = default(ComPtr<IEnumUnknown>);
        this.factory.Get()->CreateComponentEnumerator(
            (uint)WICComponentType.WICEncoder,
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
            this.factory.Get()->CreateEncoder(&containerFormat, null, encoder.GetAddressOf()).ThrowOnError();
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

            if (guidPixelFormat == GUID.GUID_WICPixelFormat32bppBGRA)
            {
                fixed (byte* pByte = bytes)
                {
                    encoderFrame.Get()->WritePixels(
                        (uint)specs.Height,
                        (uint)specs.Pitch,
                        checked((uint)bytes.Length),
                        pByte).ThrowOnError();
                }
            }
            else
            {
                using var tempBitmap = default(ComPtr<IWICBitmap>);
                fixed (Guid* pGuid = &GUID.GUID_WICPixelFormat32bppBGRA)
                fixed (byte* pBytes = bytes)
                {
                    this.factory.Get()->CreateBitmapFromMemory(
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
            }

            cancellationToken.ThrowIfCancellationRequested();

            encoderFrame.Get()->Commit().ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoder.Get()->Commit().ThrowOnError();
        }
    }
}

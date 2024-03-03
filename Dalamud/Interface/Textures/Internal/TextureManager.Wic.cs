using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.TerraFxCom;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    public async Task SaveToStreamAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        Stream stream,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapDispose = leaveWrapOpen ? null : wrap;
        using var istream = ManagedIStream.Create(stream, leaveStreamOpen);

        var (specs, bytes) = await this.GetRawDataFromExistingTextureAsync(
                                 wrap,
                                 Vector2.Zero,
                                 Vector2.One,
                                 DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                                 true,
                                 cancellationToken).ConfigureAwait(false);

        this.Wic.SaveToStreamUsingWic(
            specs,
            bytes,
            containerGuid,
            istream,
            props,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveToFileAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        string path,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapDispose = leaveWrapOpen ? null : wrap;
        var pathTemp = $"{path}.{GetCurrentThreadId():X08}{Environment.TickCount64:X16}.tmp";
        try
        {
            await this.SaveToStreamAsync(
                wrap,
                containerGuid,
                File.Create(pathTemp),
                props,
                true,
                false,
                cancellationToken);
        }
        catch (Exception e)
        {
            try
            {
                if (File.Exists(pathTemp))
                    File.Delete(pathTemp);
            }
            catch (Exception e2)
            {
                throw new AggregateException(
                    "Failed to save the file, and failed to remove the temporary file.",
                    e,
                    e2);
            }

            throw;
        }

        try
        {
            try
            {
                File.Replace(pathTemp, path, null, true);
            }
            catch
            {
                File.Move(pathTemp, path, true);
            }
        }
        catch (Exception e)
        {
            try
            {
                if (File.Exists(pathTemp))
                    File.Delete(pathTemp);
            }
            catch (Exception e2)
            {
                throw new AggregateException(
                    "Failed to move the temporary file to the target path, and failed to remove the temporary file.",
                    e,
                    e2);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    IEnumerable<IBitmapCodecInfo> ITextureProvider.GetSupportedImageDecoderInfos() =>
        this.Wic.GetSupportedDecoderInfos();

    /// <inheritdoc/>
    IEnumerable<IBitmapCodecInfo> ITextureProvider.GetSupportedImageEncoderInfos() =>
        this.Wic.GetSupportedEncoderInfos();

    /// <summary>Creates a texture from the given bytes of an image file. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="bytes">The data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromImage(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var handle = bytes.Pin();
            using var stream = this.Wic.CreateIStreamFromMemory(handle, bytes.Length);
            return this.Wic.NoThrottleCreateFromWicStream(stream, cancellationToken);
        }
        catch (Exception e1)
        {
            try
            {
                return this.NoThrottleCreateFromTexFile(bytes.Span);
            }
            catch (Exception e2)
            {
                throw new AggregateException(e1, e2);
            }
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
            using var stream = this.Wic.CreateIStreamFromFile(path);
            return this.Wic.NoThrottleCreateFromWicStream(stream, cancellationToken);
        }
        catch (Exception e1)
        {
            try
            {
                return this.NoThrottleCreateFromTexFile(await File.ReadAllBytesAsync(path, cancellationToken));
            }
            catch (Exception e2)
            {
                throw new AggregateException(e1, e2);
            }
        }
    }

    /// <summary>A part of texture manager that uses Windows Imaging Component under the hood.</summary>
    internal sealed class WicManager : IDisposable
    {
        private readonly TextureManager textureManager;
        private ComPtr<IWICImagingFactory> wicFactory;

        /// <summary>Initializes a new instance of the <see cref="WicManager"/> class.</summary>
        /// <param name="textureManager">An instance of <see cref="Interface.Textures.Internal.TextureManager"/>.</param>
        public WicManager(TextureManager textureManager)
        {
            this.textureManager = textureManager;
            unsafe
            {
                fixed (Guid* pclsidWicImagingFactory = &CLSID.CLSID_WICImagingFactory)
                fixed (Guid* piidWicImagingFactory = &IID.IID_IWICImagingFactory)
                {
                    CoCreateInstance(
                        pclsidWicImagingFactory,
                        null,
                        (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                        piidWicImagingFactory,
                        (void**)this.wicFactory.GetAddressOf()).ThrowOnError();
                }
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WicManager"/> class.
        /// </summary>
        ~WicManager() => this.ReleaseUnmanagedResource();

        /// <inheritdoc/>
        public void Dispose()
        {
            this.ReleaseUnmanagedResource();
            GC.SuppressFinalize(this);
        }

        /// <summary>Creates a new instance of <see cref="IStream"/> from a <see cref="MemoryHandle"/>.</summary>
        /// <param name="handle">An instance of <see cref="MemoryHandle"/>.</param>
        /// <param name="length">The number of bytes in the memory.</param>
        /// <returns>The new instance of <see cref="IStream"/>.</returns>
        public unsafe ComPtr<IStream> CreateIStreamFromMemory(MemoryHandle handle, int length)
        {
            using var wicStream = default(ComPtr<IWICStream>);
            this.wicFactory.Get()->CreateStream(wicStream.GetAddressOf()).ThrowOnError();
            wicStream.Get()->InitializeFromMemory((byte*)handle.Pointer, checked((uint)length)).ThrowOnError();

            var res = default(ComPtr<IStream>);
            wicStream.As(ref res).ThrowOnError();
            return res;
        }

        /// <summary>Creates a new instance of <see cref="IStream"/> from a file path.</summary>
        /// <param name="path">The file path.</param>
        /// <returns>The new instance of <see cref="IStream"/>.</returns>
        public unsafe ComPtr<IStream> CreateIStreamFromFile(string path)
        {
            using var wicStream = default(ComPtr<IWICStream>);
            this.wicFactory.Get()->CreateStream(wicStream.GetAddressOf()).ThrowOnError();
            fixed (char* pPath = path)
                wicStream.Get()->InitializeFromFilename((ushort*)pPath, GENERIC_READ).ThrowOnError();

            var res = default(ComPtr<IStream>);
            wicStream.As(ref res).ThrowOnError();
            return res;
        }

        /// <summary>Creates a new instance of <see cref="IDalamudTextureWrap"/> from a <see cref="IStream"/>.</summary>
        /// <param name="stream">The stream that will NOT be closed after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly loaded texture.</returns>
        public unsafe IDalamudTextureWrap NoThrottleCreateFromWicStream(
            ComPtr<IStream> stream,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var decoder = default(ComPtr<IWICBitmapDecoder>);
            this.wicFactory.Get()->CreateDecoderFromStream(
                stream,
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
            if (dxgiFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN || !this.textureManager.IsDxgiFormatSupported(dxgiFormat))
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
            return this.textureManager.NoThrottleCreateFromRaw(
                new(rcLock.Width, rcLock.Height, (int)stride, (int)dxgiFormat),
                new(pbData, (int)cbBufferSize));
        }

        /// <summary>Gets the supported bitmap codecs.</summary>
        /// <returns>The supported encoders.</returns>
        public IEnumerable<BitmapCodecInfo> GetSupportedEncoderInfos()
        {
            foreach (var ptr in new ComponentEnumerable<IWICBitmapCodecInfo>(
                         this.wicFactory,
                         WICComponentType.WICEncoder))
                yield return new(ptr);
        }

        /// <summary>Gets the supported bitmap codecs.</summary>
        /// <returns>The supported decoders.</returns>
        public IEnumerable<BitmapCodecInfo> GetSupportedDecoderInfos()
        {
            foreach (var ptr in new ComponentEnumerable<IWICBitmapCodecInfo>(
                         this.wicFactory,
                         WICComponentType.WICDecoder))
                yield return new(ptr);
        }

        /// <summary>Saves the given raw bitmap to a stream.</summary>
        /// <param name="specs">The raw bitmap specifications.</param>
        /// <param name="bytes">The raw bitmap bytes.</param>
        /// <param name="containerFormat">The container format from <see cref="GetSupportedEncoderInfos"/>.</param>
        /// <param name="stream">The stream to write to. The ownership is not transferred.</param>
        /// <param name="props">The encoder properties.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public unsafe void SaveToStreamUsingWic(
            RawImageSpecification specs,
            byte[] bytes,
            Guid containerFormat,
            ComPtr<IStream> stream,
            IReadOnlyDictionary<string, object>? props = null,
            CancellationToken cancellationToken = default)
        {
            var outPixelFormat = GUID.GUID_WICPixelFormat32bppBGRA;
            var inPixelFormat = GetCorrespondingWicPixelFormat((DXGI_FORMAT)specs.DxgiFormat);
            if (inPixelFormat == Guid.Empty)
                throw new NotSupportedException("DXGI_FORMAT from specs is not supported by WIC.");

            using var encoder = default(ComPtr<IWICBitmapEncoder>);
            using var encoderFrame = default(ComPtr<IWICBitmapFrameEncode>);
            this.wicFactory.Get()->CreateEncoder(&containerFormat, null, encoder.GetAddressOf()).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoder.Get()->Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache)
                .ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            using var propertyBag = default(ComPtr<IPropertyBag2>);
            encoder.Get()->CreateNewFrame(encoderFrame.GetAddressOf(), propertyBag.GetAddressOf()).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            if (props is not null)
            {
                var nprop = 0u;
                propertyBag.Get()->CountProperties(&nprop).ThrowOnError();
                for (var i = 0u; i < nprop; i++)
                {
                    var pbag2 = default(PROPBAG2);
                    var npropread = 0u;
                    propertyBag.Get()->GetPropertyInfo(i, 1, &pbag2, &npropread).ThrowOnError();
                    if (npropread == 0)
                        continue;
                    try
                    {
                        var propName = new string((char*)pbag2.pstrName);
                        if (props.TryGetValue(propName, out var untypedValue))
                        {
                            VARIANT val;
                            // Marshal calls VariantInit.
                            Marshal.GetNativeVariantForObject(untypedValue, (nint)(&val));
                            VariantChangeType(&val, &val, 0, pbag2.vt).ThrowOnError();
                            propertyBag.Get()->Write(1, &pbag2, &val).ThrowOnError();
                            VariantClear(&val);
                        }
                    }
                    finally
                    {
                        CoTaskMemFree(pbag2.pstrName);
                    }
                }
            }

            encoderFrame.Get()->Initialize(propertyBag).ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoderFrame.Get()->SetPixelFormat(&outPixelFormat).ThrowOnError();
            encoderFrame.Get()->SetSize(checked((uint)specs.Width), checked((uint)specs.Height)).ThrowOnError();

            using var tempBitmap = default(ComPtr<IWICBitmap>);
            fixed (byte* pBytes = bytes)
            {
                this.wicFactory.Get()->CreateBitmapFromMemory(
                    (uint)specs.Width,
                    (uint)specs.Height,
                    &inPixelFormat,
                    (uint)specs.Pitch,
                    checked((uint)bytes.Length),
                    pBytes,
                    tempBitmap.GetAddressOf()).ThrowOnError();
            }

            using var outBitmapSource = default(ComPtr<IWICBitmapSource>);
            if (inPixelFormat != outPixelFormat)
            {
                WICConvertBitmapSource(
                    &outPixelFormat,
                    (IWICBitmapSource*)tempBitmap.Get(),
                    outBitmapSource.GetAddressOf()).ThrowOnError();
            }
            else
            {
                tempBitmap.As(&outBitmapSource);
            }

            encoderFrame.Get()->SetSize(checked((uint)specs.Width), checked((uint)specs.Height)).ThrowOnError();
            encoderFrame.Get()->WriteSource(outBitmapSource.Get(), null).ThrowOnError();

            cancellationToken.ThrowIfCancellationRequested();

            encoderFrame.Get()->Commit().ThrowOnError();
            cancellationToken.ThrowIfCancellationRequested();

            encoder.Get()->Commit().ThrowOnError();
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
            _ when fmt == GUID.GUID_WICPixelFormat32bppRGBA1010102XR => DXGI_FORMAT
                .DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
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

        /// <summary>
        /// Gets the corresponding <see cref="Guid"/> containing a WIC pixel format from a <see cref="DXGI_FORMAT"/>.
        /// </summary>
        /// <param name="fmt">The DXGI pixel format.</param>
        /// <returns>The corresponding <see cref="Guid"/>, or <see cref="Guid.Empty"/> if unavailable.</returns>
        private static Guid GetCorrespondingWicPixelFormat(DXGI_FORMAT fmt) => fmt switch
        {
            // See https://github.com/microsoft/DirectXTex/wiki/WIC-I-O-Functions#savetowicmemory-savetowicfile
            DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT => GUID.GUID_WICPixelFormat128bppRGBAFloat,
            DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT => GUID.GUID_WICPixelFormat64bppRGBAHalf,
            DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM => GUID.GUID_WICPixelFormat64bppRGBA,
            DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM => GUID.GUID_WICPixelFormat32bppRGBA1010102XR,
            DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM => GUID.GUID_WICPixelFormat32bppRGBA1010102,
            DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM => GUID.GUID_WICPixelFormat16bppBGRA5551,
            DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM => GUID.GUID_WICPixelFormat16bppBGR565,
            DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT => GUID.GUID_WICPixelFormat32bppGrayFloat,
            DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT => GUID.GUID_WICPixelFormat16bppGrayHalf,
            DXGI_FORMAT.DXGI_FORMAT_R16_UNORM => GUID.GUID_WICPixelFormat16bppGray,
            DXGI_FORMAT.DXGI_FORMAT_R8_UNORM => GUID.GUID_WICPixelFormat8bppGray,
            DXGI_FORMAT.DXGI_FORMAT_A8_UNORM => GUID.GUID_WICPixelFormat8bppAlpha,
            DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM => GUID.GUID_WICPixelFormat32bppRGBA,
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM => GUID.GUID_WICPixelFormat32bppBGRA,
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM => GUID.GUID_WICPixelFormat32bppBGR,
            _ => Guid.Empty,
        };

        private void ReleaseUnmanagedResource() => this.wicFactory.Reset();

        private readonly struct ComponentEnumerable<T> : IEnumerable<ComPtr<T>>
            where T : unmanaged, IWICComponentInfo.Interface
        {
            private readonly ComPtr<IWICImagingFactory> factory;
            private readonly WICComponentType componentType;

            /// <summary>Initializes a new instance of the <see cref="ComponentEnumerable{T}"/> struct.</summary>
            /// <param name="factory">The WIC factory. Ownership is not transferred.
            /// </param>
            /// <param name="componentType">The component type to enumerate.</param>
            public ComponentEnumerable(ComPtr<IWICImagingFactory> factory, WICComponentType componentType)
            {
                this.factory = factory;
                this.componentType = componentType;
            }

            public unsafe ManagedIEnumUnknownEnumerator<T> GetEnumerator()
            {
                var enumUnknown = default(ComPtr<IEnumUnknown>);
                this.factory.Get()->CreateComponentEnumerator(
                    (uint)this.componentType,
                    (uint)WICComponentEnumerateOptions.WICComponentEnumerateDefault,
                    enumUnknown.GetAddressOf()).ThrowOnError();
                return new(enumUnknown);
            }

            IEnumerator<ComPtr<T>> IEnumerable<ComPtr<T>>.GetEnumerator() => this.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Helpers for <see cref="WicEasy"/>.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal static class WicEasyExtensions
{
    /// <summary>
    /// Creates an instance of <see cref="IStream"/> from a path to file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns><see cref="IStream"/> wrapped in a smart pointer.</returns>
    public static unsafe ComPtr<IStream> CreateStreamFromFile(string path)
    {
        var cb = Encoding.Unicode.GetByteCount(path);
        var buf = stackalloc byte[cb + 2];
        buf[cb] = buf[cb + 1] = 0;
        Encoding.Unicode.GetBytes(path, new(buf, cb));

        var stream = default(ComPtr<IStream>);
        TerraFX.Interop.Windows.Windows.SHCreateStreamOnFileW(
            (ushort*)buf,
            STGM.STGM_SHARE_DENY_WRITE,
            stream.ReleaseAndGetAddressOf()).ThrowOnError();
        return stream;
    }

    /// <summary>
    /// Locks the bits of <paramref name="bitmap"/> for its whole region.
    /// </summary>
    /// <param name="bitmap">The pointer to an instance of <see cref="IWICBitmap"/>.</param>
    /// <param name="mode">The lock mode.</param>
    /// <param name="pb">The pointer to data bytes.</param>
    /// <param name="nb">The number of bytes available.</param>
    /// <param name="fixedBytes">The span view of the data.</param>
    /// <returns>The handle to the lock, providing additional information on demand, wrapped in a smart pointer. Dispose after use.</returns>
    public static unsafe ComPtr<IWICBitmapLock> LockBits(
        ref this IWICBitmap bitmap,
        WICBitmapLockFlags mode,
        out byte* pb,
        out uint nb,
        out Span<byte> fixedBytes)
    {
        var rc = default(WICRect);
        bitmap.GetSize((uint*)&rc.Width, (uint*)&rc.Height).ThrowOnError();

        var result = default(ComPtr<IWICBitmapLock>);
        bitmap.Lock(&rc, (uint)mode, result.GetAddressOf());
        try
        {
            uint nbc;
            byte* pbc;
            result.Get()->GetDataPointer(&nbc, &pbc).ThrowOnError();
            pb = pbc;
            nb = nbc;
            fixedBytes = new(pb, (int)nb);
        }
        catch
        {
            result.Reset();
            throw;
        }

        return result;
    }
    
    /// <summary>
    /// Gets the corresponding <see cref="DXGI_FORMAT"/> from a <see cref="Guid"/> containing a WIC pixel format.
    /// </summary>
    /// <param name="fmt">The WIC pixel format.</param>
    /// <returns>The corresponding <see cref="DXGI_FORMAT"/>, or <see cref="DXGI_FORMAT.DXGI_FORMAT_UNKNOWN"/> if unavailable.</returns>
    public static DXGI_FORMAT ToDxgiFormat(in this Guid fmt) => 0 switch
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

    /// <summary>
    /// Gets the pixel format of <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The bitmap source.</param>
    /// <returns>The WIC pixel format.</returns>
    public static unsafe Guid GetPixelFormat(ref this IWICBitmapSource source)
    {
        var pixelFormat = default(Guid);
        source.GetPixelFormat(&pixelFormat).ThrowOnError();
        return pixelFormat;
    }
}

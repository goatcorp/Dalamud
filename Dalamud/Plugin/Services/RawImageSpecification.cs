using System.Diagnostics.CodeAnalysis;

using TerraFX.Interop.DirectX;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Describes a raw image.
/// </summary>
/// <param name="Width">The width of the image.</param>
/// <param name="Height">The height of the image.</param>
/// <param name="Pitch">The pitch of the image.</param>
/// <param name="DxgiFormat">The format of the image. See <a href="https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format">DXGI_FORMAT</a>.</param>
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1313:Parameter names should begin with lower-case letter",
    Justification = "no")]
public record struct RawImageSpecification(int Width, int Height, int Pitch, int DxgiFormat)
{
    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution and pixel
    /// format. Pitch will be automatically calculated.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">The format.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification From(int width, int height, int format)
    {
        int bitsPerPixel;
        var isBlockCompression = false;
        switch ((DXGI_FORMAT)format)
        {
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT:
                bitsPerPixel = 128;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT:
                bitsPerPixel = 96;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                bitsPerPixel = 64;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                bitsPerPixel = 32;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_D16_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R16_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R16_SINT:
                bitsPerPixel = 16;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R8_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                bitsPerPixel = 8;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R1_UNORM:
                bitsPerPixel = 1;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM:
                throw new NotSupportedException();
            case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                bitsPerPixel = 4;
                isBlockCompression = true;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                bitsPerPixel = 8;
                isBlockCompression = true;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                bitsPerPixel = 4;
                isBlockCompression = true;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                bitsPerPixel = 8;
                isBlockCompression = true;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM:
                bitsPerPixel = 16;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                bitsPerPixel = 32;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                bitsPerPixel = 8;
                isBlockCompression = true;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_AYUV:
            case DXGI_FORMAT.DXGI_FORMAT_Y410:
            case DXGI_FORMAT.DXGI_FORMAT_Y416:
            case DXGI_FORMAT.DXGI_FORMAT_NV12:
            case DXGI_FORMAT.DXGI_FORMAT_P010:
            case DXGI_FORMAT.DXGI_FORMAT_P016:
            case DXGI_FORMAT.DXGI_FORMAT_420_OPAQUE:
            case DXGI_FORMAT.DXGI_FORMAT_YUY2:
            case DXGI_FORMAT.DXGI_FORMAT_Y210:
            case DXGI_FORMAT.DXGI_FORMAT_Y216:
            case DXGI_FORMAT.DXGI_FORMAT_NV11:
            case DXGI_FORMAT.DXGI_FORMAT_AI44:
            case DXGI_FORMAT.DXGI_FORMAT_IA44:
            case DXGI_FORMAT.DXGI_FORMAT_P8:
            case DXGI_FORMAT.DXGI_FORMAT_A8P8:
                throw new NotSupportedException();
            case DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM:
                bitsPerPixel = 16;
                break;
            default:
                throw new NotSupportedException();
        }

        var pitch = isBlockCompression
                        ? Math.Max(1, (width + 3) / 4) * 2 * bitsPerPixel
                        : ((width * bitsPerPixel) + 7) / 8;

        return new(width, height, pitch, format);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in B8G8R8A8(BGRA32) UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification Bgra32(int width, int height) =>
        new(width, height, width * 4, (int)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM);

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in R8G8B8A8(RGBA32) UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification Rgba32(int width, int height) =>
        new(width, height, width * 4, (int)DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM);

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in A8 UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification A8(int width, int height) =>
        new(width, height, width, (int)DXGI_FORMAT.DXGI_FORMAT_A8_UNORM);
}

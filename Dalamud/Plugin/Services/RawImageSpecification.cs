using TerraFX.Interop.DirectX;

namespace Dalamud.Plugin.Services;

/// <summary>Describes a raw image.</summary>
public record struct RawImageSpecification
{
    private const string FormatNotSupportedMessage = $"{nameof(DxgiFormat)} is not supported.";

    /// <summary>Initializes a new instance of the <see cref="RawImageSpecification"/> class.</summary>
    /// <param name="width">The width of the raw image.</param>
    /// <param name="height">The height of the raw image.</param>
    /// <param name="dxgiFormat">The DXGI format of the raw image.</param>
    /// <param name="pitch">The pitch of the raw image in bytes.
    /// Specify <c>-1</c> to calculate it from other parameters.</param>
    public RawImageSpecification(int width, int height, int dxgiFormat, int pitch = -1)
    {
        if (pitch < 0)
        {
            if (!GetFormatInfo((DXGI_FORMAT)dxgiFormat, out var bitsPerPixel, out var isBlockCompression))
                throw new NotSupportedException(FormatNotSupportedMessage);

            pitch = isBlockCompression
                            ? Math.Max(1, (width + 3) / 4) * 2 * bitsPerPixel
                            : ((width * bitsPerPixel) + 7) / 8;
        }

        this.Width = width;
        this.Height = height;
        this.Pitch = pitch;
        this.DxgiFormat = dxgiFormat;
    }

    /// <summary>Gets or sets the width of the raw image.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the height of the raw image.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the pitch of the raw image in bytes.</summary>
    /// <remarks>The value may not always exactly match
    /// <c><see cref="Width"/> * bytesPerPixelFromDxgiFormat</c>.
    /// </remarks>
    public int Pitch { get; set; }

    /// <summary>Gets or sets the format of the raw image.</summary>
    /// <remarks>See <a href="https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format">
    /// DXGI_FORMAT</a>.</remarks>
    public int DxgiFormat { get; set; }

    /// <summary>Gets the number of bits per pixel.</summary>
    /// <exception cref="NotSupportedException">Thrown if <see cref="DxgiFormat"/> is not supported.</exception>
    public int BitsPerPixel =>
        GetFormatInfo((DXGI_FORMAT)this.DxgiFormat, out var bitsPerPixel, out _)
            ? bitsPerPixel
            : throw new NotSupportedException(FormatNotSupportedMessage);

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in B8G8R8A8(BGRA32) UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification Bgra32(int width, int height) =>
        new(width, height, (int)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, width * 4);

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in R8G8B8A8(RGBA32) UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification Rgba32(int width, int height) =>
        new(width, height, (int)DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, width * 4);

    /// <summary>
    /// Creates a new instance of <see cref="RawImageSpecification"/> record using the given resolution,
    /// in A8 UNorm pixel format.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>The new instance.</returns>
    public static RawImageSpecification A8(int width, int height) =>
        new(width, height, (int)DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, width);

    private static bool GetFormatInfo(DXGI_FORMAT format, out int bitsPerPixel, out bool isBlockCompression)
    {
        switch (format)
        {
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT:
                bitsPerPixel = 128;
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT:
                bitsPerPixel = 96;
                isBlockCompression = false;
                return true;
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
                isBlockCompression = false;
                return true;
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
                isBlockCompression = false;
                return true;
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
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
            case DXGI_FORMAT.DXGI_FORMAT_R8_SNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8_SINT:
            case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                bitsPerPixel = 8;
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_R1_UNORM:
                bitsPerPixel = 1;
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                bitsPerPixel = 4;
                isBlockCompression = true;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                bitsPerPixel = 8;
                isBlockCompression = true;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                bitsPerPixel = 4;
                isBlockCompression = true;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                bitsPerPixel = 8;
                isBlockCompression = true;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM:
                bitsPerPixel = 16;
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                bitsPerPixel = 32;
                isBlockCompression = false;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                bitsPerPixel = 8;
                isBlockCompression = true;
                return true;
            case DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM:
                bitsPerPixel = 16;
                isBlockCompression = true;
                return false;
            default:
                bitsPerPixel = 0;
                isBlockCompression = false;
                return false;
        }
    }
}

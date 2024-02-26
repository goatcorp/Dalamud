using Dalamud.ImGuiScene;

using Lumina.Data.Files;

using TerraFX.Interop.DirectX;

namespace Dalamud.Utility;

/// <summary>
/// Extensions to <see cref="TexFile"/>.
/// </summary>
public static class TexFileExtensions
{
    /// <summary>
    /// Returns the image data formatted for <see cref="IImGuiScene.CreateTexture2DFromRaw"/>,
    /// using <see cref="DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM"/>.<br />
    /// <b>Consider using <see cref="TexFile.ImageData"/> with <see cref="DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM"/>.</b>
    /// </summary>
    /// <param name="texFile">The TexFile to format.</param>
    /// <returns>The formatted image data.</returns>
    public static byte[] GetRgbaImageData(this TexFile texFile)
    {
        var imageData = texFile.ImageData;
        var dst = new byte[imageData.Length];

        for (var i = 0; i < dst.Length; i += 4)
        {
            dst[i] = imageData[i + 2];
            dst[i + 1] = imageData[i + 1];
            dst[i + 2] = imageData[i];
            dst[i + 3] = imageData[i + 3];
        }

        return dst;
    }
}

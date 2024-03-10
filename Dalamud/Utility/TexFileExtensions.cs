using System.Runtime.CompilerServices;

using Dalamud.Memory;

using ImGuiScene;
using Lumina.Data.Files;

namespace Dalamud.Utility;

/// <summary>
/// Extensions to <see cref="TexFile"/>.
/// </summary>
public static class TexFileExtensions
{
    /// <summary>
    /// Returns the image data formatted for <see cref="RawDX11Scene.LoadImageRaw"/>.
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

    /// <summary>Determines if the given data is possibly a <see cref="TexFile"/>.</summary>
    /// <param name="data">The data.</param>
    /// <returns><c>true</c> if it should be attempted to be interpreted as a <see cref="TexFile"/>.</returns>
    internal static unsafe bool IsPossiblyTexFile2D(ReadOnlySpan<byte> data)
    {
        if (data.Length < Unsafe.SizeOf<TexFile.TexHeader>())
            return false;
        fixed (byte* ptr = data)
        {
            ref readonly var texHeader = ref MemoryHelper.Cast<TexFile.TexHeader>((nint)ptr);
            if ((texHeader.Type & TexFile.Attribute.TextureTypeMask) != TexFile.Attribute.TextureType2D)
                return false;
            if (!Enum.IsDefined(texHeader.Format))
                return false;
            if (texHeader.Width == 0 || texHeader.Height == 0)
                return false;
        }

        return true;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Internal;

namespace Dalamud.Interface.Spannables.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannableRenderer
{
    /// <summary>The path of texture files associated with gfdata.gfd.</summary>
    public static readonly string[] GfdTexturePaths =
    {
        "common/font/fonticon_xinput.tex",
        "common/font/fonticon_ps3.tex",
        "common/font/fonticon_ps4.tex",
        "common/font/fonticon_ps5.tex",
        "common/font/fonticon_lys.tex",
    };

    private readonly byte[] gfdFile;
    private readonly IDalamudTextureWrap[] gfdTextures;

    /// <summary>Gets the GFD file view.</summary>
    private GfdFileView GfdFileView => new(new(Unsafe.AsPointer(ref this.gfdFile[0]), this.gfdFile.Length));

    /// <inheritdoc/>
    public bool TryGetIcon(
        int iconType,
        uint iconId,
        Vector2 minDimensions,
        [NotNullWhen(true)] out IDalamudTextureWrap? textureWrap,
        out Vector2 uv0,
        out Vector2 uv1)
    {
        if (iconType < 0
            || iconType >= this.gfdTextures.Length
            || !this.GfdFileView.TryGetEntry(iconId, out var entry))
        {
            textureWrap = null;
            uv0 = uv1 = default;
            return false;
        }

        textureWrap = this.gfdTextures[iconType];
        var useHiRes = entry.Width < minDimensions.X || entry.Height < minDimensions.Y;
        uv0 = new(entry.Left, entry.Top);
        uv1 = new(entry.Width, entry.Height);
        if (useHiRes)
        {
            uv0 *= 2;
            uv0.Y += 341;
            uv1 *= 2;
        }

        uv1 += uv0;

        uv0 /= textureWrap.Size;
        uv1 /= textureWrap.Size;
        return true;
    }
}

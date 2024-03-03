using Dalamud.Interface.Textures;

using TerraFX.Interop.DirectX;

namespace Dalamud.Storage.Assets;

/// <summary>Provide raw texture data directly. </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class DalamudAssetRawTextureAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DalamudAssetRawTextureAttribute"/> class.</summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">The format.</param>
    /// <param name="pitch">The pitch.</param>
    public DalamudAssetRawTextureAttribute(int width, int height, DXGI_FORMAT format, int pitch) =>
        this.Specification = new(width, height, (int)format, pitch);

    /// <summary>
    /// Gets the specification.
    /// </summary>
    public RawImageSpecification Specification { get; }
}

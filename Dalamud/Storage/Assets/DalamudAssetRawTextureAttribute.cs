using Dalamud.Plugin.Services;

using SharpDX.DXGI;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Provide raw texture data directly.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class DalamudAssetRawTextureAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetRawTextureAttribute"/> class.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="pitch">The pitch.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">The format.</param>
    public DalamudAssetRawTextureAttribute(int width, int pitch, int height, Format format)
    {
        this.Specification = new(width, height, pitch, (int)format);
    }

    /// <summary>
    /// Gets the specification.
    /// </summary>
    public RawImageSpecification Specification { get; }
}

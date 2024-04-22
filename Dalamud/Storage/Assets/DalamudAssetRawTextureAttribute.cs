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
        this.Width = width;
        this.Pitch = pitch;
        this.Height = height;
        this.Format = format;
    }

    /// <summary>
    /// Gets the width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the pitch.
    /// </summary>
    public int Pitch { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the format.
    /// </summary>
    public Format Format { get; }
}

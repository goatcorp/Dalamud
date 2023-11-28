namespace Dalamud.Storage.Assets;

/// <summary>
/// Purposes of a Dalamud asset.
/// </summary>
public enum DalamudAssetPurpose
{
    /// <summary>
    /// The asset has no purpose.
    /// </summary>
    Empty = 0,
    
    /// <summary>
    /// The asset is a .png file, and can be purposed as a <see cref="SharpDX.Direct3D11.Texture2D"/>.
    /// </summary>
    TextureFromPng = 10,
    
    /// <summary>
    /// The asset is a raw texture, and can be purposed as a <see cref="SharpDX.Direct3D11.Texture2D"/>.
    /// </summary>
    TextureFromRaw = 1001,

    /// <summary>
    /// The asset is a font file.
    /// </summary>
    Font = 2000,
}

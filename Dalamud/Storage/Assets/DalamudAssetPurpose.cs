namespace Dalamud.Storage.Assets;

/// <summary>
/// Purposes of a Dalamud asset.
/// </summary>
public enum DalamudAssetPurpose
{
    /// <summary>
    /// The asset has no purpose, and is not valid and/or not accessible.
    /// </summary>
    Empty = 0,
    
    /// <summary>
    /// The asset is a .png file, and can be purposed as a <see cref="TerraFX.Interop.DirectX.ID3D11Texture2D"/>.
    /// </summary>
    TextureFromPng = 10,

    /// <summary>
    /// The asset is a raw texture, and can be purposed as a <see cref="TerraFX.Interop.DirectX.ID3D11Texture2D"/>.
    /// </summary>
    TextureFromRaw = 1001,

    /// <summary>
    /// The asset is a font file.
    /// </summary>
    Font = 2000,
}

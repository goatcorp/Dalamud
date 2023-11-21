namespace Dalamud.Storage.Assets;

/// <summary>
/// Stores the basic information of a Dalamud asset.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class DalamudAssetAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetAttribute"/> class.
    /// </summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="data">The data.</param>
    /// <param name="required">Whether the asset is required.</param>
    public DalamudAssetAttribute(DalamudAssetPurpose purpose, byte[]? data = null, bool required = true)
    {
        this.Purpose = purpose;
        this.Data = data;
        this.Required = required;
    }

    /// <summary>
    /// Gets the purpose of the asset.
    /// </summary>
    public DalamudAssetPurpose Purpose { get; }

    /// <summary>
    /// Gets the data, if available.
    /// </summary>
    public byte[]? Data { get; }

    /// <summary>
    /// Gets a value indicating whether the asset is required.
    /// </summary>
    public bool Required { get; }
}

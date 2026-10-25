namespace Dalamud.Storage.Assets;

/// <summary>Overrides the local asset file name for a <see cref="DalamudAsset"/> field.</summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class DalamudAssetLocalFileAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DalamudAssetLocalFileAttribute"/> class.</summary>
    /// <param name="fileName">The file name.</param>
    public DalamudAssetLocalFileAttribute(string fileName) => this.FileName = fileName;

    /// <summary>Gets the file name.</summary>
    public string FileName { get; }
}

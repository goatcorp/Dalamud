namespace Dalamud.Storage.Assets;

/// <summary>Names the file that stands for an asset in the local asset folder, instead of the name of the asset.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class DalamudAssetLocalFileAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DalamudAssetLocalFileAttribute"/> class.</summary>
    /// <param name="fileName">The file name.</param>
    public DalamudAssetLocalFileAttribute(string fileName) => this.FileName = fileName;

    /// <summary>Gets the file name.</summary>
    public string FileName { get; }
}

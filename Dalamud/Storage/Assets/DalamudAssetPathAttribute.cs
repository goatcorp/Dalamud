using System.IO;

namespace Dalamud.Storage.Assets;

/// <summary>
/// File names to look up in Dalamud assets.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal class DalamudAssetPathAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetPathAttribute"/> class.
    /// </summary>
    /// <param name="pathComponents">The path components.</param>
    public DalamudAssetPathAttribute(params string[] pathComponents) => this.FileName = Path.Join(pathComponents);

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; }
}

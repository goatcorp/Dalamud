using System.IO;
using System.Numerics;

using Dalamud.Interface.Internal;

namespace Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

/// <summary>Represents the use of a texture from a file as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
internal class FilePathNotificationIcon : INotificationIcon
{
    private readonly FileInfo fileInfo;

    /// <summary>Initializes a new instance of the <see cref="FilePathNotificationIcon"/> class.</summary>
    /// <param name="filePath">The path to a .tex file inside the game resources.</param>
    public FilePathNotificationIcon(string filePath) => this.fileInfo = new(filePath);

    /// <inheritdoc/>
    public bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color) =>
        NotificationUtilities.DrawIconFrom(
            minCoord,
            maxCoord,
            Service<TextureManager>.Get().GetTextureFromFile(this.fileInfo));

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is FilePathNotificationIcon r && r.fileInfo.FullName == this.fileInfo.FullName;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.GetType().GetHashCode(), this.fileInfo.FullName);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(FilePathNotificationIcon)}({this.fileInfo.FullName})";
}

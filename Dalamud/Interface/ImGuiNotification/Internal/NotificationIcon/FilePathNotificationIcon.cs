using System.Numerics;

using Dalamud.Interface.Textures.Internal;

namespace Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

/// <summary>Represents the use of a texture from a file as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
internal class FilePathNotificationIcon : INotificationIcon
{
    private readonly string filePath;

    /// <summary>Initializes a new instance of the <see cref="FilePathNotificationIcon"/> class.</summary>
    /// <param name="filePath">The path to a .tex file inside the game resources.</param>
    public FilePathNotificationIcon(string filePath) => this.filePath = new(filePath);

    /// <inheritdoc/>
    public bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color) =>
        NotificationUtilities.DrawIconFrom(
            minCoord,
            maxCoord,
            Service<TextureManager>.Get().Shared.GetFromFile(this.filePath).GetWrapOrDefault());

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is FilePathNotificationIcon r && r.filePath == this.filePath;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.GetType().GetHashCode(), this.filePath);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(FilePathNotificationIcon)}({this.filePath})";
}

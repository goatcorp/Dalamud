using System.Numerics;

using Dalamud.Interface.Textures.Internal;

namespace Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

/// <summary>Represents the use of a game-shipped texture as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
internal class GamePathNotificationIcon : INotificationIcon
{
    private readonly string gamePath;

    /// <summary>Initializes a new instance of the <see cref="GamePathNotificationIcon"/> class.</summary>
    /// <param name="gamePath">The path to a .tex file inside the game resources.</param>
    public GamePathNotificationIcon(string gamePath) => this.gamePath = gamePath;

    /// <inheritdoc/>
    public bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color) =>
        NotificationUtilities.DrawIconFrom(
            minCoord,
            maxCoord,
            Service<TextureManager>.Get().Shared.GetFromGame(this.gamePath).GetWrapOrDefault());

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GamePathNotificationIcon r && r.gamePath == this.gamePath;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.GetType().GetHashCode(), this.gamePath);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(GamePathNotificationIcon)}({this.gamePath})";
}

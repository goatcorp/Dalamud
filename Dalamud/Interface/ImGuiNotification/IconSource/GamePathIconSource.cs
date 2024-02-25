using System.Numerics;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.ImGuiNotification.IconSource;

/// <summary>Represents the use of a game-shipped texture as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
public readonly struct GamePathIconSource : INotificationIconSource.IInternal
{
    /// <summary>The path to a .tex file inside the game resources.</summary>
    public readonly string GamePath;

    /// <summary>Initializes a new instance of the <see cref="GamePathIconSource"/> struct.</summary>
    /// <param name="gamePath">The path to a .tex file inside the game resources.</param>
    /// <remarks>Use <see cref="ITextureProvider.GetIconPath"/> to get the game path from icon IDs.</remarks>
    public GamePathIconSource(string gamePath) => this.GamePath = gamePath;

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
    }

    /// <inheritdoc/>
    INotificationMaterializedIcon INotificationIconSource.IInternal.Materialize() =>
        new MaterializedIcon(this.GamePath);

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private readonly string gamePath;

        public MaterializedIcon(string gamePath) => this.gamePath = gamePath;

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawTexture(
                Service<TextureManager>.Get().GetTextureFromGame(this.gamePath),
                minCoord,
                maxCoord,
                initiatorPlugin);
    }
}

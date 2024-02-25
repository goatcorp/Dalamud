using System.IO;
using System.Numerics;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.ImGuiNotification.IconSource;

/// <summary>Represents the use of a texture from a file as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
public readonly struct FilePathIconSource : INotificationIconSource.IInternal
{
    /// <summary>The path to a .tex file inside the game resources.</summary>
    public readonly string FilePath;

    /// <summary>Initializes a new instance of the <see cref="FilePathIconSource"/> struct.</summary>
    /// <param name="filePath">The path to a .tex file inside the game resources.</param>
    public FilePathIconSource(string filePath) => this.FilePath = filePath;

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
    }

    /// <inheritdoc/>
    INotificationMaterializedIcon INotificationIconSource.IInternal.Materialize() =>
        new MaterializedIcon(this.FilePath);

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private readonly FileInfo fileInfo;

        public MaterializedIcon(string filePath) => this.fileInfo = new(filePath);

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawTexture(
                Service<TextureManager>.Get().GetTextureFromFile(this.fileInfo),
                minCoord,
                maxCoord,
                initiatorPlugin);
    }
}

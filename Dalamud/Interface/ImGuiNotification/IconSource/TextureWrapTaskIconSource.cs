using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.Interface.ImGuiNotification.IconSource;

/// <summary>Represents the use of future <see cref="IDalamudTextureWrap"/> as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
public readonly struct TextureWrapTaskIconSource : INotificationIconSource.IInternal
{
    /// <summary>The function that returns a task resulting in a new instance of <see cref="IDalamudTextureWrap"/>.
    /// </summary>
    /// <remarks>Dalamud will take ownership of the result. Do not call <see cref="IDisposable.Dispose"/>.</remarks>
    public readonly Func<Task<IDalamudTextureWrap?>?>? TextureWrapTaskFunc;

    /// <summary>Gets the default materialized icon, for the purpose of displaying the plugin icon.</summary>
    internal static readonly INotificationMaterializedIcon DefaultMaterializedIcon = new MaterializedIcon(null);

    /// <summary>Initializes a new instance of the <see cref="TextureWrapTaskIconSource"/> struct.</summary>
    /// <param name="taskFunc">The function.</param>
    public TextureWrapTaskIconSource(Func<Task<IDalamudTextureWrap?>?>? taskFunc) =>
        this.TextureWrapTaskFunc = taskFunc;

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
    }

    /// <inheritdoc/>
    INotificationMaterializedIcon INotificationIconSource.IInternal.Materialize() =>
        new MaterializedIcon(this.TextureWrapTaskFunc);

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private Task<IDalamudTextureWrap>? task;

        public MaterializedIcon(Func<Task<IDalamudTextureWrap?>?>? taskFunc) => this.task = taskFunc?.Invoke();

        public void Dispose()
        {
            this.task?.ToContentDisposedTask(true);
            this.task = null;
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawTexture(
                this.task?.IsCompletedSuccessfully is true ? this.task.Result : null,
                minCoord,
                maxCoord,
                initiatorPlugin);
    }
}

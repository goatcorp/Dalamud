using System.Numerics;
using System.Threading;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.ImGuiNotification.Internal.IconSource;

/// <summary>Represents the use of future <see cref="IDalamudTextureWrap"/> as the icon of a notification.</summary>
/// <remarks>If there was no texture loaded for any reason, the plugin icon will be displayed instead.</remarks>
internal class TextureWrapIconSource : INotificationIconSource.IInternal
{
    private IDalamudTextureWrap? wrap;

    /// <summary>Initializes a new instance of the <see cref="TextureWrapIconSource"/> class.</summary>
    /// <param name="wrap">The texture wrap to handle over the ownership.</param>
    /// <param name="takeOwnership">
    /// If <c>true</c>, this class will own the passed <paramref name="wrap"/>, and you <b>must not</b> call
    /// <see cref="IDisposable.Dispose"/> on the passed wrap.
    /// If <c>false</c>, this class will create a new reference of the passed wrap, and you <b>should</b> call
    /// <see cref="IDisposable.Dispose"/> on the passed wrap.
    /// In both cases, this class must be disposed after use.</param>
    public TextureWrapIconSource(IDalamudTextureWrap? wrap, bool takeOwnership) =>
        this.wrap = takeOwnership ? wrap : wrap?.CreateWrapSharingLowLevelResource();

    /// <summary>Gets the underlying texture wrap.</summary>
    public IDalamudTextureWrap? Wrap => this.wrap;

    /// <inheritdoc/>
    public INotificationIconSource Clone() => new TextureWrapIconSource(this.wrap, false);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.wrap, null) is { } w)
            w.Dispose();
    }

    /// <inheritdoc/>
    public INotificationMaterializedIcon Materialize() =>
        new MaterializedIcon(this.wrap?.CreateWrapSharingLowLevelResource());

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private IDalamudTextureWrap? wrap;

        public MaterializedIcon(IDalamudTextureWrap? wrap) => this.wrap = wrap;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.wrap, null) is { } w)
                w.Dispose();
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawTexture(
                this.wrap,
                minCoord,
                maxCoord,
                initiatorPlugin);
    }
}

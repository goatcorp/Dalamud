using System.Numerics;

using Dalamud.Game.Text;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.ImGuiNotification.Internal.IconSource;

/// <summary>Represents the use of <see cref="SeIconChar"/> as the icon of a notification.</summary>
internal class SeIconCharIconSource : INotificationIconSource.IInternal
{
    /// <summary>Initializes a new instance of the <see cref="SeIconCharIconSource"/> class.</summary>
    /// <param name="c">The character.</param>
    public SeIconCharIconSource(SeIconChar c) => this.IconChar = c;

    /// <summary>Gets the icon character.</summary>
    public SeIconChar IconChar { get; }

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public INotificationMaterializedIcon Materialize() => new MaterializedIcon(this.IconChar);

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private readonly char iconChar;

        public MaterializedIcon(SeIconChar c) => this.iconChar = c.ToIconChar();

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawIconString(
                Service<NotificationManager>.Get().IconAxisFontHandle,
                this.iconChar,
                minCoord,
                maxCoord,
                color);
    }
}

using System.Numerics;

using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.ImGuiNotification.Internal.IconSource;

/// <summary>Represents the use of <see cref="FontAwesomeIcon"/> as the icon of a notification.</summary>
internal class FontAwesomeIconIconSource : INotificationIconSource.IInternal
{
    /// <summary>Initializes a new instance of the <see cref="FontAwesomeIconIconSource"/> class.</summary>
    /// <param name="iconChar">The character.</param>
    public FontAwesomeIconIconSource(FontAwesomeIcon iconChar) => this.IconChar = iconChar;

    /// <summary>Gets the icon character.</summary>
    public FontAwesomeIcon IconChar { get; }

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

        public MaterializedIcon(FontAwesomeIcon c) => this.iconChar = c.ToIconChar();

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            NotificationUtilities.DrawIconString(
                Service<NotificationManager>.Get().IconFontAwesomeFontHandle,
                this.iconChar,
                minCoord,
                maxCoord,
                color);
    }
}

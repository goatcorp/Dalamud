using System.Numerics;

namespace Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

/// <summary>Represents the use of <see cref="FontAwesomeIcon"/> as the icon of a notification.</summary>
internal class FontAwesomeIconNotificationIcon : INotificationIcon
{
    private readonly char iconChar;

    /// <summary>Initializes a new instance of the <see cref="FontAwesomeIconNotificationIcon"/> class.</summary>
    /// <param name="iconChar">The character.</param>
    public FontAwesomeIconNotificationIcon(FontAwesomeIcon iconChar) => this.iconChar = (char)iconChar;

    /// <inheritdoc/>
    public bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color) =>
        NotificationUtilities.DrawIconFrom(
            minCoord,
            maxCoord,
            this.iconChar,
            Service<NotificationManager>.Get().IconFontAwesomeFontHandle,
            color);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FontAwesomeIconNotificationIcon r && r.iconChar == this.iconChar;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.GetType().GetHashCode(), this.iconChar);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(FontAwesomeIconNotificationIcon)}({this.iconChar})";
}

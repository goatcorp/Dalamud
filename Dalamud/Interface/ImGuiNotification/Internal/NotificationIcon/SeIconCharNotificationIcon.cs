using System.Numerics;

using Dalamud.Game.Text;

namespace Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

/// <summary>Represents the use of <see cref="SeIconChar"/> as the icon of a notification.</summary>
internal class SeIconCharNotificationIcon : INotificationIcon
{
    private readonly SeIconChar iconChar;

    /// <summary>Initializes a new instance of the <see cref="SeIconCharNotificationIcon"/> class.</summary>
    /// <param name="c">The character.</param>
    public SeIconCharNotificationIcon(SeIconChar c) => this.iconChar = c;

    /// <inheritdoc/>
    public bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color) =>
        NotificationUtilities.DrawIconFrom(
            minCoord,
            maxCoord,
            (char)this.iconChar,
            Service<NotificationManager>.Get().IconAxisFontHandle,
            color);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SeIconCharNotificationIcon r && r.iconChar == this.iconChar;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.GetType().GetHashCode(), this.iconChar);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SeIconCharNotificationIcon)}({this.iconChar})";
}

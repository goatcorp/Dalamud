namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>
/// Where notifications should snap to on the screen when they are shown.
/// </summary>
public enum NotificationSnapDirection
{
    /// <summary>
    /// Snap to the top of the screen.
    /// </summary>
    Top,

    /// <summary>
    /// Snap to the bottom of the screen.
    /// </summary>
    Bottom,

    /// <summary>
    /// Snap to the left of the screen.
    /// </summary>
    Left,

    /// <summary>
    /// Snap to the right of the screen.
    /// </summary>
    Right,
}

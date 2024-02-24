namespace Dalamud.Interface.ImGuiNotification;

/// <summary>
/// Delegate representing the dismissal of an active notification.
/// </summary>
/// <param name="notification">The notification being dismissed.</param>
/// <param name="dismissReason">The reason of dismissal.</param>
public delegate void NotificationDismissedDelegate(
    IActiveNotification notification,
    NotificationDismissReason dismissReason);

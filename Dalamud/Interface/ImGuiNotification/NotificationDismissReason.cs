namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Specifies the reason of dismissal for a notification.</summary>
public enum NotificationDismissReason
{
    /// <summary>The notification is dismissed because the expiry specified from <see cref="INotification.HardExpiry"/> is
    /// met.</summary>
    Timeout = 1,

    /// <summary>The notification is dismissed because the user clicked on the close button on a notification window.
    /// </summary>
    Manual = 2,

    /// <summary>The notification is dismissed from calling <see cref="IActiveNotification.DismissNow"/>.</summary>
    Programmatical = 3,
}

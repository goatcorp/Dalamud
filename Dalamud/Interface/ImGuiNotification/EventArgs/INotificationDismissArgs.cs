namespace Dalamud.Interface.ImGuiNotification.EventArgs;

/// <summary>Arguments for use with <see cref="IActiveNotification.Dismiss"/>.</summary>
/// <remarks>Not to be implemented by plugins.</remarks>
public interface INotificationDismissArgs
{
    /// <summary>Gets the notification being dismissed.</summary>
    IActiveNotification Notification { get; }

    /// <summary>Gets the dismiss reason.</summary>
    NotificationDismissReason Reason { get; }
}

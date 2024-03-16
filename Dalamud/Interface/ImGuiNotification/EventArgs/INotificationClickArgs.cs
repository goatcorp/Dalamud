namespace Dalamud.Interface.ImGuiNotification.EventArgs;

/// <summary>Arguments for use with <see cref="IActiveNotification.Click"/>.</summary>
/// <remarks>Not to be implemented by plugins.</remarks>
public interface INotificationClickArgs
{
    /// <summary>Gets the notification being clicked.</summary>
    IActiveNotification Notification { get; }
}

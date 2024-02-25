using Dalamud.Interface.ImGuiNotification;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Manager for notifications provided by Dalamud using ImGui.
/// </summary>
public interface INotificationManager
{
    /// <summary>
    /// Adds a notification.
    /// </summary>
    /// <param name="notification">The new notification.</param>
    /// <param name="disposeNotification">
    /// Dispose <paramref name="notification"/> when this function returns, even if the function throws an exception.
    /// Set to <c>false</c> to reuse <paramref name="notification"/> for multiple calls to this function, in which case,
    /// you should call <see cref="IDisposable.Dispose"/> on the value supplied to <paramref name="notification"/> at a
    /// later time.
    /// </param>
    /// <returns>The added notification.</returns>
    IActiveNotification AddNotification(Notification notification, bool disposeNotification = true);
}

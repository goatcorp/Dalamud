using Dalamud.Interface.ImGuiNotification;

namespace Dalamud.Plugin.Services;

/// <summary>Manager for notifications provided by Dalamud using ImGui.</summary>
public interface INotificationManager
{
    /// <summary>Adds a notification.</summary>
    /// <param name="notification">The new notification.</param>
    /// <returns>The added notification.</returns>
    IActiveNotification AddNotification(Notification notification);
}

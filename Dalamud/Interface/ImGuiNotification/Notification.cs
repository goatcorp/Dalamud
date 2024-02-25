using System.Threading.Tasks;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>
/// Represents a blueprint for a notification.
/// </summary>
public sealed record Notification : INotification
{
    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public NotificationType Type { get; set; } = NotificationType.None;

    /// <inheritdoc/>
    public Func<Task<object>>? IconCreator { get; set; }

    /// <inheritdoc/>
    public DateTime Expiry { get; set; } = DateTime.Now + NotificationConstants.DefaultDisplayDuration;

    /// <inheritdoc/>
    public bool Interactible { get; set; }

    /// <inheritdoc/>
    public TimeSpan HoverExtendDuration { get; set; } = NotificationConstants.DefaultHoverExtendDuration;

    /// <inheritdoc/>
    public float Progress { get; set; } = 1f;
}

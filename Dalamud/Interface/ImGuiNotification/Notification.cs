using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents a blueprint for a notification.</summary>
public sealed record Notification : INotification
{
    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public NotificationType Type { get; set; } = NotificationType.None;

    /// <inheritdoc/>
    public INotificationIconSource? IconSource { get; set; }

    /// <inheritdoc/>
    public DateTime Expiry { get; set; } = DateTime.Now + NotificationConstants.DefaultDisplayDuration;

    /// <inheritdoc/>
    public bool ShowIndeterminateIfNoExpiry { get; set; } = true;

    /// <inheritdoc/>
    public bool Interactable { get; set; } = true;

    /// <inheritdoc/>
    public bool UserDismissable { get; set; } = true;

    /// <inheritdoc/>
    public TimeSpan HoverExtendDuration { get; set; } = NotificationConstants.DefaultHoverExtendDuration;

    /// <inheritdoc/>
    public float Progress { get; set; } = 1f;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.IconSource?.Dispose();
        this.IconSource = null;
    }
}

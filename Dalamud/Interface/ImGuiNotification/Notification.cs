using System.Threading;

using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents a blueprint for a notification.</summary>
public sealed record Notification : INotification
{
    private INotificationIconSource? iconSource;

    /// <summary>Initializes a new instance of the <see cref="Notification"/> class.</summary>
    public Notification()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Notification"/> class.</summary>
    /// <param name="notification">The instance of <see cref="INotification"/> to copy from.</param>
    public Notification(INotification notification) => this.CopyValuesFrom(notification);

    /// <summary>Initializes a new instance of the <see cref="Notification"/> class.</summary>
    /// <param name="notification">The instance of <see cref="Notification"/> to copy from.</param>
    public Notification(Notification notification) => this.CopyValuesFrom(notification);

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public string? MinimizedText { get; set; }

    /// <inheritdoc/>
    public NotificationType Type { get; set; } = NotificationType.None;

    /// <inheritdoc/>
    public INotificationIconSource? IconSource
    {
        get => this.iconSource;
        set
        {
            var prevSource = Interlocked.Exchange(ref this.iconSource, value);
            if (prevSource != value)
                prevSource?.Dispose();
        }
    }

    /// <inheritdoc/>
    public DateTime HardExpiry { get; set; } = DateTime.MaxValue;

    /// <inheritdoc/>
    public TimeSpan InitialDuration { get; set; } = NotificationConstants.DefaultDisplayDuration;

    /// <inheritdoc/>
    public TimeSpan DurationSinceLastInterest { get; set; } = NotificationConstants.DefaultHoverExtendDuration;

    /// <inheritdoc/>
    public bool ShowIndeterminateIfNoExpiry { get; set; } = true;

    /// <inheritdoc/>
    public bool Minimized { get; set; } = true;

    /// <inheritdoc/>
    public bool UserDismissable { get; set; } = true;

    /// <inheritdoc/>
    public float Progress { get; set; } = 1f;

    /// <inheritdoc/>
    public void Dispose()
    {
        // Assign to the property; it will take care of disposing
        this.IconSource = null;
    }

    /// <summary>Copy values from the given instance of <see cref="INotification"/>.</summary>
    /// <param name="copyFrom">The instance of <see cref="INotification"/> to copy from.</param>
    private void CopyValuesFrom(INotification copyFrom)
    {
        this.Content = copyFrom.Content;
        this.Title = copyFrom.Title;
        this.MinimizedText = copyFrom.MinimizedText;
        this.Type = copyFrom.Type;
        this.IconSource = copyFrom.IconSource?.Clone();
        this.HardExpiry = copyFrom.HardExpiry;
        this.InitialDuration = copyFrom.InitialDuration;
        this.DurationSinceLastInterest = copyFrom.DurationSinceLastInterest;
        this.ShowIndeterminateIfNoExpiry = copyFrom.ShowIndeterminateIfNoExpiry;
        this.Minimized = copyFrom.Minimized;
        this.UserDismissable = copyFrom.UserDismissable;
        this.Progress = copyFrom.Progress;
    }
}

using Dalamud.Interface.ImGuiNotification.Internal.IconSource;
using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents a notification.</summary>
public interface INotification : IDisposable
{
    /// <summary>Gets the content body of the notification.</summary>
    string Content { get; }

    /// <summary>Gets the title of the notification.</summary>
    string? Title { get; }

    /// <summary>Gets the type of the notification.</summary>
    NotificationType Type { get; }

    /// <summary>Gets the icon source.</summary>
    /// <remarks>
    /// The assigned value will be disposed upon the <see cref="IDisposable.Dispose"/> call on this instance of
    /// <see cref="INotification"/>.<br />
    /// <br /> 
    /// The following icon sources are currently available.<br />
    /// <ul>
    /// <li><see cref="SeIconCharIconSource"/></li>
    /// <li><see cref="FontAwesomeIconIconSource"/></li>
    /// <li><see cref="TextureWrapIconSource"/></li>
    /// <li><see cref="TextureWrapTaskIconSource"/></li>
    /// <li><see cref="GamePathIconSource"/></li>
    /// <li><see cref="FilePathIconSource"/></li>
    /// </ul>
    /// </remarks>
    INotificationIconSource? IconSource { get; }

    /// <summary>Gets the expiry.</summary>
    /// <remarks>Set to <see cref="DateTime.MaxValue"/> to make the notification not have an expiry time
    /// (sticky, indeterminate, permanent, or persistent).</remarks>
    DateTime Expiry { get; }

    /// <summary>Gets a value indicating whether to show an indeterminate expiration animation if <see cref="Expiry"/>
    /// is set to <see cref="DateTime.MaxValue"/>.</summary>
    bool ShowIndeterminateIfNoExpiry { get; }

    /// <summary>Gets a value indicating whether this notification may be interacted.</summary>
    /// <remarks>
    /// Set this value to <c>true</c> if you want to respond to user inputs from
    /// <see cref="IActiveNotification.DrawActions"/>.
    /// Note that the close buttons for notifications are always provided and interactable.
    /// If set to <c>true</c>, then clicking on the notification itself will be interpreted as user-initiated dismissal,
    /// unless <see cref="IActiveNotification.Click"/> is set or <see cref="UserDismissable"/> is unset.
    /// </remarks>
    bool Interactable { get; }

    /// <summary>Gets a value indicating whether the user can dismiss the notification by themselves.</summary>
    /// <remarks>Consider adding a cancel button to <see cref="IActiveNotification.DrawActions"/>.</remarks>
    bool UserDismissable { get; }

    /// <summary>Gets the new duration for this notification if mouse cursor is on the notification window.</summary>
    /// <remarks>
    /// If set to <see cref="TimeSpan.Zero"/> or less, then this feature is turned off.
    /// This property is applicable regardless of <see cref="Interactable"/>.
    /// </remarks>
    TimeSpan HoverExtendDuration { get; }

    /// <summary>Gets the progress for the background progress bar of the notification.</summary>
    /// <remarks>The progress should be in the range between 0 and 1.</remarks>
    float Progress { get; }
}

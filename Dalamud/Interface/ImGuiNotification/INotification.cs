using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents a notification.</summary>
public interface INotification : IDisposable
{
    /// <summary>Gets or sets the content body of the notification.</summary>
    string Content { get; set; }

    /// <summary>Gets or sets the title of the notification.</summary>
    string? Title { get; set; }

    /// <summary>Gets or sets the text to display when the notification is minimized.</summary>
    string? MinimizedText { get; set; }

    /// <summary>Gets or sets the type of the notification.</summary>
    NotificationType Type { get; set; }

    /// <summary>Gets or sets the icon source.</summary>
    /// <remarks>
    /// <para>Assigning a new value that does not equal to the previous value will dispose the old value. The ownership
    /// of the new value is transferred to this <see cref="INotification"/>. <b>Even if the assignment throws an
    /// exception</b>, the ownership is transferred, causing the value to be disposed. Assignment should not throw an
    /// exception though, so wrapping the assignment in try...catch block is not required.</para>
    /// <para>The assigned value will be disposed upon the <see cref="IDisposable.Dispose"/> call on this instance of
    /// <see cref="INotification"/>, unless the same value is assigned, in which case it will do nothing.</para>
    /// <para>If this <see cref="INotification"/> is an <see cref="IActiveNotification"/>, then updating this property
    /// will change the icon being displayed (calls <see cref="IActiveNotification.UpdateIcon"/>), unless
    /// <see cref="IActiveNotification.IsDismissed"/> is <c>true</c>.</para>
    /// </remarks>
    INotificationIconSource? IconSource { get; set; }

    /// <summary>Gets or sets the hard expiry.</summary>
    /// <remarks>
    /// Setting this value will override <see cref="InitialDuration"/> and <see cref="DurationSinceLastInterest"/>, in that
    /// the notification will be dismissed when this expiry expires.<br />
    /// Set to <see cref="DateTime.MaxValue"/> to make only <see cref="InitialDuration"/> take effect.<br />
    /// If neither <see cref="HardExpiry"/> nor <see cref="InitialDuration"/> is not MaxValue, then the notification
    /// will not expire after a set time. It must be explicitly dismissed by the user of via calling
    /// <see cref="IActiveNotification.DismissNow"/>.<br />
    /// Updating this value will reset the dismiss timer.
    /// </remarks>
    DateTime HardExpiry { get; set; }

    /// <summary>Gets or sets the initial duration.</summary>
    /// <remarks>Set to <see cref="TimeSpan.MaxValue"/> to make only <see cref="HardExpiry"/> take effect.</remarks>
    /// <remarks>Updating this value will reset the dismiss timer.</remarks>
    TimeSpan InitialDuration { get; set; }

    /// <summary>Gets or sets the new duration for this notification once the mouse cursor leaves the window and the
    /// window is no longer focused.</summary>
    /// <remarks>
    /// If set to <see cref="TimeSpan.Zero"/> or less, then this feature is turned off, and hovering the mouse on the
    /// notification or focusing on it will not make the notification stay.<br />
    /// Updating this value will reset the dismiss timer.
    /// </remarks>
    TimeSpan DurationSinceLastInterest { get; set; }

    /// <summary>Gets or sets a value indicating whether to show an indeterminate expiration animation if
    /// <see cref="HardExpiry"/> is set to <see cref="DateTime.MaxValue"/>.</summary>
    bool ShowIndeterminateIfNoExpiry { get; set; }

    /// <summary>Gets or sets a value indicating whether the notification has been minimized.</summary>
    bool Minimized { get; set; }

    /// <summary>Gets or sets a value indicating whether the user can dismiss the notification by themselves.</summary>
    /// <remarks>Consider adding a cancel button to <see cref="IActiveNotification.DrawActions"/>.</remarks>
    bool UserDismissable { get; set; }

    /// <summary>Gets or sets the progress for the background progress bar of the notification.</summary>
    /// <remarks>The progress should be in the range between 0 and 1.</remarks>
    float Progress { get; set; }
}

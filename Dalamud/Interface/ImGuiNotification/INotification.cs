using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents a notification.</summary>
/// <remarks>Not to be implemented by plugins.</remarks>
public interface INotification
{
    /// <summary>Gets or sets the content body of the notification.</summary>
    string Content { get; set; }

    /// <summary>Gets or sets the title of the notification.</summary>
    string? Title { get; set; }

    /// <summary>Gets or sets the text to display when the notification is minimized.</summary>
    string? MinimizedText { get; set; }

    /// <summary>Gets or sets the type of the notification.</summary>
    NotificationType Type { get; set; }

    /// <summary>Gets or sets the icon source, in case <see cref="IconTextureTask"/> is not set or the task has faulted.
    /// </summary>
    INotificationIcon? Icon { get; set; }

    /// <summary>Gets or sets a texture wrap that will be used in place of <see cref="Icon"/> if set.</summary>
    /// <remarks>
    /// <para>A texture wrap set via this property will <b>NOT</b> be disposed when the notification is dismissed.
    /// Use <see cref="IActiveNotification.SetIconTexture(IDalamudTextureWrap?)"/> or
    /// <see cref="IActiveNotification.SetIconTexture(Task{IDalamudTextureWrap?}?)"/> to use a texture, after calling
    /// <see cref="INotificationManager.AddNotification"/>. Call either of those functions with <c>null</c> to revert
    /// the effective icon back to this property.</para>
    /// <para>This property and <see cref="IconTextureTask"/> are bound together. If the task is not <c>null</c> but
    /// <see cref="Task.IsCompletedSuccessfully"/> is <c>false</c> (because the task is still in progress or faulted,)
    /// the property will return <c>null</c>. Setting this property will set <see cref="IconTextureTask"/> to a new
    /// completed <see cref="Task{TResult}"/> with the new value as its result.</para>
    /// </remarks>
    public IDalamudTextureWrap? IconTexture { get; set; }

    /// <summary>Gets or sets a task that results in a texture wrap that will be used in place of <see cref="Icon"/> if
    /// available.</summary>
    /// <remarks>
    /// <para>A texture wrap set via this property will <b>NOT</b> be disposed when the notification is dismissed.
    /// Use <see cref="IActiveNotification.SetIconTexture(IDalamudTextureWrap?)"/> or
    /// <see cref="IActiveNotification.SetIconTexture(Task{IDalamudTextureWrap?}?)"/> to use a texture, after calling
    /// <see cref="INotificationManager.AddNotification"/>. Call either of those functions with <c>null</c> to revert
    /// the effective icon back to this property.</para>
    /// <para>This property and <see cref="IconTexture"/> are bound together.</para>
    /// </remarks>
    Task<IDalamudTextureWrap?>? IconTextureTask { get; set; }

    /// <summary>Gets or sets the hard expiry.</summary>
    /// <remarks>
    /// Setting this value will override <see cref="InitialDuration"/> and <see cref="ExtensionDurationSinceLastInterest"/>, in that
    /// the notification will be dismissed when this expiry expires.<br />
    /// Set to <see cref="DateTime.MaxValue"/> to make only <see cref="InitialDuration"/> take effect.<br />
    /// If both <see cref="HardExpiry"/> and <see cref="InitialDuration"/> are MaxValue, then the notification
    /// will not expire after a set time. It must be explicitly dismissed by the user or via calling
    /// <see cref="IActiveNotification.DismissNow"/>.<br />
    /// Updating this value will reset the dismiss timer.
    /// </remarks>
    DateTime HardExpiry { get; set; }

    /// <summary>Gets or sets the initial duration.</summary>
    /// <remarks>Set to <see cref="TimeSpan.MaxValue"/> to make only <see cref="HardExpiry"/> take effect.</remarks>
    /// <remarks>Updating this value will reset the dismiss timer, but the remaining duration will still be calculated
    /// based on <see cref="IActiveNotification.CreatedAt"/>.</remarks>
    TimeSpan InitialDuration { get; set; }

    /// <summary>Gets or sets the new duration for this notification once the mouse cursor leaves the window and the
    /// window is no longer focused.</summary>
    /// <remarks>
    /// If set to <see cref="TimeSpan.Zero"/> or less, then this feature is turned off, and hovering the mouse on the
    /// notification or focusing on it will not make the notification stay.<br />
    /// Updating this value will reset the dismiss timer.
    /// </remarks>
    TimeSpan ExtensionDurationSinceLastInterest { get; set; }

    /// <summary>Gets or sets a value indicating whether to show an indeterminate expiration animation if
    /// <see cref="HardExpiry"/> is set to <see cref="DateTime.MaxValue"/>.</summary>
    bool ShowIndeterminateIfNoExpiry { get; set; }

    /// <summary>Gets or sets a value indicating whether to respect the current UI visibility state.</summary>
    bool RespectUiHidden { get; set; }

    /// <summary>Gets or sets a value indicating whether the notification has been minimized.</summary>
    bool Minimized { get; set; }

    /// <summary>Gets or sets a value indicating whether the user can dismiss the notification by themselves.</summary>
    /// <remarks>Consider adding a cancel button to <see cref="IActiveNotification.DrawActions"/>.</remarks>
    bool UserDismissable { get; set; }

    /// <summary>Gets or sets the progress for the background progress bar of the notification.</summary>
    /// <remarks>The progress should be in the range between 0 and 1.</remarks>
    float Progress { get; set; }
}

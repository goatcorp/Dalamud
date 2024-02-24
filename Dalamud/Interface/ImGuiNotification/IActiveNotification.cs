using System.Threading;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>
/// Represents an active notification.
/// </summary>
public interface IActiveNotification : INotification
{
    /// <summary>
    /// The counter for <see cref="Id"/> field.
    /// </summary>
    private static long idCounter;

    /// <summary>
    /// Invoked upon dismissing the notification.
    /// </summary>
    /// <remarks>
    /// The event callback will not be called, if a user interacts with the notification after the plugin is unloaded.
    /// </remarks>
    event NotificationDismissedDelegate Dismiss;

    /// <summary>
    /// Invoked upon clicking on the notification.
    /// </summary>
    /// <remarks>
    /// This event is not applicable when <see cref="INotification.Interactible"/> is set to <c>false</c>.
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> Click;

    /// <summary>
    /// Invoked when the mouse enters the notification window.
    /// </summary>
    /// <remarks>
    /// This event is applicable regardless of <see cref="INotification.Interactible"/>.
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> MouseEnter;

    /// <summary>
    /// Invoked when the mouse leaves the notification window.
    /// </summary>
    /// <remarks>
    /// This event is applicable regardless of <see cref="INotification.Interactible"/>.
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> MouseLeave;

    /// <summary>
    /// Invoked upon drawing the action bar of the notification.
    /// </summary>
    /// <remarks>
    /// This event is applicable regardless of <see cref="INotification.Interactible"/>.
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> DrawActions;

    /// <summary>
    /// Gets the ID of this notification.
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Gets a value indicating whether the mouse cursor is on the notification window.
    /// </summary>
    bool IsMouseHovered { get; }

    /// <summary>
    /// Gets a value indicating whether the notification has been dismissed.
    /// This includes when the hide animation is being played.
    /// </summary>
    bool IsDismissed { get; }

    /// <summary>
    /// Clones this notification as a <see cref="Notification"/>.
    /// </summary>
    /// <returns>A new instance of <see cref="Notification"/>.</returns>
    Notification CloneNotification();

    /// <summary>
    /// Dismisses this notification.
    /// </summary>
    void DismissNow();

    /// <summary>
    /// Updates the notification data.
    /// </summary>
    /// <remarks>
    /// Call <see cref="UpdateIcon"/> to update the icon using the new <see cref="INotification.IconCreator"/>.
    /// </remarks>
    /// <param name="newNotification">The new notification entry.</param>
    void Update(INotification newNotification);

    /// <summary>
    /// Loads the icon again using <see cref="INotification.IconCreator"/>.
    /// </summary>
    void UpdateIcon();

    /// <summary>
    /// Generates a new value to use for <see cref="Id"/>.
    /// </summary>
    /// <returns>The new value.</returns>
    internal static long CreateNewId() => Interlocked.Increment(ref idCounter);
}

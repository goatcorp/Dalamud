using System.Threading;

using Dalamud.Interface.ImGuiNotification.EventArgs;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents an active notification.</summary>
/// <remarks>Not to be implemented by plugins.</remarks>
public interface IActiveNotification : INotification
{
    /// <summary>The counter for <see cref="Id"/> field.</summary>
    private static long idCounter;

    /// <summary>Invoked upon dismissing the notification.</summary>
    /// <remarks>The event callback will not be called, if it gets dismissed after plugin unload.</remarks>
    event Action<INotificationDismissArgs> Dismiss;

    /// <summary>Invoked upon clicking on the notification.</summary>
    /// <remarks>Note that this function may be called even after <see cref="Dismiss"/> has been invoked.</remarks>
    event Action<INotificationClickArgs> Click;

    /// <summary>Invoked upon drawing the action bar of the notification.</summary>
    /// <remarks>Note that this function may be called even after <see cref="Dismiss"/> has been invoked.</remarks>
    event Action<INotificationDrawArgs> DrawActions;

    /// <summary>Gets the ID of this notification.</summary>
    /// <remarks>This value does not change.</remarks>
    long Id { get; }

    /// <summary>Gets the time of creating this notification.</summary>
    /// <remarks>This value does not change.</remarks>
    DateTime CreatedAt { get; }

    /// <summary>Gets the effective expiry time.</summary>
    /// <remarks>Contains <see cref="DateTime.MaxValue"/> if the notification does not expire.</remarks>
    /// <remarks>This value will change depending on property changes and user interactions.</remarks>
    DateTime EffectiveExpiry { get; }

    /// <summary>Gets the reason how this notification got dismissed. <c>null</c> if not dismissed.</summary>
    /// <remarks>This includes when the hide animation is being played.</remarks>
    NotificationDismissReason? DismissReason { get; }

    /// <summary>Dismisses this notification.</summary>
    /// <remarks>If the notification has already been dismissed, this function does nothing.</remarks>
    void DismissNow();

    /// <summary>Extends this notifiation.</summary>
    /// <param name="extension">The extension time.</param>
    /// <remarks>This does not override <see cref="INotification.HardExpiry"/>.</remarks>
    void ExtendBy(TimeSpan extension);

    /// <summary>Generates a new value to use for <see cref="Id"/>.</summary>
    /// <returns>The new value.</returns>
    internal static long CreateNewId() => Interlocked.Increment(ref idCounter);
}

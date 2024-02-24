using System.Threading.Tasks;

using Dalamud.Game.Text;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>
/// Represents a notification.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Gets the content body of the notification.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the title of the notification.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the type of the notification.
    /// </summary>
    NotificationType Type { get; }

    /// <summary>
    /// Gets the icon creator function for the notification.<br />
    /// Currently <see cref="IDalamudTextureWrap"/>, <see cref="SeIconChar"/>, and <see cref="FontAwesomeIcon"/> types
    /// are accepted.
    /// </summary>
    /// <remarks>
    /// The icon created by the task returned will be owned by Dalamud,
    /// i.e. it will be <see cref="IDisposable.Dispose"/>d automatically as needed.<br />
    /// If <c>null</c> is supplied for this property or <see cref="Task.IsCompletedSuccessfully"/> of the returned task
    /// is <c>false</c>, then the corresponding icon with <see cref="Type"/> will be used.<br />
    /// Use <see cref="Task.FromResult{TResult}"/> if you have an instance of <see cref="IDalamudTextureWrap"/> that you
    /// can transfer ownership to Dalamud and is available for use right away.
    /// </remarks>
    Func<Task<object>>? IconCreator { get; }

    /// <summary>
    /// Gets the expiry.
    /// </summary>
    DateTime Expiry { get; }
    
    /// <summary>
    /// Gets a value indicating whether this notification may be interacted.
    /// </summary>
    /// <remarks>
    /// Set this value to <c>true</c> if you want to respond to user inputs from
    /// <see cref="IActiveNotification.DrawActions"/>.
    /// Note that the close buttons for notifications are always provided and interactible.
    /// </remarks>
    bool Interactible { get; }
    
    /// <summary>
    /// Gets a value indicating whether clicking on the notification window counts as dismissing the notification.
    /// </summary>
    /// <remarks>
    /// This property has no effect if <see cref="Interactible"/> is <c>false</c>.
    /// </remarks>
    bool ClickIsDismiss { get; }

    /// <summary>
    /// Gets the new duration for this notification if mouse cursor is on the notification window.
    /// If set to <see cref="TimeSpan.Zero"/> or less, then this feature is turned off.
    /// </summary>
    /// <remarks>
    /// This property is applicable regardless of <see cref="Interactible"/>.
    /// </remarks>
    TimeSpan HoverExtendDuration { get; }
}

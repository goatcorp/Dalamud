using System.Threading;

using Dalamud.Interface.Internal;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Represents an active notification.</summary>
public interface IActiveNotification : INotification
{
    /// <summary>The counter for <see cref="Id"/> field.</summary>
    private static long idCounter;

    /// <summary>Invoked upon dismissing the notification.</summary>
    /// <remarks>The event callback will not be called,
    /// if a user interacts with the notification after the plugin is unloaded.</remarks>
    event NotificationDismissedDelegate Dismiss;

    /// <summary>Invoked upon clicking on the notification.</summary>
    /// <remarks>
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> Click;

    /// <summary>Invoked upon drawing the action bar of the notification.</summary>
    /// <remarks>
    /// Note that this function may be called even after <see cref="Dismiss"/> has been invoked.
    /// Refer to <see cref="IsDismissed"/>.
    /// </remarks>
    event Action<IActiveNotification> DrawActions;

    /// <summary>Gets the ID of this notification.</summary>
    long Id { get; }

    /// <summary>Gets the time of creating this notification.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Gets the effective expiry time.</summary>
    /// <remarks>Contains <see cref="DateTime.MaxValue"/> if the notification does not expire.</remarks>
    DateTime EffectiveExpiry { get; }

    /// <summary>Gets a value indicating whether the notification has been dismissed.</summary>
    /// <remarks>This includes when the hide animation is being played.</remarks>
    bool IsDismissed { get; }

    /// <summary>Dismisses this notification.</summary>
    void DismissNow();

    /// <summary>Extends this notifiation.</summary>
    /// <param name="extension">The extension time.</param>
    /// <remarks>This does not override <see cref="INotification.HardExpiry"/>.</remarks>
    void ExtendBy(TimeSpan extension);

    /// <summary>Sets the icon from <see cref="IDalamudTextureWrap"/>, overriding the icon .</summary>
    /// <param name="textureWrap">The new texture wrap to use, or null to clear and revert back to the icon specified
    /// from <see cref="INotification.Icon"/>.</param>
    /// <remarks>
    /// <para>The texture passed will be disposed when the notification is dismissed or a new different texture is set
    /// via another call to this function. You do not have to dispose it yourself.</para>
    /// <para>If <see cref="IsDismissed"/> is <c>true</c>, then calling this function will simply dispose the passed
    /// <paramref name="textureWrap"/> without actually updating the icon.</para>
    /// </remarks>
    void SetIconTexture(IDalamudTextureWrap? textureWrap);

    /// <summary>Generates a new value to use for <see cref="Id"/>.</summary>
    /// <returns>The new value.</returns>
    internal static long CreateNewId() => Interlocked.Increment(ref idCounter);
}

using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.ImGuiNotification.EventArgs;
using Dalamud.Interface.Internal;

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

    /// <summary>Sets the icon from <see cref="IDalamudTextureWrap"/>, overriding the icon.</summary>
    /// <param name="textureWrap">The new texture wrap to use, or null to clear and revert back to the icon specified
    /// from <see cref="INotification.Icon"/>.</param>
    /// <remarks>
    /// <para>The texture passed will be disposed when the notification is dismissed or a new different texture is set
    /// via another call to this function or overwriting the property. You do not have to dispose it yourself.</para>
    /// <para>If <see cref="DismissReason"/> is not <c>null</c>, then calling this function will simply dispose the
    /// passed <paramref name="textureWrap"/> without actually updating the icon.</para>
    /// </remarks>
    void SetIconTexture(IDalamudTextureWrap? textureWrap);

    /// <summary>Sets the icon from <see cref="IDalamudTextureWrap"/>, overriding the icon, once the given task
    /// completes.</summary>
    /// <param name="textureWrapTask">The task that will result in a new texture wrap to use, or null to clear and
    /// revert back to the icon specified from <see cref="INotification.Icon"/>.</param>
    /// <remarks>
    /// <para>The texture resulted from the passed <see cref="Task{TResult}"/> will be disposed when the notification
    /// is dismissed or a new different texture is set via another call to this function over overwriting the property.
    /// You do not have to dispose the resulted instance of <see cref="IDalamudTextureWrap"/> yourself.</para>
    /// <para>If the task fails for any reason, the exception will be silently ignored and the icon specified from
    /// <see cref="INotification.Icon"/> will be used instead.</para>
    /// <para>If <see cref="DismissReason"/> is not <c>null</c>, then calling this function will simply dispose the
    /// result of the passed <paramref name="textureWrapTask"/> without actually updating the icon.</para>
    /// </remarks>
    void SetIconTexture(Task<IDalamudTextureWrap?>? textureWrapTask);

    /// <summary>Sets the icon from <see cref="IDalamudTextureWrap"/>, overriding the icon.</summary>
    /// <param name="textureWrap">The new texture wrap to use, or null to clear and revert back to the icon specified
    /// from <see cref="INotification.Icon"/>.</param>
    /// <param name="leaveOpen">Whether to keep the passed <paramref name="textureWrap"/> not disposed.</param>
    /// <remarks>
    /// <para>If <paramref name="leaveOpen"/> is <c>false</c>, the texture passed will be disposed when the
    /// notification is dismissed or a new different texture is set via another call to this function. You do not have
    /// to dispose it yourself.</para>
    /// <para>If <see cref="DismissReason"/> is not <c>null</c> and <paramref name="leaveOpen"/> is <c>false</c>, then
    /// calling this function will simply dispose the passed <paramref name="textureWrap"/> without actually updating
    /// the icon.</para>
    /// </remarks>
    void SetIconTexture(IDalamudTextureWrap? textureWrap, bool leaveOpen);

    /// <summary>Sets the icon from <see cref="IDalamudTextureWrap"/>, overriding the icon, once the given task
    /// completes.</summary>
    /// <param name="textureWrapTask">The task that will result in a new texture wrap to use, or null to clear and
    /// revert back to the icon specified from <see cref="INotification.Icon"/>.</param>
    /// <param name="leaveOpen">Whether to keep the result from the passed <paramref name="textureWrapTask"/> not
    /// disposed.</param>
    /// <remarks>
    /// <para>If <paramref name="leaveOpen"/> is <c>false</c>, the texture resulted from the passed
    /// <see cref="Task{TResult}"/> will be disposed when the notification is dismissed or a new different texture is
    /// set via another call to this function. You do not have to dispose the resulted instance of
    /// <see cref="IDalamudTextureWrap"/> yourself.</para>
    /// <para>If the task fails for any reason, the exception will be silently ignored and the icon specified from
    /// <see cref="INotification.Icon"/> will be used instead.</para>
    /// <para>If <see cref="DismissReason"/> is not <c>null</c>, then calling this function will simply dispose the
    /// result of the passed <paramref name="textureWrapTask"/> without actually updating the icon.</para>
    /// </remarks>
    void SetIconTexture(Task<IDalamudTextureWrap?>? textureWrapTask, bool leaveOpen);

    /// <summary>Generates a new value to use for <see cref="Id"/>.</summary>
    /// <returns>The new value.</returns>
    internal static long CreateNewId() => Interlocked.Increment(ref idCounter);
}

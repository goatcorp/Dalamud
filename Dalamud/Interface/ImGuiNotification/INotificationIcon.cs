using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiNotification.Internal.NotificationIcon;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Icon source for <see cref="INotification"/>.</summary>
/// <remarks>Plugins implementing this interface are left to their own on managing the resources contained by the
/// instance of their implementation of <see cref="INotificationIcon"/>. In other words, they should not expect to have
/// <see cref="IDisposable.Dispose"/> called if their implementation is an <see cref="IDisposable"/>. Dalamud will not
/// call <see cref="IDisposable.Dispose"/> on any instance of <see cref="INotificationIcon"/>. On plugin unloads, the
/// icon may be reverted back to the default, if the instance of <see cref="INotificationIcon"/> is not provided by
/// Dalamud.</remarks>
public interface INotificationIcon
{
    /// <summary>Gets a new instance of <see cref="INotificationIcon"/> that will source the icon from an
    /// <see cref="SeIconChar"/>.</summary>
    /// <param name="iconChar">The icon character.</param>
    /// <returns>A new instance of <see cref="INotificationIcon"/> that should be disposed after use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon From(SeIconChar iconChar) => new SeIconCharNotificationIcon(iconChar);

    /// <summary>Gets a new instance of <see cref="INotificationIcon"/> that will source the icon from an
    /// <see cref="FontAwesomeIcon"/>.</summary>
    /// <param name="iconChar">The icon character.</param>
    /// <returns>A new instance of <see cref="INotificationIcon"/> that should be disposed after use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon From(FontAwesomeIcon iconChar) => new FontAwesomeIconNotificationIcon(iconChar);

    /// <summary>Gets a new instance of <see cref="INotificationIcon"/> that will source the icon from a texture
    /// file shipped as a part of the game resources.</summary>
    /// <param name="gamePath">The path to a texture file in the game virtual file system.</param>
    /// <returns>A new instance of <see cref="INotificationIcon"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown, the default icon will be displayed instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon FromGame(string gamePath) => new GamePathNotificationIcon(gamePath);

    /// <summary>Gets a new instance of <see cref="INotificationIcon"/> that will source the icon from an image
    /// file from the file system.</summary>
    /// <param name="filePath">The path to an image file in the file system.</param>
    /// <returns>A new instance of <see cref="INotificationIcon"/> that should be disposed after use.</returns>
    /// <remarks>If any errors are thrown, the default icon will be displayed instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INotificationIcon FromFile(string filePath) => new FilePathNotificationIcon(filePath);

    /// <summary>Draws the icon.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="color">The foreground color.</param>
    /// <returns><c>true</c> if anything has been drawn.</returns>
    bool DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color);
}

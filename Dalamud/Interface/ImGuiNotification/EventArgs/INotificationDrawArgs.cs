using System.Numerics;

namespace Dalamud.Interface.ImGuiNotification.EventArgs;

/// <summary>Arguments for use with <see cref="IActiveNotification.DrawActions"/>.</summary>
/// <remarks>Not to be implemented by plugins.</remarks>
public interface INotificationDrawArgs
{
    /// <summary>Gets the notification being drawn.</summary>
    IActiveNotification Notification { get; }

    /// <summary>Gets the top left coordinates of the area being drawn.</summary>
    Vector2 MinCoord { get; }

    /// <summary>Gets the bottom right coordinates of the area being drawn.</summary>
    /// <remarks>Note that <see cref="Vector2.Y"/> can be <see cref="float.MaxValue"/>, in which case there is no
    /// vertical limits to the drawing region.</remarks>
    Vector2 MaxCoord { get; }
}

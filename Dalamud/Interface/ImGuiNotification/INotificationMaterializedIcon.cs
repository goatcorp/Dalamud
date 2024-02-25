using System.Numerics;

using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>
/// Represents a materialized icon.
/// </summary>
internal interface INotificationMaterializedIcon : IDisposable
{
    /// <summary>Draws the icon.</summary>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="color">The foreground color.</param>
    /// <param name="initiatorPlugin">The initiator plugin.</param>
    void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin);
}

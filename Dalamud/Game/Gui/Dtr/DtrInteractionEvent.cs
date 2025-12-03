using System.Numerics;

using Dalamud.Game.Addon.Events.EventDataTypes;

namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// Represents an interaction event from the DTR system.
/// </summary>
public class DtrInteractionEvent
{
    /// <summary>
    /// Gets the type of mouse click (left or right).
    /// </summary>
    public MouseClickType ClickType { get; init; }

    /// <summary>
    /// Gets the modifier keys that were held during the click.
    /// </summary>
    public ClickModifierKeys ModifierKeys { get; init; }

    /// <summary>
    /// Gets the scroll direction of the mouse wheel, if applicable.
    /// </summary>
    public MouseScrollDirection ScrollDirection { get; init; }

    /// <summary>
    /// Gets lower-level mouse data, if this event came from native UI.
    ///
    /// Can only be set by Dalamud. If null, this event was manually created.
    /// </summary>
    public AddonMouseEventData? AtkEventSource { get; private init; }

    /// <summary>
    /// Gets the position of the mouse cursor when the event occurred.
    /// </summary>
    public Vector2 Position { get; init; }

    /// <summary>
    /// Helper to create a <see cref="DtrInteractionEvent"/> from an <see cref="AddonMouseEventData"/>.
    /// </summary>
    /// <param name="ev">The event.</param>
    /// <returns>A better event.</returns>
    public static DtrInteractionEvent FromMouseEvent(AddonMouseEventData ev)
    {
        return new DtrInteractionEvent
        {
            AtkEventSource = ev,
            ClickType = ev.IsLeftClick ? MouseClickType.Left : MouseClickType.Right,
            ModifierKeys = (ev.IsAltHeld ? ClickModifierKeys.Alt : 0) |
                           (ev.IsControlHeld ? ClickModifierKeys.Ctrl : 0) |
                           (ev.IsShiftHeld ? ClickModifierKeys.Shift : 0),
            ScrollDirection = ev.IsScrollUp ? MouseScrollDirection.Up :
                              ev.IsScrollDown ? MouseScrollDirection.Down :
                              MouseScrollDirection.None,
            Position = ev.Position,
        };
    }
}

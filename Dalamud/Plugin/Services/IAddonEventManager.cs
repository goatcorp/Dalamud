using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service provider for addon event management.
/// </summary>
public interface IAddonEventManager : IDalamudService
{
    /// <summary>
    /// Delegate to be called when an event is received.
    /// </summary>
    /// <param name="atkEventType">The AtkEventType that triggered this event.</param>
    /// <param name="data">The event data object for use in handling this event.</param>
    public delegate void AddonEventDelegate(AddonEventType atkEventType, AddonEventData data);

    /// <summary>
    /// Registers an event handler for the specified addon, node, and type.
    /// </summary>
    /// <param name="atkUnitBase">The parent addon for this event.</param>
    /// <param name="atkResNode">The node that will trigger this event.</param>
    /// <param name="eventType">The event type for this event.</param>
    /// <param name="eventDelegate">The handler to call when event is triggered.</param>
    /// <returns>IAddonEventHandle used to remove the event. Null if no event was added.</returns>
    IAddonEventHandle? AddEvent(nint atkUnitBase, nint atkResNode, AddonEventType eventType, AddonEventDelegate eventDelegate);

    /// <summary>
    /// Unregisters an event handler with the specified event id and event type.
    /// </summary>
    /// <param name="eventHandle">Unique handle identifying this event.</param>
    void RemoveEvent(IAddonEventHandle eventHandle);

    /// <summary>
    /// Force the game cursor to be the specified cursor.
    /// </summary>
    /// <param name="cursor">Which cursor to use.</param>
    void SetCursor(AddonCursorType cursor);

    /// <summary>
    /// Un-forces the game cursor.
    /// </summary>
    void ResetCursor();
}

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Interface representing the data used for managing AddonEvents.
/// </summary>
public interface IAddonEventHandle
{
    /// <summary>
    /// Gets the param key associated with this event.
    /// </summary>
    uint ParamKey { get; init; }

    /// <summary>
    /// Gets the name of the addon that this event was attached to.
    /// </summary>
    string AddonName { get; init; }

    /// <summary>
    /// Gets the event type associated with this handle.
    /// </summary>
    AddonEventType EventType { get; init; }

    /// <summary>
    /// Gets the unique ID for this handle.
    /// </summary>
    Guid EventGuid { get; init; }
}

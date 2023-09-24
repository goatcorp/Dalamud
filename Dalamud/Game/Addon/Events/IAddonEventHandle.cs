namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Interface representing the data used for managing AddonEvents.
/// </summary>
public interface IAddonEventHandle
{
    /// <summary>
    /// Gets the param key associated with this event.
    /// </summary>
    public uint ParamKey { get; init; }
    
    /// <summary>
    /// Gets the name of the addon that this event was attached to.
    /// </summary>
    public string AddonName { get; init; }
    
    /// <summary>
    /// Gets the event type associated with this handle.
    /// </summary>
    public AddonEventType EventType { get; init; }
    
    /// <summary>
    /// Gets the unique ID for this handle.
    /// </summary>
    public Guid EventGuid { get; init; }
}

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Class that represents a addon event handle.
/// </summary>
public class AddonEventHandle : IAddonEventHandle
{
    /// <inheritdoc/>
    public uint ParamKey { get; init; }

    /// <inheritdoc/>
    public string AddonName { get; init; } = "NullAddon";
    
    /// <inheritdoc/>
    public AddonEventType EventType { get; init; }

    /// <inheritdoc/>
    public Guid EventGuid { get; init; }
}

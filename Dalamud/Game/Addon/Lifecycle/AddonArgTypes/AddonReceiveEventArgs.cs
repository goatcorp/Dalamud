namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for ReceiveEvent events.
/// </summary>
public class AddonReceiveEventArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.ReceiveEvent;
    
    /// <summary>
    /// Gets the AtkEventType for this event message.
    /// </summary>
    public byte AtkEventType { get; init; }
    
    /// <summary>
    /// Gets the event id for this event message.
    /// </summary>
    public int EventParam { get; init; }
    
    /// <summary>
    /// Gets the pointer to an AtkEvent for this event message.
    /// </summary>
    public nint AtkEvent { get; init; }
    
    /// <summary>
    /// Gets the pointer to a block of data for this event message.
    /// </summary>
    public nint Data { get; init; }
}

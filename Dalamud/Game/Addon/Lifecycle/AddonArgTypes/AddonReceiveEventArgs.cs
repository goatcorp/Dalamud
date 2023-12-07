namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for ReceiveEvent events.
/// </summary>
public class AddonReceiveEventArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.ReceiveEvent;
    
    /// <summary>
    /// Gets or sets the AtkEventType for this event message.
    /// </summary>
    public byte AtkEventType { get; set; }
    
    /// <summary>
    /// Gets or sets the event id for this event message.
    /// </summary>
    public int EventParam { get; set; }
    
    /// <summary>
    /// Gets or sets the pointer to an AtkEvent for this event message.
    /// </summary>
    public nint AtkEvent { get; set; }
    
    /// <summary>
    /// Gets or sets the pointer to a block of data for this event message.
    /// </summary>
    public nint Data { get; set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonReceiveEventArgs Clone() => (AddonReceiveEventArgs)this.MemberwiseClone();
    
    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

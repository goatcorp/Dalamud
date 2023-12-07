namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for OnRequestedUpdate events.
/// </summary>
public class AddonRequestedUpdateArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;
    
    /// <summary>
    /// Gets or sets the NumberArrayData** for this event.
    /// </summary>
    public nint NumberArrayData { get; set; }
    
    /// <summary>
    /// Gets or sets the StringArrayData** for this event.
    /// </summary>
    public nint StringArrayData { get; set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonRequestedUpdateArgs Clone() => (AddonRequestedUpdateArgs)this.MemberwiseClone();
    
    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

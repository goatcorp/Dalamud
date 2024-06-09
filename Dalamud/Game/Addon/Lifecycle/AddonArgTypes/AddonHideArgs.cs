namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Hide events.
/// </summary>
public class AddonHideArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Hide;
    
    /// <summary>
    /// Gets or sets a value indicating whether to do something that we don't know what it does yet.
    /// </summary>
    public bool Unknown { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to trigger a hide callback internally.
    /// </summary>
    public bool CallHideCallback { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating which flags to set when the window closes.
    /// </summary>
    public uint SetShowHideFlags { get; set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonHideArgs Clone() => (AddonHideArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

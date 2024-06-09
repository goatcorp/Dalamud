namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Show events.
/// </summary>
public class AddonShowArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Show;
        
    /// <summary>
    /// Gets or sets a value indicating whether to open the window without sound effects.
    /// </summary>
    public bool OpenSilently { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating which flags to clear when the window opens.
    /// </summary>
    public uint UnsetShowHideFlags { get; set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonShowArgs Clone() => (AddonShowArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Update events.
/// </summary>
public class AddonUpdateArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Update;
    
    /// <summary>
    /// Gets the time since the last update.
    /// </summary>
    public float TimeDelta { get; internal set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonUpdateArgs Clone() => (AddonUpdateArgs)this.MemberwiseClone();
    
    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

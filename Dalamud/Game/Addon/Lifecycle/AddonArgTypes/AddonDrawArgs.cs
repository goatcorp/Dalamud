namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Draw events.
/// </summary>
public class AddonDrawArgs : AddonArgs, ICloneable
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Draw;

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonDrawArgs Clone() => (AddonDrawArgs)this.MemberwiseClone();
    
    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

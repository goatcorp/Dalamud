namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Draw events.
/// </summary>
public class AddonDrawArgs : AddonArgs, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonDrawArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonDrawArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Draw;

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonDrawArgs Clone() => (AddonDrawArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

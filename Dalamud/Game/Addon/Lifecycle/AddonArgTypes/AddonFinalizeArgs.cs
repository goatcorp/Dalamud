namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for ReceiveEvent events.
/// </summary>
public class AddonFinalizeArgs : AddonArgs, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonFinalizeArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonFinalizeArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Finalize;

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonFinalizeArgs Clone() => (AddonFinalizeArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

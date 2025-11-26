namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Draw events.
/// </summary>
public class AddonGenericArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonGenericArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonGenericArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Generic;
}

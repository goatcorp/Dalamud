namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Draw events.
/// </summary>
public class AddonDrawArgs : AddonArgs
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
}

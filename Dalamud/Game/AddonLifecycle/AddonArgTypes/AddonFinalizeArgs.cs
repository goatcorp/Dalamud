namespace Dalamud.Game.Addon;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonFinalizeArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Finalize;
}

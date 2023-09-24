namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonDrawArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Draw;
}

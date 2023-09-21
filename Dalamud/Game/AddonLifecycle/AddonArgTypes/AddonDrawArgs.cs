namespace Dalamud.Game.Addon.AddonArgTypes;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonDrawArgs : IAddonArgs
{
    /// <inheritdoc/>
    public nint Addon { get; init; }

    /// <inheritdoc/>
    public AddonArgsType Type => AddonArgsType.Draw;
}

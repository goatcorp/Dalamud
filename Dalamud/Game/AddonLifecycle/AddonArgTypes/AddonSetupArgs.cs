namespace Dalamud.Game.Addon.AddonArgTypes;

/// <summary>
/// Addon argument data for Setup events.
/// </summary>
public class AddonSetupArgs : IAddonArgs
{
    /// <inheritdoc/>
    public nint Addon { get; init; }

    /// <inheritdoc/>
    public AddonArgsType Type => AddonArgsType.Setup;
}

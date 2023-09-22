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
    
    /// <summary>
    /// Gets the number of AtkValues.
    /// </summary>
    public uint AtkValueCount { get; init; }
    
    /// <summary>
    /// Gets the address of the AtkValue array.
    /// </summary>
    public nint AtkValues { get; init; }
}

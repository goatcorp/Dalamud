namespace Dalamud.Game.Addon.AddonArgTypes;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonUpdateArgs : IAddonArgs
{
    /// <inheritdoc/>
    public nint Addon { get; init; }

    /// <inheritdoc/>
    public AddonArgsType Type => AddonArgsType.Update;
    
    /// <summary>
    /// Gets the time since the last update.
    /// </summary>
    public float TimeDelta { get; init; }
}

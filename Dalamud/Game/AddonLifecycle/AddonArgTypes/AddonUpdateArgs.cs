namespace Dalamud.Game.Addon;

/// <summary>
/// Addon argument data for Finalize events.
/// </summary>
public class AddonUpdateArgs : AddonArgs
{
    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Update;
    
    /// <summary>
    /// Gets the time since the last update.
    /// </summary>
    public float TimeDelta { get; init; }
}

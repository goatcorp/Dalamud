namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// Which creature flags are present on the gauge.
/// </summary>
public enum CreatureFlags : byte
{
    /// <summary>
    /// Pom flag present
    /// </summary>
    Pom = 1,
    
    /// <summary>
    /// Wings flag present
    /// </summary>
    Wings = 2,
    
    /// <summary>
    /// Claw flag present
    /// </summary>
    Claw = 4,

    /// <summary>
    /// Moogle portrait present
    /// </summary>
    MooglePortait = 16,
    
    /// <summary>
    /// Madeen portrait present
    /// </summary>
    MadeenPortrait = 32,
}

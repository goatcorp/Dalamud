namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// DRG Blood of the Dragon state types.
/// </summary>
public enum BOTDState : byte
{
    /// <summary>
    /// Inactive type.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// Blood of the Dragon is active.
    /// </summary>
    BOTD = 1,

    /// <summary>
    /// Life of the Dragon is active.
    /// </summary>
    LOTD = 2,
}

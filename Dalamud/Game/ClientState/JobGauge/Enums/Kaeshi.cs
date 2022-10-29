namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// SAM Kaeshi types.
/// </summary>
public enum Kaeshi : byte
{
    /// <summary>
    /// No Kaeshi is active.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// Kaeshi: Higanbana type.
    /// </summary>
    HIGANBANA = 1,

    /// <summary>
    /// Kaeshi: Goken type.
    /// </summary>
    GOKEN = 2,

    /// <summary>
    /// Kaeshi: Setsugekka type.
    /// </summary>
    SETSUGEKKA = 3,

    /// <summary>
    /// Kaeshi: Namikiri type.
    /// </summary>
    NAMIKIRI = 4,
}

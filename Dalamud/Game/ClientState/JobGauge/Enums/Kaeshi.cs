namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// SAM Kaeshi types.
/// </summary>
public enum Kaeshi : byte
{
    /// <summary>
    /// No Kaeshi is active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Kaeshi: Higanbana type.
    /// </summary>
    Higanbana = 1,

    /// <summary>
    /// Kaeshi: Goken type.
    /// </summary>
    Goken = 2,

    /// <summary>
    /// Kaeshi: Setsugekka type.
    /// </summary>
    Setsugekka = 3,

    /// <summary>
    /// Kaeshi: Namikiri type.
    /// </summary>
    Namikiri = 4,
}

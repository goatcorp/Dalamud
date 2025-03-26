namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// Enum representing the SerpentCombo actions for the VPR job gauge.
/// </summary>
public enum SerpentCombo : byte
{
    /// <summary>
    /// No Serpent combo is active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Death Rattle action.
    /// </summary>
    DeathRattle = 1,

    /// <summary>
    /// Last Lash action.
    /// </summary>
    LastLash = 2,

    /// <summary>
    /// First Legacy action.
    /// </summary>
    FirstLegacy = 3,

    /// <summary>
    /// Second Legacy action.
    /// </summary>
    SecondLegacy = 4,

    /// <summary>
    /// Third Legacy action.
    /// </summary>
    ThirdLegacy = 5,

    /// <summary>
    /// Fourth Legacy action.
    /// </summary>
    FourthLegacy = 6,
}

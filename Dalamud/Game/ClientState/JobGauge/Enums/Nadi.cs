namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// MNK Nadi types.
/// </summary>
[Flags]
public enum Nadi : byte
{
    /// <summary>
    /// No card.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// The Lunar nadi.
    /// </summary>
    LUNAR = 2,

    /// <summary>
    /// The Solar nadi.
    /// </summary>
    SOLAR = 4,
}

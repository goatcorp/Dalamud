namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// MNK Nadi types.
/// </summary>
[Flags]
public enum Nadi : byte
{
    /// <summary>
    /// No nadi.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// The Lunar nadi.
    /// </summary>
    LUNAR = 1,

    /// <summary>
    /// The Solar nadi.
    /// </summary>
    SOLAR = 2,
}

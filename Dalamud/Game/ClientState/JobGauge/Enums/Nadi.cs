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
    None = 0,

    /// <summary>
    /// The Lunar nadi.
    /// </summary>
    Lunar = 1,

    /// <summary>
    /// The Solar nadi.
    /// </summary>
    Solar = 2,
}

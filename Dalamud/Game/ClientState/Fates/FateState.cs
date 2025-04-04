namespace Dalamud.Game.ClientState.Fates;

/// <summary>
/// This represents the state of a single Fate.
/// </summary>
public enum FateState : byte
{
    /// <summary>
    /// The Fate is active.
    /// </summary>
    Running = 0x04,

    /// <summary>
    /// The Fate has ended.
    /// </summary>
    Ended = 0x07,

    /// <summary>
    /// The player failed the Fate.
    /// </summary>
    Failed = 0x08,

    /// <summary>
    /// The Fate is preparing to run.
    /// </summary>
    Preparation = 0x03,

    /// <summary>
    /// The Fate is preparing to end.
    /// </summary>
    WaitingForEnd = 0x05,
}

namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// Samurai Sen types.
/// </summary>
[Flags]
public enum Sen : byte
{
    /// <summary>
    /// No Sen.
    /// </summary>
    None = 0,

    /// <summary>
    /// Setsu Sen type.
    /// </summary>
    Setsu = 1 << 0,

    /// <summary>
    /// Getsu Sen type.
    /// </summary>
    Getsu = 1 << 1,

    /// <summary>
    /// Ka Sen type.
    /// </summary>
    Ka = 1 << 2,
}

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
    NONE = 0,

    /// <summary>
    /// Setsu Sen type.
    /// </summary>
    SETSU = 1 << 0,

    /// <summary>
    /// Getsu Sen type.
    /// </summary>
    GETSU = 1 << 1,

    /// <summary>
    /// Ka Sen type.
    /// </summary>
    KA = 1 << 2,
}

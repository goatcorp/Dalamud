namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// Represents the flags for the canvas in the job gauge.
/// </summary>
public enum CanvasFlags : byte
{
    /// <summary>
    /// Represents the Pom flag for the canvas in the job gauge.
    /// </summary>
    Pom = 1,

    /// <summary>
    /// Represents the Wing canvas flag in the job gauge.
    /// </summary>
    Wing = 2,

    /// <summary>
    /// Represents the flag for the claw in the canvas of the job gauge.
    /// </summary>
    Claw = 4,

    /// <summary>
    /// Represents the 'Maw' flag for the canvas in the job gauge.
    /// </summary>
    Maw = 8,

    /// <summary>
    /// Represents the weapon flag for the canvas in the job gauge.
    /// </summary>
    Weapon = 16,

    /// <summary>
    /// Represents the Landscape flag for the canvas in the job gauge.
    /// </summary>
    Landscape = 32,
}

namespace Dalamud.Game.Network;

/// <summary>
/// This represents the direction of a network message.
/// </summary>
[Obsolete("No longer part of public API", true)]
public enum NetworkMessageDirection
{
    /// <summary>
    /// A zone down message.
    /// </summary>
    ZoneDown,

    /// <summary>
    /// A zone up message.
    /// </summary>
    ZoneUp,
}

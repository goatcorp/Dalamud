namespace Dalamud.Game.Player;

/// <summary>
/// Specifies the mentor certification version for a player.
/// </summary>
public enum MentorVersion : byte
{
    /// <summary>
    /// Indicates that the player has never held mentor status in any expansion.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the player was last a mentor during the <c>Shadowbringers</c> expansion.
    /// </summary>
    Shadowbringers = 1,

    /// <summary>
    /// Indicates that the player was last a mentor during the <c>Endwalker</c> expansion.
    /// </summary>
    Endwalker = 2,

    /// <summary>
    /// Indicates that the player was last a mentor during the <c>Dawntrail</c> expansion.
    /// </summary>
    Dawntrail = 3,
}

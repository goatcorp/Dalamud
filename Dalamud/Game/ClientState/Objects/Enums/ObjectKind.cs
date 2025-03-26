namespace Dalamud.Game.ClientState.Objects.Enums;

/// <summary>
/// Enum describing possible entity kinds.
/// </summary>
public enum ObjectKind : byte
{
    /// <summary>
    /// Invalid character.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Objects representing player characters.
    /// </summary>
    Player = 0x01,

    /// <summary>
    /// Objects representing battle NPCs.
    /// </summary>
    BattleNpc = 0x02,

    /// <summary>
    /// Objects representing event NPCs.
    /// </summary>
    EventNpc = 0x03,

    /// <summary>
    /// Objects representing treasures.
    /// </summary>
    Treasure = 0x04,

    /// <summary>
    /// Objects representing aetherytes.
    /// </summary>
    Aetheryte = 0x05,

    /// <summary>
    /// Objects representing gathering points.
    /// </summary>
    GatheringPoint = 0x06,

    /// <summary>
    /// Objects representing event objects.
    /// </summary>
    EventObj = 0x07,

    /// <summary>
    /// Objects representing mounts.
    /// </summary>
    MountType = 0x08,

    /// <summary>
    /// Objects representing minions.
    /// </summary>
    Companion = 0x09, // Minion

    /// <summary>
    /// Objects representing retainers.
    /// </summary>
    Retainer = 0x0A,

    /// <summary>
    /// Objects representing area objects.
    /// </summary>
    Area = 0x0B,

    /// <summary>
    /// Objects representing housing objects.
    /// </summary>
    Housing = 0x0C,

    /// <summary>
    /// Objects representing cutscene objects.
    /// </summary>
    Cutscene = 0x0D,

    /// <summary>
    /// Objects representing card stand objects.
    /// </summary>
    CardStand = 0x0E,

    /// <summary>
    /// Objects representing ornament (Fashion Accessories) objects.
    /// </summary>
    Ornament = 0x0F,
}

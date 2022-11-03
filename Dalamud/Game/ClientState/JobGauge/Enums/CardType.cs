namespace Dalamud.Game.ClientState.JobGauge.Enums;

/// <summary>
/// AST Arcanum (card) types.
/// </summary>
public enum CardType : byte
{
    /// <summary>
    /// No card.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// The Balance card.
    /// </summary>
    BALANCE = 1,

    /// <summary>
    /// The Bole card.
    /// </summary>
    BOLE = 2,

    /// <summary>
    /// The Arrow card.
    /// </summary>
    ARROW = 3,

    /// <summary>
    /// The Spear card.
    /// </summary>
    SPEAR = 4,

    /// <summary>
    /// The Ewer card.
    /// </summary>
    EWER = 5,

    /// <summary>
    /// The Spire card.
    /// </summary>
    SPIRE = 6,

    /// <summary>
    /// The Lord of Crowns card.
    /// </summary>
    LORD = 0x70,

    /// <summary>
    /// The Lady of Crowns card.
    /// </summary>
    LADY = 0x80,
}

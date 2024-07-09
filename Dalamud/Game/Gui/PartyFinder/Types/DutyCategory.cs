namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Category flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
public enum DutyCategory : ushort
{
    /// <summary>
    /// The none category.
    /// </summary>
    None = 0,

    /// <summary>
    /// The duty roulette category.
    /// </summary>
    DutyRoulette = 1 << 1,

    /// <summary>
    /// The dungeons category.
    /// </summary>
    Dungeon = 1 << 2,

    /// <summary>
    /// The guildhests category.
    /// </summary>
    Guildhest = 1 << 3,

    /// <summary>
    /// The trials category.
    /// </summary>
    Trial = 1 << 4,

    /// <summary>
    /// The raids category.
    /// </summary>
    Raid = 1 << 5,

    /// <summary>
    /// The high-end duty category.
    /// </summary>
    HighEndDuty = 1 << 6,

    /// <summary>
    /// The pvp category.
    /// </summary>
    PvP = 1 << 7,

    /// <summary>
    /// The gold saucer category.
    /// </summary>
    GoldSaucer = 1 << 8,

    /// <summary>
    /// The FATEs category.
    /// </summary>
    Fate = 1 << 9,

    /// <summary>
    /// The treasure hunts category.
    /// </summary>
    TreasureHunt = 1 << 10,

    /// <summary>
    /// The hunts category.
    /// </summary>
    TheHunt = 1 << 11,

    /// <summary>
    /// The gathering forays category.
    /// </summary>
    GatheringForay = 1 << 12,

    /// <summary>
    /// The deep dungeons category.
    /// </summary>
    DeepDungeon = 1 << 13,

    /// <summary>
    /// The field operations category.
    /// </summary>
    FieldOperation = 1 << 14,

    /// <summary>
    /// The variant and criterion dungeons category.
    /// </summary>
    VariantAndCriterionDungeon = 1 << 15,
}

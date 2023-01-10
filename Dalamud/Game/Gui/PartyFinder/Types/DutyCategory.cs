namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Category flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
public enum DutyCategory
{
    /// <summary>
    /// The duty category.
    /// </summary>
    Duty = 0,

    /// <summary>
    /// The quest battle category.
    /// </summary>
    QuestBattles = 1 << 0,

    /// <summary>
    /// The fate category.
    /// </summary>
    Fates = 1 << 1,

    /// <summary>
    /// The treasure hunt category.
    /// </summary>
    TreasureHunt = 1 << 2,

    /// <summary>
    /// The hunt category.
    /// </summary>
    TheHunt = 1 << 3,

    /// <summary>
    /// The gathering forays category.
    /// </summary>
    GatheringForays = 1 << 4,

    /// <summary>
    /// The deep dungeons category.
    /// </summary>
    DeepDungeons = 1 << 5,

    /// <summary>
    /// The adventuring forays category.
    /// </summary>
    AdventuringForays = 1 << 6,
}

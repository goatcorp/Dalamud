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
    /// The gold saucer category.
    /// </summary>
    GoldSaucer = 1 << 0,

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
    [Obsolete("Adventuring Forays have been renamed to Field Operations")]
    AdventuringForays = 1 << 6,

    /// <summary>
    /// The field operations category.
    /// </summary>
    FieldOperations = 1 << 6,

    /// <summary>
    /// The variant and criterion dungeons category.
    /// </summary>
    VariantAndCriterionDungeons = 1 << 7,
}

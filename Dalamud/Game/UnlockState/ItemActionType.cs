using Lumina.Excel.Sheets;

namespace Dalamud.Game.UnlockState;

// TODO: Switch to FFXIVClientStructs.FFXIV.Client.Enums.ItemActionType.

/// <summary>
/// Enum for <see cref="ItemAction.Type"/>.
/// </summary>
internal enum ItemActionType : ushort
{
    /// <summary>
    /// No item action.
    /// </summary>
    None = 0,

    /// <summary>
    /// Unlocks a companion (minion).
    /// </summary>
    Companion = 853,

    /// <summary>
    /// Unlocks a chocobo companion barding.
    /// </summary>
    BuddyEquip = 1013,

    /// <summary>
    /// Unlocks a mount.
    /// </summary>
    Mount = 1322,

    /// <summary>
    /// Unlocks recipes from a crafting recipe book.
    /// </summary>
    SecretRecipeBook = 2136,

    /// <summary>
    /// Unlocks various types of content (e.g. Riding Maps, Blue Mage Totems, Emotes, Hairstyles).
    /// </summary>
    UnlockLink = 2633,

    /// <summary>
    /// Unlocks a Triple Triad Card.
    /// </summary>
    TripleTriadCard = 3357,

    /// <summary>
    /// Unlocks gathering nodes of a Folklore Tome.
    /// </summary>
    FolkloreTome = 4107,

    /// <summary>
    /// Unlocks an Orchestrion Roll.
    /// </summary>
    OrchestrionRoll = 25183,

    /// <summary>
    /// Unlocks portrait designs.
    /// </summary>
    FramersKit = 29459,

    /// <summary>
    /// Unlocks Bozjan Field Notes.
    /// </summary>
    /// <remarks> These are server-side but are cached client-side. </remarks>
    FieldNotes = 19743,

    /// <summary>
    /// Unlocks an Ornament (fashion accessory).
    /// </summary>
    Ornament = 20086,

    /// <summary>
    /// Unlocks Glasses.
    /// </summary>
    Glasses = 37312,

    /// <summary>
    /// Company Seal Vouchers, which convert the item into Company Seals when used.
    /// </summary>
    CompanySealVouchers = 41120,

    /// <summary>
    /// Unlocks Occult Records in Occult Crescent.
    /// </summary>
    OccultRecords = 43141,

    /// <summary>
    /// Unlocks Phantom Jobs in Occult Crescent.
    /// </summary>
    SoulShards = 43142,

    /// <summary>
    /// Star Contributor Certificate, which grants the Star Contributor status in Cosmic Exploration.
    /// </summary>
    StarContributorCertificate = 45189,
}

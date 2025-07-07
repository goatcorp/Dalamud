using Lumina.Excel.Sheets;

namespace Dalamud.Game;

/// <summary>
/// Enum for <see cref="ItemAction.Type"/>.
/// </summary>
public enum ItemActionType : ushort
{
    /// <summary>
    /// Used to unlock a companion (minion).
    /// </summary>
    Companion = 853,

    /// <summary>
    /// Used to unlock a chocobo companion barding.
    /// </summary>
    BuddyEquip = 1013,

    /// <summary>
    /// Used to unlock a mount.
    /// </summary>
    Mount = 1322,

    /// <summary>
    /// Used to unlock recipes from a crafting recipe book.
    /// </summary>
    SecretRecipeBook = 2136,

    /// <summary>
    /// Used to unlock various types of content (e.g. Riding Maps, Blue Mage Totems, Emotes, Hairstyles).
    /// </summary>
    UnlockLink = 2633,

    /// <summary>
    /// Used to unlock a Triple Triad Card.
    /// </summary>
    TripleTriadCard = 3357,

    /// <summary>
    /// Used to unlock gathering nodes of a Folklore Tome.
    /// </summary>
    FolkloreTome = 4107,

    /// <summary>
    /// Used to unlock an Orchestrion Roll.
    /// </summary>
    OrchestrionRoll = 25183,

    /// <summary>
    /// Used to unlock portrait designs.
    /// </summary>
    FramersKit = 29459,

    /// <summary>
    /// Used to unlock Bozjan Field Notes. These are server-side but are cached client-side.
    /// </summary>
    FieldNotes = 19743,

    /// <summary>
    /// Used to unlock an Ornament (fashion accessory).
    /// </summary>
    Ornament = 20086,

    /// <summary>
    /// Used to unlock glasses.
    /// </summary>
    Glasses = 37312,

    /// <summary>
    /// Used for Company Seal Vouchers, which convert the item into Company Seals when used.<br/>
    /// Can be used only if in a Grand Company.<br/>
    /// IsUnlocked always returns false.
    /// </summary>
    CompanySealVouchers = 41120,
}

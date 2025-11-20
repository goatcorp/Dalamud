using System.Diagnostics.CodeAnalysis;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Plugin.Services;

#pragma warning disable SA1400 // Access modifier should be declared: Interface members are public by default

/// <summary>
/// Interface for determining unlock state of various content in the game.
/// </summary>
[Experimental("Dalamud001")]
public interface IUnlockState : IDalamudService
{
    /// <summary>
    /// A delegate type used for the <see cref="Unlock"/> event.
    /// </summary>
    /// <param name="rowRef">A RowRef of the unlocked thing.</param>
    delegate void UnlockDelegate(RowRef rowRef);

    /// <summary>
    /// Event triggered when something was unlocked.
    /// </summary>
    event UnlockDelegate? Unlock;

    /// <summary>
    /// Determines whether the specified Action is unlocked.
    /// </summary>
    /// <param name="row">The Action row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsActionUnlocked(Lumina.Excel.Sheets.Action row);

    /// <summary>
    /// Determines whether the specified AetherCurrentCompFlgSet is unlocked.
    /// </summary>
    /// <param name="row">The AetherCurrentCompFlgSet row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsAetherCurrentCompFlgSetUnlocked(AetherCurrentCompFlgSet row);

    /// <summary>
    /// Determines whether the specified AetherCurrent is unlocked.
    /// </summary>
    /// <param name="row">The AetherCurrent row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsAetherCurrentUnlocked(AetherCurrent row);

    /// <summary>
    /// Determines whether the specified AozAction (Blue Mage Action) is unlocked.
    /// </summary>
    /// <param name="row">The AozAction row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsAozActionUnlocked(AozAction row);

    /// <summary>
    /// Determines whether the specified BannerBg (Portrait Backgrounds) is unlocked.
    /// </summary>
    /// <param name="row">The BannerBg row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerBgUnlocked(BannerBg row);

    /// <summary>
    /// Determines whether the specified BannerCondition is unlocked.
    /// </summary>
    /// <param name="row">The BannerCondition row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerConditionUnlocked(BannerCondition row);

    /// <summary>
    /// Determines whether the specified BannerDecoration (Portrait Accents) is unlocked.
    /// </summary>
    /// <param name="row">The BannerDecoration row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerDecorationUnlocked(BannerDecoration row);

    /// <summary>
    /// Determines whether the specified BannerFacial (Portrait Expressions) is unlocked.
    /// </summary>
    /// <param name="row">The BannerFacial row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerFacialUnlocked(BannerFacial row);

    /// <summary>
    /// Determines whether the specified BannerFrame (Portrait Frames) is unlocked.
    /// </summary>
    /// <param name="row">The BannerFrame row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerFrameUnlocked(BannerFrame row);

    /// <summary>
    /// Determines whether the specified BannerTimeline (Portrait Poses) is unlocked.
    /// </summary>
    /// <param name="row">The BannerTimeline row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBannerTimelineUnlocked(BannerTimeline row);

    /// <summary>
    /// Determines whether the specified BuddyAction (Action of the players Chocobo Companion) is unlocked.
    /// </summary>
    /// <param name="row">The BuddyAction row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBuddyActionUnlocked(BuddyAction row);

    /// <summary>
    /// Determines whether the specified BuddyEquip (Equipment of the players Chocobo Companion) is unlocked.
    /// </summary>
    /// <param name="row">The BuddyEquip row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsBuddyEquipUnlocked(BuddyEquip row);

    /// <summary>
    /// Determines whether the specified CharaMakeCustomize (Hairstyles and Face Paint patterns) is unlocked.
    /// </summary>
    /// <param name="row">The CharaMakeCustomize row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsCharaMakeCustomizeUnlocked(CharaMakeCustomize row);

    /// <summary>
    /// Determines whether the specified ChocoboTaxiStand (Chocobokeeps of the Chocobo Porter service) is unlocked.
    /// </summary>
    /// <param name="row">The ChocoboTaxiStand row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsChocoboTaxiStandUnlocked(ChocoboTaxiStand row);

    /// <summary>
    /// Determines whether the specified Companion (Minions) is unlocked.
    /// </summary>
    /// <param name="row">The Companion row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsCompanionUnlocked(Companion row);

    /// <summary>
    /// Determines whether the specified CraftAction is unlocked.
    /// </summary>
    /// <param name="row">The CraftAction row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsCraftActionUnlocked(CraftAction row);

    /// <summary>
    /// Determines whether the specified CSBonusContentType is unlocked.
    /// </summary>
    /// <param name="row">The CSBonusContentType row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsCSBonusContentTypeUnlocked(CSBonusContentType row);

    /// <summary>
    /// Determines whether the specified Emote is unlocked.
    /// </summary>
    /// <param name="row">The Emote row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsEmoteUnlocked(Emote row);

    /// <summary>
    /// Determines whether the specified EmjVoiceNpc (Doman Mahjong Characters) is unlocked.
    /// </summary>
    /// <param name="row">The EmjVoiceNpc row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsEmjVoiceNpcUnlocked(EmjVoiceNpc row);

    /// <summary>
    /// Determines whether the specified EmjCostume (Doman Mahjong Character Costume) is unlocked.
    /// </summary>
    /// <param name="row">The EmjCostume row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsEmjCostumeUnlocked(EmjCostume row);

    /// <summary>
    /// Determines whether the specified GeneralAction is unlocked.
    /// </summary>
    /// <param name="row">The GeneralAction row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsGeneralActionUnlocked(GeneralAction row);

    /// <summary>
    /// Determines whether the specified Glasses is unlocked.
    /// </summary>
    /// <param name="row">The Glasses row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsGlassesUnlocked(Glasses row);

    /// <summary>
    /// Determines whether the specified HowTo is unlocked.
    /// </summary>
    /// <param name="row">The HowTo row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsHowToUnlocked(HowTo row);

    /// <summary>
    /// Determines whether the specified InstanceContent is unlocked.
    /// </summary>
    /// <param name="row">The InstanceContent row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsInstanceContentUnlocked(InstanceContent row);

    /// <summary>
    /// Determines whether the specified Item is considered unlockable.
    /// </summary>
    /// <param name="row">The Item row to check.</param>
    /// <returns><see langword="true"/> if unlockable; otherwise, <see langword="false"/>.</returns>
    bool IsItemUnlockable(Item row);

    /// <summary>
    /// Determines whether the specified Item is unlocked.
    /// </summary>
    /// <param name="row">The Item row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsItemUnlocked(Item row);

    /// <summary>
    /// Determines whether the specified McGuffin is unlocked.
    /// </summary>
    /// <param name="row">The McGuffin row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsMcGuffinUnlocked(McGuffin row);

    /// <summary>
    /// Determines whether the specified MJILandmark (Island Sanctuary landmark) is unlocked.
    /// </summary>
    /// <param name="row">The MJILandmark row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsMJILandmarkUnlocked(MJILandmark row);

    /// <summary>
    /// Determines whether the specified MKDLore (Occult Record) is unlocked.
    /// </summary>
    /// <param name="row">The MKDLore row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsMKDLoreUnlocked(MKDLore row);

    /// <summary>
    /// Determines whether the specified Mount is unlocked.
    /// </summary>
    /// <param name="row">The Mount row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsMountUnlocked(Mount row);

    /// <summary>
    /// Determines whether the specified NotebookDivision (Categories in Crafting/Gathering Log) is unlocked.
    /// </summary>
    /// <param name="row">The NotebookDivision row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsNotebookDivisionUnlocked(NotebookDivision row);

    /// <summary>
    /// Determines whether the specified Orchestrion roll is unlocked.
    /// </summary>
    /// <param name="row">The Orchestrion row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsOrchestrionUnlocked(Orchestrion row);

    /// <summary>
    /// Determines whether the specified Ornament (Fashion Accessories) is unlocked.
    /// </summary>
    /// <param name="row">The Ornament row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsOrnamentUnlocked(Ornament row);

    /// <summary>
    /// Determines whether the specified Perform (Performance Instruments) is unlocked.
    /// </summary>
    /// <param name="row">The Perform row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsPerformUnlocked(Perform row);

    /// <summary>
    /// Determines whether the specified PublicContent is unlocked.
    /// </summary>
    /// <param name="row">The PublicContent row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsPublicContentUnlocked(PublicContent row);

    /// <summary>
    /// Determines whether the specified Recipe is unlocked.
    /// </summary>
    /// <param name="row">The Recipe row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsRecipeUnlocked(Recipe row);

    /// <summary>
    /// Determines whether the underlying RowRef type is unlocked.
    /// </summary>
    /// <param name="rowRef">The RowRef to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsRowRefUnlocked(RowRef rowRef);

    /// <summary>
    /// Determines whether the underlying RowRef type is unlocked.
    /// </summary>
    /// <typeparam name="T">The type of the Excel row.</typeparam>
    /// <param name="rowRef">The RowRef to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsRowRefUnlocked<T>(RowRef<T> rowRef) where T : struct, IExcelRow<T>;

    /// <summary>
    /// Determines whether the specified SecretRecipeBook (Master Recipe Books) is unlocked.
    /// </summary>
    /// <param name="row">The SecretRecipeBook row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsSecretRecipeBookUnlocked(SecretRecipeBook row);

    /// <summary>
    /// Determines whether the specified Trait is unlocked.
    /// </summary>
    /// <param name="row">The Trait row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsTraitUnlocked(Trait row);

    /// <summary>
    /// Determines whether the specified TripleTriadCard is unlocked.
    /// </summary>
    /// <param name="row">The TripleTriadCard row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsTripleTriadCardUnlocked(TripleTriadCard row);

    /// <summary>
    /// Determines whether the specified unlock link is unlocked or quest is completed.
    /// </summary>
    /// <param name="unlockLink">The unlock link id or quest id (quest ids in this case are over 65536).</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsUnlockLinkUnlocked(uint unlockLink);

    /// <summary>
    /// Determines whether the specified unlock link is unlocked.
    /// </summary>
    /// <param name="unlockLink">The unlock link id.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsUnlockLinkUnlocked(ushort unlockLink);
}

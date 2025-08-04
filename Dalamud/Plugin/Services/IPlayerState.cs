using Dalamud.Game.PlayerState;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Plugin.Services;

#pragma warning disable SA1400 // Access modifier should be declared: Interface members are public by default

/// <summary>
/// Interface for determining unlock state of various content in the game.
/// </summary>
public interface IPlayerState
{
    /// <summary>
    /// A delegate type used for the <see cref="ClassJobChange"/> event.
    /// </summary>
    /// <param name="classJobId">The new ClassJob id.</param>
    delegate void ClassJobChangeDelegate(uint classJobId);

    /// <summary>
    /// A delegate type used for the <see cref="LevelChange"/> event.
    /// </summary>
    /// <param name="classJobId">The ClassJob id.</param>
    /// <param name="level">The level of the corresponding ClassJob.</param>
    delegate void LevelChangeDelegate(uint classJobId, uint level);

    /// <summary>
    /// A delegate type used for the <see cref="Unlock"/> event.
    /// </summary>
    /// <param name="rowRef">A RowRef of the unlocked thing.</param>
    delegate void UnlockDelegate(RowRef rowRef);

    /// <summary>
    /// Event that fires when a characters ClassJob changed.
    /// </summary>
    event ClassJobChangeDelegate? ClassJobChange;

    /// <summary>
    /// Event that fires when <em>any</em> character level changes, including levels
    /// for a not-currently-active ClassJob (e.g. PvP matches, DoH/DoL).
    /// </summary>
    event LevelChangeDelegate? LevelChange;

    /// <summary>
    /// Event triggered when something was unlocked.
    /// </summary>
    event UnlockDelegate? Unlock;

    /// <summary>
    /// Gets a value indicating whether the local character is loaded.
    /// </summary>
    /// <remarks>
    /// The actual GameObject will not immediately exist when this changes to true.
    /// </remarks>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the name of the local character.
    /// </summary>
    string CharacterName { get; }

    /// <summary>
    /// Gets the entity ID of the local character.
    /// </summary>
    uint EntityId { get; }

    /// <summary>
    /// Gets the content ID of the local character.
    /// </summary>
    ulong ContentId { get; }

    /// <summary>
    /// Gets the World row for the local character's current world.
    /// </summary>
    RowRef<World> CurrentWorld { get; }

    /// <summary>
    /// Gets the World row for the local character's home world.
    /// </summary>
    RowRef<World> HomeWorld { get; }

    /// <summary>
    /// Gets the sex of the local character.
    /// </summary>
    Sex Sex { get; }

    /// <summary>
    /// Gets the Race row for the local character.
    /// </summary>
    RowRef<Race> Race { get; }

    /// <summary>
    /// Gets the Tribe row for the local character.
    /// </summary>
    RowRef<Tribe> Tribe { get; }

    /// <summary>
    /// Gets the ClassJob row for the local character's current class/job.
    /// </summary>
    RowRef<ClassJob> ClassJob { get; }

    /// <summary>
    /// Gets the current class/job's level of the local character.
    /// </summary>
    short Level { get; }

    /// <summary>
    /// Gets a value indicating whether the local character's level is synced.
    /// </summary>
    bool IsLevelSynced { get; }

    /// <summary>
    /// Gets the effective level of the local character.
    /// </summary>
    short EffectiveLevel { get; }

    /// <summary>
    /// Gets the GuardianDeity row for the local character.
    /// </summary>
    RowRef<GuardianDeity> GuardianDeity { get; }

    /// <summary>
    /// Gets the birth month of the local character.
    /// </summary>
    byte BirthMonth { get; }

    /// <summary>
    /// Gets the birth day of the local character.
    /// </summary>
    byte BirthDay { get; }

    /// <summary>
    /// Gets the ClassJob row for the local character's starting class.
    /// </summary>
    RowRef<ClassJob> FirstClass { get; }

    /// <summary>
    /// Gets the Town row for the local character's starting town.
    /// </summary>
    RowRef<Town> StartTown { get; }

    /// <summary>
    /// Gets the base strength of the local character.
    /// </summary>
    int BaseStrength { get; }

    /// <summary>
    /// Gets the base dexterity of the local character.
    /// </summary>
    int BaseDexterity { get; }

    /// <summary>
    /// Gets the base vitality of the local character.
    /// </summary>
    int BaseVitality { get; }

    /// <summary>
    /// Gets the base intelligence of the local character.
    /// </summary>
    int BaseIntelligence { get; }

    /// <summary>
    /// Gets the base mind of the local character.
    /// </summary>
    int BaseMind { get; }

    /// <summary>
    /// Gets the piety mind of the local character.
    /// </summary>
    int BasePiety { get; }

    /// <summary>
    /// Gets the GrandCompany row for the local character's current Grand Company affiliation.
    /// </summary>
    RowRef<GrandCompany> GrandCompany { get; }

    /// <summary>
    /// Gets the Aetheryte row for the local character's home aetheryte.
    /// </summary>
    RowRef<Aetheryte> HomeAetheryte { get; }

    /// <summary>
    /// Gets a span of Aetheryte rows for the local character's favourite aetherytes.
    /// </summary>
    ReadOnlySpan<RowRef<Aetheryte>> FavouriteAetherytes { get; }

    /// <summary>
    /// Gets the Aetheryte row for the local character's free aetheryte.
    /// </summary>
    RowRef<Aetheryte> FreeAetheryte { get; }

    /// <summary>
    /// Gets the amount of received player commendations of the local character.
    /// </summary>
    uint BaseRestedExperience { get; }

    /// <summary>
    /// Gets the amount of received player commendations of the local character.
    /// </summary>
    short PlayerCommendations { get; }

    /// <summary>
    /// Gets the Carrier Level of Delivery Moogle Quests of the local character.
    /// </summary>
    byte DeliveryLevel { get; }

    /// <summary>
    /// Gets the mentor version of the local character.
    /// </summary>
    MentorVersion MentorVersion { get; }

    /// <summary>
    /// Gets the value of an attribute of the local character.
    /// </summary>
    /// <param name="attribute">The attribute to check.</param>
    /// <returns>The value of the specific attribute.</returns>
    int GetAttribute(PlayerAttribute attribute);

    /// <summary>
    /// Gets the Grand Company rank of the local character.
    /// </summary>
    /// <param name="grandCompany">The Grand Company to check.</param>
    /// <returns>The Grand Company rank of the local character.</returns>
    byte GetGrandCompanyRank(GrandCompany grandCompany);

    /// <summary>
    /// Gets the level of the local character's class/job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The level of the requested class/job.</returns>
    short GetClassJobLevel(ClassJob classJob);

    /// <summary>
    /// Gets the experience of the local character's class/job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The experience of the requested class/job.</returns>
    int GetClassJobExperience(ClassJob classJob);

    /// <summary>
    /// Gets the desynthesis level of the local character's crafter job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The desynthesis level of the requested crafter job.</returns>
    float GetDesynthesisLevel(ClassJob classJob);

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
    /// Determines whether the specified ChocoboTaxi (Chocobokeeps of the Chocobo Porter service) is unlocked.
    /// </summary>
    /// <param name="row">The ChocoboTaxi row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsChocoboTaxiUnlocked(ChocoboTaxi row);

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
    /// <param name="item">The Item row to check.</param>
    /// <returns><see langword="true"/> if unlockable; otherwise, <see langword="false"/>.</returns>
    bool IsItemUnlockable(Item item);

    /// <summary>
    /// Determines whether the specified Item is unlocked.
    /// </summary>
    /// <param name="item">The Item row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    bool IsItemUnlocked(Item item);

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

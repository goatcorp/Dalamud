using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.PlayerState;

/// <summary>
/// This class represents the state of the local player.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IPlayerState>]
#pragma warning restore SA1015
internal unsafe class PlayerStatePluginScoped : IInternalDisposableService, IPlayerState
{
    [ServiceManager.ServiceDependency]
    private readonly PlayerState playerStateService = Service<PlayerState>.Get();

    private readonly LocalPlugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerStatePluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    internal PlayerStatePluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
        this.playerStateService.ClassJobChange += this.OnClassJobChange;
        this.playerStateService.LevelChange += this.OnLevelChange;
        this.playerStateService.Unlock += this.OnUnlock;
    }

    /// <inheritdoc />
    public event IPlayerState.ClassJobChangeDelegate? ClassJobChange;

    /// <inheritdoc />
    public event IPlayerState.LevelChangeDelegate? LevelChange;

    /// <inheritdoc />
    public event IPlayerState.UnlockDelegate? Unlock;

    /// <inheritdoc />
    public bool IsLoaded => this.playerStateService.IsLoaded;

    /// <inheritdoc />
    public string CharacterName => this.playerStateService.CharacterName;

    /// <inheritdoc />
    public uint EntityId => this.playerStateService.EntityId;

    /// <inheritdoc />
    public ulong ContentId => this.playerStateService.ContentId;

    /// <inheritdoc />
    public RowRef<World> CurrentWorld => this.playerStateService.CurrentWorld;

    /// <inheritdoc />
    public RowRef<World> HomeWorld => this.playerStateService.HomeWorld;

    /// <inheritdoc />
    public Sex Sex => this.playerStateService.Sex;

    /// <inheritdoc />
    public RowRef<Race> Race => this.playerStateService.Race;

    /// <inheritdoc />
    public RowRef<Tribe> Tribe => this.playerStateService.Tribe;

    /// <inheritdoc />
    public RowRef<ClassJob> ClassJob => this.playerStateService.ClassJob;

    /// <inheritdoc />
    public short Level => this.playerStateService.Level;

    /// <inheritdoc />
    public bool IsLevelSynced => this.playerStateService.IsLevelSynced;

    /// <inheritdoc />
    public short EffectiveLevel => this.playerStateService.EffectiveLevel;

    /// <inheritdoc />
    public RowRef<GuardianDeity> GuardianDeity => this.playerStateService.GuardianDeity;

    /// <inheritdoc />
    public byte BirthMonth => this.playerStateService.BirthMonth;

    /// <inheritdoc />
    public byte BirthDay => this.playerStateService.BirthDay;

    /// <inheritdoc />
    public RowRef<ClassJob> FirstClass => this.playerStateService.FirstClass;

    /// <inheritdoc />
    public RowRef<Town> StartTown => this.playerStateService.StartTown;

    /// <inheritdoc />
    public int BaseStrength => this.playerStateService.BaseStrength;

    /// <inheritdoc />
    public int BaseDexterity => this.playerStateService.BaseDexterity;

    /// <inheritdoc />
    public int BaseVitality => this.playerStateService.BaseVitality;

    /// <inheritdoc />
    public int BaseIntelligence => this.playerStateService.BaseIntelligence;

    /// <inheritdoc />
    public int BaseMind => this.playerStateService.BaseMind;

    /// <inheritdoc />
    public int BasePiety => this.playerStateService.BasePiety;

    /// <inheritdoc />
    public RowRef<GrandCompany> GrandCompany => this.playerStateService.GrandCompany;

    /// <inheritdoc />
    public RowRef<Aetheryte> HomeAetheryte => this.playerStateService.HomeAetheryte;

    /// <inheritdoc />
    public ReadOnlySpan<RowRef<Aetheryte>> FavouriteAetherytes => this.playerStateService.FavouriteAetherytes;

    /// <inheritdoc />
    public RowRef<Aetheryte> FreeAetheryte => this.playerStateService.FreeAetheryte;

    /// <inheritdoc />
    public uint BaseRestedExperience => this.playerStateService.BaseRestedExperience;

    /// <inheritdoc />
    public short PlayerCommendations => this.playerStateService.PlayerCommendations;

    /// <inheritdoc />
    public byte DeliveryLevel => this.playerStateService.DeliveryLevel;

    /// <inheritdoc />
    public MentorVersion MentorVersion => this.playerStateService.MentorVersion;

    /// <inheritdoc />
    public int GetAttribute(PlayerAttribute attribute)
        => this.playerStateService.GetAttribute(attribute);

    /// <inheritdoc />
    public int GetClassJobExperience(ClassJob classJob)
        => this.playerStateService.GetClassJobExperience(classJob);

    /// <inheritdoc />
    public short GetClassJobLevel(ClassJob classJob)
        => this.playerStateService.GetClassJobLevel(classJob);

    /// <inheritdoc />
    public float GetDesynthesisLevel(ClassJob classJob)
        => this.playerStateService.GetDesynthesisLevel(classJob);

    /// <inheritdoc />
    public byte GetGrandCompanyRank(GrandCompany grandCompany)
        => this.playerStateService.GetGrandCompanyRank(grandCompany);

    /// <inheritdoc />
    public bool IsActionUnlocked(Lumina.Excel.Sheets.Action row)
        => this.playerStateService.IsActionUnlocked(row);

    /// <inheritdoc />
    public bool IsAetherCurrentCompFlgSetUnlocked(AetherCurrentCompFlgSet row)
        => this.playerStateService.IsAetherCurrentCompFlgSetUnlocked(row);

    /// <inheritdoc />
    public bool IsAetherCurrentUnlocked(AetherCurrent row)
        => this.playerStateService.IsAetherCurrentUnlocked(row);

    /// <inheritdoc />
    public bool IsAozActionUnlocked(AozAction row)
        => this.playerStateService.IsAozActionUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerBgUnlocked(BannerBg row)
        => this.playerStateService.IsBannerBgUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerConditionUnlocked(BannerCondition row)
        => this.playerStateService.IsBannerConditionUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerDecorationUnlocked(BannerDecoration row)
        => this.playerStateService.IsBannerDecorationUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerFacialUnlocked(BannerFacial row)
        => this.playerStateService.IsBannerFacialUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerFrameUnlocked(BannerFrame row)
        => this.playerStateService.IsBannerFrameUnlocked(row);

    /// <inheritdoc />
    public bool IsBannerTimelineUnlocked(BannerTimeline row)
        => this.playerStateService.IsBannerTimelineUnlocked(row);

    /// <inheritdoc />
    public bool IsBuddyActionUnlocked(BuddyAction row)
        => this.playerStateService.IsBuddyActionUnlocked(row);

    /// <inheritdoc />
    public bool IsBuddyEquipUnlocked(BuddyEquip row)
        => this.playerStateService.IsBuddyEquipUnlocked(row);

    /// <inheritdoc />
    public bool IsCharaMakeCustomizeUnlocked(CharaMakeCustomize row)
        => this.playerStateService.IsCharaMakeCustomizeUnlocked(row);

    /// <inheritdoc />
    public bool IsChocoboTaxiUnlocked(ChocoboTaxi row)
        => this.playerStateService.IsChocoboTaxiUnlocked(row);

    /// <inheritdoc />
    public bool IsCompanionUnlocked(Companion row)
        => this.playerStateService.IsCompanionUnlocked(row);

    /// <inheritdoc />
    public bool IsCraftActionUnlocked(CraftAction row)
        => this.playerStateService.IsCraftActionUnlocked(row);

    /// <inheritdoc />
    public bool IsCSBonusContentTypeUnlocked(CSBonusContentType row)
        => this.playerStateService.IsCSBonusContentTypeUnlocked(row);

    /// <inheritdoc />
    public bool IsEmoteUnlocked(Emote row)
        => this.playerStateService.IsEmoteUnlocked(row);

    /// <inheritdoc />
    public bool IsGeneralActionUnlocked(GeneralAction row)
        => this.playerStateService.IsGeneralActionUnlocked(row);

    /// <inheritdoc />
    public bool IsGlassesUnlocked(Glasses row)
        => this.playerStateService.IsGlassesUnlocked(row);

    /// <inheritdoc />
    public bool IsHowToUnlocked(HowTo row)
        => this.playerStateService.IsHowToUnlocked(row);

    /// <inheritdoc />
    public bool IsInstanceContentUnlocked(InstanceContent row)
        => this.playerStateService.IsInstanceContentUnlocked(row);

    /// <inheritdoc />
    public bool IsItemUnlockable(Item item)
        => this.playerStateService.IsItemUnlockable(item);

    /// <inheritdoc />
    public bool IsItemUnlocked(Item item)
        => this.playerStateService.IsItemUnlocked(item);

    /// <inheritdoc />
    public bool IsMcGuffinUnlocked(McGuffin row)
        => this.playerStateService.IsMcGuffinUnlocked(row);

    /// <inheritdoc />
    public bool IsMJILandmarkUnlocked(MJILandmark row)
        => this.playerStateService.IsMJILandmarkUnlocked(row);

    /// <inheritdoc />
    public bool IsMountUnlocked(Mount row)
        => this.playerStateService.IsMountUnlocked(row);

    /// <inheritdoc />
    public bool IsNotebookDivisionUnlocked(NotebookDivision row)
        => this.playerStateService.IsNotebookDivisionUnlocked(row);

    /// <inheritdoc />
    public bool IsOrchestrionUnlocked(Orchestrion row)
        => this.playerStateService.IsOrchestrionUnlocked(row);

    /// <inheritdoc />
    public bool IsOrnamentUnlocked(Ornament row)
        => this.playerStateService.IsOrnamentUnlocked(row);

    /// <inheritdoc />
    public bool IsPerformUnlocked(Perform row)
        => this.playerStateService.IsPerformUnlocked(row);

    /// <inheritdoc />
    public bool IsPublicContentUnlocked(PublicContent row)
        => this.playerStateService.IsPublicContentUnlocked(row);

    /// <inheritdoc />
    public bool IsRowRefUnlocked(RowRef rowRef)
        => this.playerStateService.IsRowRefUnlocked(rowRef);

    /// <inheritdoc />
    public bool IsRowRefUnlocked<T>(RowRef<T> rowRef) where T : struct, IExcelRow<T>
        => this.playerStateService.IsRowRefUnlocked(rowRef);

    /// <inheritdoc />
    public bool IsSecretRecipeBookUnlocked(SecretRecipeBook row)
        => this.playerStateService.IsSecretRecipeBookUnlocked(row);

    /// <inheritdoc />
    public bool IsTraitUnlocked(Trait row)
        => this.playerStateService.IsTraitUnlocked(row);

    /// <inheritdoc />
    public bool IsTripleTriadCardUnlocked(TripleTriadCard row)
        => this.playerStateService.IsTripleTriadCardUnlocked(row);

    /// <inheritdoc />
    public bool IsUnlockLinkUnlocked(uint unlockLink)
        => this.playerStateService.IsUnlockLinkUnlocked(unlockLink);

    /// <inheritdoc />
    public bool IsUnlockLinkUnlocked(ushort unlockLink)
        => this.playerStateService.IsUnlockLinkUnlocked(unlockLink);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.playerStateService.ClassJobChange -= this.OnClassJobChange;
        this.playerStateService.LevelChange -= this.OnLevelChange;
        this.playerStateService.Unlock -= this.OnUnlock;
    }

    private void OnLevelChange(uint classJobId, uint level)
        => this.LevelChange?.Invoke(classJobId, level);

    private void OnClassJobChange(uint classJobId)
        => this.ClassJobChange?.Invoke(classJobId);

    private void OnUnlock(RowRef rowRef)
        => this.Unlock?.Invoke(rowRef);
}

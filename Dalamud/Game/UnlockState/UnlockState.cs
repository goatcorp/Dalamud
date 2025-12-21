using System.Collections.Concurrent;
using System.Collections.Generic;

using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using ActionSheet = Lumina.Excel.Sheets.Action;
using InstanceContentSheet = Lumina.Excel.Sheets.InstanceContent;
using PublicContentSheet = Lumina.Excel.Sheets.PublicContent;

namespace Dalamud.Game.UnlockState;

#pragma warning disable Dalamud001

/// <summary>
/// This class provides unlock state of various content in the game.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class UnlockState : IInternalDisposableService, IUnlockState
{
    private static readonly ModuleLog Log = new(nameof(UnlockState));

    private readonly ConcurrentDictionary<Type, HashSet<uint>> cachedUnlockedRowIds = [];

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly RecipeData recipeData = Service<RecipeData>.Get();

    [ServiceManager.ServiceConstructor]
    private UnlockState()
    {
        this.clientState.Login += this.OnLogin;
        this.clientState.Logout += this.OnLogout;
        this.gameGui.AgentUpdate += this.OnAgentUpdate;
    }

    /// <inheritdoc/>
    public event IUnlockState.UnlockDelegate Unlock;

    private bool IsLoaded => PlayerState.Instance()->IsLoaded;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.clientState.Login -= this.OnLogin;
        this.clientState.Logout -= this.OnLogout;
        this.gameGui.AgentUpdate -= this.OnAgentUpdate;
    }

    /// <inheritdoc/>
    public bool IsActionUnlocked(ActionSheet row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink.RowId);
    }

    /// <inheritdoc/>
    public bool IsAetherCurrentUnlocked(AetherCurrent row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsAetherCurrentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsAetherCurrentCompFlgSetUnlocked(AetherCurrentCompFlgSet row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsAetherCurrentZoneComplete(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsAozActionUnlocked(AozAction row)
    {
        if (!this.IsLoaded)
            return false;

        if (row.RowId == 0 || !row.Action.IsValid)
            return false;

        return UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(row.Action.Value.UnlockLink.RowId);
    }

    /// <inheritdoc/>
    public bool IsBannerBgUnlocked(BannerBg row)
    {
        return row.UnlockCondition.IsValid && this.IsBannerConditionUnlocked(row.UnlockCondition.Value);
    }

    /// <inheritdoc/>
    public bool IsBannerConditionUnlocked(BannerCondition row)
    {
        if (row.RowId == 0)
            return false;

        if (!this.IsLoaded)
            return false;

        var rowPtr = ExdModule.GetBannerConditionByIndex(row.RowId);
        if (rowPtr == null)
            return false;

        return ExdModule.GetBannerConditionUnlockState(rowPtr) == 0;
    }

    /// <inheritdoc/>
    public bool IsBannerDecorationUnlocked(BannerDecoration row)
    {
        return row.UnlockCondition.IsValid && this.IsBannerConditionUnlocked(row.UnlockCondition.Value);
    }

    /// <inheritdoc/>
    public bool IsBannerFacialUnlocked(BannerFacial row)
    {
        return row.UnlockCondition.IsValid && this.IsBannerConditionUnlocked(row.UnlockCondition.Value);
    }

    /// <inheritdoc/>
    public bool IsBannerFrameUnlocked(BannerFrame row)
    {
        return row.UnlockCondition.IsValid && this.IsBannerConditionUnlocked(row.UnlockCondition.Value);
    }

    /// <inheritdoc/>
    public bool IsBannerTimelineUnlocked(BannerTimeline row)
    {
        return row.UnlockCondition.IsValid && this.IsBannerConditionUnlocked(row.UnlockCondition.Value);
    }

    /// <inheritdoc/>
    public bool IsBuddyActionUnlocked(BuddyAction row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsBuddyEquipUnlocked(BuddyEquip row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsCharaMakeCustomizeUnlocked(CharaMakeCustomize row)
    {
        return row.IsPurchasable && this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsChocoboTaxiStandUnlocked(ChocoboTaxiStand row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->IsChocoboTaxiStandUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsCompanionUnlocked(Companion row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->IsCompanionUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsCraftActionUnlocked(CraftAction row)
    {
        return this.IsUnlockLinkUnlocked(row.QuestRequirement.RowId);
    }

    /// <inheritdoc/>
    public bool IsCSBonusContentTypeUnlocked(CSBonusContentType row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsEmoteUnlocked(Emote row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsEmjVoiceNpcUnlocked(EmjVoiceNpc row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsEmjCostumeUnlocked(EmjCostume row)
    {
        return this.dataManager.GetExcelSheet<EmjVoiceNpc>().TryGetRow(row.RowId, out var emjVoiceNpcRow)
            && this.IsEmjVoiceNpcUnlocked(emjVoiceNpcRow)
            && QuestManager.IsQuestComplete(row.UnlockQuest.RowId);
    }

    /// <inheritdoc/>
    public bool IsGeneralActionUnlocked(GeneralAction row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsGlassesUnlocked(Glasses row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsGlassesUnlocked((ushort)row.RowId);
    }

    /// <inheritdoc/>
    public bool IsHowToUnlocked(HowTo row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->IsHowToUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsInstanceContentUnlocked(InstanceContentSheet row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.IsInstanceContentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public unsafe bool IsItemUnlocked(Item row)
    {
        if (row.ItemAction.RowId == 0)
            return false;

        if (!this.IsLoaded)
            return false;

        // To avoid the ExdModule.GetItemRowById call, which can return null if the excel page
        // is not loaded, we're going to imitate the IsItemActionUnlocked call first:
        switch ((ItemActionAction)row.ItemAction.Value.Action.RowId)
        {
            case ItemActionAction.Companion:
                return UIState.Instance()->IsCompanionUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.BuddyEquip:
                return UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.Mount:
                return PlayerState.Instance()->IsMountUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.SecretRecipeBook:
                return PlayerState.Instance()->IsSecretRecipeBookUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.UnlockLink:
            case ItemActionAction.OccultRecords:
                return UIState.Instance()->IsUnlockLinkUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.TripleTriadCard when row.AdditionalData.Is<TripleTriadCard>():
                return UIState.Instance()->IsTripleTriadCardUnlocked((ushort)row.AdditionalData.RowId);

            case ItemActionAction.FolkloreTome:
                return PlayerState.Instance()->IsFolkloreBookUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.OrchestrionRoll when row.AdditionalData.Is<Orchestrion>():
                return PlayerState.Instance()->IsOrchestrionRollUnlocked(row.AdditionalData.RowId);

            case ItemActionAction.FramersKit:
                return PlayerState.Instance()->IsFramersKitUnlocked(row.AdditionalData.RowId);

            case ItemActionAction.Ornament:
                return PlayerState.Instance()->IsOrnamentUnlocked(row.ItemAction.Value.Data[0].RowId);

            case ItemActionAction.Glasses:
                return PlayerState.Instance()->IsGlassesUnlocked((ushort)row.AdditionalData.RowId);

            case ItemActionAction.SoulShards when PublicContentOccultCrescent.GetState() is var occultCrescentState && occultCrescentState != null:
                var supportJobId = (byte)row.ItemAction.Value.Data[0].RowId;
                return supportJobId < occultCrescentState->SupportJobLevels.Length && occultCrescentState->SupportJobLevels[supportJobId] != 0;

            case ItemActionAction.CompanySealVouchers:
                return false;
        }

        var nativeRow = ExdModule.GetItemRowById(row.RowId);
        return nativeRow != null && UIState.Instance()->IsItemActionUnlocked(nativeRow) == 1;
    }

    /// <inheritdoc/>
    public bool IsMcGuffinUnlocked(McGuffin row)
    {
        return PlayerState.Instance()->IsMcGuffinUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsMJILandmarkUnlocked(MJILandmark row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsMKDLoreUnlocked(MKDLore row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsMountUnlocked(Mount row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsMountUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsNotebookDivisionUnlocked(NotebookDivision row)
    {
        return this.IsUnlockLinkUnlocked(row.QuestUnlock.RowId);
    }

    /// <inheritdoc/>
    public bool IsOrchestrionUnlocked(Orchestrion row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsOrchestrionRollUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsOrnamentUnlocked(Ornament row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsPerformUnlocked(Perform row)
    {
        return this.IsUnlockLinkUnlocked((uint)row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsPublicContentUnlocked(PublicContentSheet row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.IsPublicContentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsRecipeUnlocked(Recipe row)
    {
        return this.recipeData.IsRecipeUnlocked(row);
    }

    /// <inheritdoc/>
    public bool IsSecretRecipeBookUnlocked(SecretRecipeBook row)
    {
        if (!this.IsLoaded)
            return false;

        return PlayerState.Instance()->IsSecretRecipeBookUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsTraitUnlocked(Trait row)
    {
        return this.IsUnlockLinkUnlocked(row.Quest.RowId);
    }

    /// <inheritdoc/>
    public bool IsTripleTriadCardUnlocked(TripleTriadCard row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->IsTripleTriadCardUnlocked((ushort)row.RowId);
    }

    /// <inheritdoc/>
    public bool IsItemUnlockable(Item row)
    {
        if (row.ItemAction.RowId == 0)
            return false;

        return (ItemActionAction)row.ItemAction.Value.Action.RowId
            is ItemActionAction.Companion
            or ItemActionAction.BuddyEquip
            or ItemActionAction.Mount
            or ItemActionAction.SecretRecipeBook
            or ItemActionAction.UnlockLink
            or ItemActionAction.TripleTriadCard
            or ItemActionAction.FolkloreTome
            or ItemActionAction.OrchestrionRoll
            or ItemActionAction.FramersKit
            or ItemActionAction.Ornament
            or ItemActionAction.Glasses
            or ItemActionAction.OccultRecords
            or ItemActionAction.SoulShards;
    }

    /// <inheritdoc/>
    public bool IsRowRefUnlocked<T>(RowRef<T> rowRef) where T : struct, IExcelRow<T>
    {
        return this.IsRowRefUnlocked((RowRef)rowRef);
    }

    /// <inheritdoc/>
    public bool IsRowRefUnlocked(RowRef rowRef)
    {
        if (!this.IsLoaded || rowRef.IsUntyped)
            return false;

        if (rowRef.TryGetValue<ActionSheet>(out var actionRow))
            return this.IsActionUnlocked(actionRow);

        if (rowRef.TryGetValue<AetherCurrent>(out var aetherCurrentRow))
            return this.IsAetherCurrentUnlocked(aetherCurrentRow);

        if (rowRef.TryGetValue<AetherCurrentCompFlgSet>(out var aetherCurrentCompFlgSetRow))
            return this.IsAetherCurrentCompFlgSetUnlocked(aetherCurrentCompFlgSetRow);

        if (rowRef.TryGetValue<AozAction>(out var aozActionRow))
            return this.IsAozActionUnlocked(aozActionRow);

        if (rowRef.TryGetValue<BannerBg>(out var bannerBgRow))
            return this.IsBannerBgUnlocked(bannerBgRow);

        if (rowRef.TryGetValue<BannerCondition>(out var bannerConditionRow))
            return this.IsBannerConditionUnlocked(bannerConditionRow);

        if (rowRef.TryGetValue<BannerDecoration>(out var bannerDecorationRow))
            return this.IsBannerDecorationUnlocked(bannerDecorationRow);

        if (rowRef.TryGetValue<BannerFacial>(out var bannerFacialRow))
            return this.IsBannerFacialUnlocked(bannerFacialRow);

        if (rowRef.TryGetValue<BannerFrame>(out var bannerFrameRow))
            return this.IsBannerFrameUnlocked(bannerFrameRow);

        if (rowRef.TryGetValue<BannerTimeline>(out var bannerTimelineRow))
            return this.IsBannerTimelineUnlocked(bannerTimelineRow);

        if (rowRef.TryGetValue<BuddyAction>(out var buddyActionRow))
            return this.IsBuddyActionUnlocked(buddyActionRow);

        if (rowRef.TryGetValue<BuddyEquip>(out var buddyEquipRow))
            return this.IsBuddyEquipUnlocked(buddyEquipRow);

        if (rowRef.TryGetValue<CSBonusContentType>(out var csBonusContentTypeRow))
            return this.IsCSBonusContentTypeUnlocked(csBonusContentTypeRow);

        if (rowRef.TryGetValue<CharaMakeCustomize>(out var charaMakeCustomizeRow))
            return this.IsCharaMakeCustomizeUnlocked(charaMakeCustomizeRow);

        if (rowRef.TryGetValue<ChocoboTaxiStand>(out var chocoboTaxiStandRow))
            return this.IsChocoboTaxiStandUnlocked(chocoboTaxiStandRow);

        if (rowRef.TryGetValue<Companion>(out var companionRow))
            return this.IsCompanionUnlocked(companionRow);

        if (rowRef.TryGetValue<CraftAction>(out var craftActionRow))
            return this.IsCraftActionUnlocked(craftActionRow);

        if (rowRef.TryGetValue<Emote>(out var emoteRow))
            return this.IsEmoteUnlocked(emoteRow);

        if (rowRef.TryGetValue<GeneralAction>(out var generalActionRow))
            return this.IsGeneralActionUnlocked(generalActionRow);

        if (rowRef.TryGetValue<Glasses>(out var glassesRow))
            return this.IsGlassesUnlocked(glassesRow);

        if (rowRef.TryGetValue<HowTo>(out var howToRow))
            return this.IsHowToUnlocked(howToRow);

        if (rowRef.TryGetValue<InstanceContentSheet>(out var instanceContentRow))
            return this.IsInstanceContentUnlocked(instanceContentRow);

        if (rowRef.TryGetValue<Item>(out var itemRow))
            return this.IsItemUnlocked(itemRow);

        if (rowRef.TryGetValue<MJILandmark>(out var mjiLandmarkRow))
            return this.IsMJILandmarkUnlocked(mjiLandmarkRow);

        if (rowRef.TryGetValue<MKDLore>(out var mkdLoreRow))
            return this.IsMKDLoreUnlocked(mkdLoreRow);

        if (rowRef.TryGetValue<McGuffin>(out var mcGuffinRow))
            return this.IsMcGuffinUnlocked(mcGuffinRow);

        if (rowRef.TryGetValue<Mount>(out var mountRow))
            return this.IsMountUnlocked(mountRow);

        if (rowRef.TryGetValue<NotebookDivision>(out var notebookDivisionRow))
            return this.IsNotebookDivisionUnlocked(notebookDivisionRow);

        if (rowRef.TryGetValue<Orchestrion>(out var orchestrionRow))
            return this.IsOrchestrionUnlocked(orchestrionRow);

        if (rowRef.TryGetValue<Ornament>(out var ornamentRow))
            return this.IsOrnamentUnlocked(ornamentRow);

        if (rowRef.TryGetValue<Perform>(out var performRow))
            return this.IsPerformUnlocked(performRow);

        if (rowRef.TryGetValue<PublicContentSheet>(out var publicContentRow))
            return this.IsPublicContentUnlocked(publicContentRow);

        if (rowRef.TryGetValue<Recipe>(out var recipeRow))
            return this.IsRecipeUnlocked(recipeRow);

        if (rowRef.TryGetValue<SecretRecipeBook>(out var secretRecipeBookRow))
            return this.IsSecretRecipeBookUnlocked(secretRecipeBookRow);

        if (rowRef.TryGetValue<Trait>(out var traitRow))
            return this.IsTraitUnlocked(traitRow);

        if (rowRef.TryGetValue<TripleTriadCard>(out var tripleTriadCardRow))
            return this.IsTripleTriadCardUnlocked(tripleTriadCardRow);

        return false;
    }

    /// <inheritdoc/>
    public bool IsUnlockLinkUnlocked(ushort unlockLink)
    {
        if (!this.IsLoaded)
            return false;

        if (unlockLink == 0)
            return false;

        return UIState.Instance()->IsUnlockLinkUnlocked(unlockLink);
    }

    /// <inheritdoc/>
    public bool IsUnlockLinkUnlocked(uint unlockLink)
    {
        if (!this.IsLoaded)
            return false;

        if (unlockLink == 0)
            return false;

        return UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(unlockLink);
    }

    private void OnLogin()
    {
        this.Update();
    }

    private void OnLogout(int type, int code)
    {
        this.cachedUnlockedRowIds.Clear();
    }

    private void OnAgentUpdate(AgentUpdateFlag agentUpdateFlag)
    {
        if (agentUpdateFlag.HasFlag(AgentUpdateFlag.UnlocksUpdate))
            this.Update();
    }

    private void Update()
    {
        if (!this.IsLoaded)
            return;

        this.UpdateUnlocksForSheet<ActionSheet>();
        this.UpdateUnlocksForSheet<AetherCurrent>();
        this.UpdateUnlocksForSheet<AetherCurrentCompFlgSet>();
        this.UpdateUnlocksForSheet<AozAction>();
        this.UpdateUnlocksForSheet<BannerBg>();
        this.UpdateUnlocksForSheet<BannerCondition>();
        this.UpdateUnlocksForSheet<BannerDecoration>();
        this.UpdateUnlocksForSheet<BannerFacial>();
        this.UpdateUnlocksForSheet<BannerFrame>();
        this.UpdateUnlocksForSheet<BannerTimeline>();
        this.UpdateUnlocksForSheet<BuddyAction>();
        this.UpdateUnlocksForSheet<BuddyEquip>();
        this.UpdateUnlocksForSheet<CSBonusContentType>();
        this.UpdateUnlocksForSheet<CharaMakeCustomize>();
        this.UpdateUnlocksForSheet<ChocoboTaxi>();
        this.UpdateUnlocksForSheet<Companion>();
        this.UpdateUnlocksForSheet<CraftAction>();
        this.UpdateUnlocksForSheet<EmjVoiceNpc>();
        this.UpdateUnlocksForSheet<Emote>();
        this.UpdateUnlocksForSheet<GeneralAction>();
        this.UpdateUnlocksForSheet<Glasses>();
        this.UpdateUnlocksForSheet<HowTo>();
        this.UpdateUnlocksForSheet<InstanceContentSheet>();
        this.UpdateUnlocksForSheet<Item>();
        this.UpdateUnlocksForSheet<MJILandmark>();
        this.UpdateUnlocksForSheet<MKDLore>();
        this.UpdateUnlocksForSheet<McGuffin>();
        this.UpdateUnlocksForSheet<Mount>();
        this.UpdateUnlocksForSheet<NotebookDivision>();
        this.UpdateUnlocksForSheet<Orchestrion>();
        this.UpdateUnlocksForSheet<Ornament>();
        this.UpdateUnlocksForSheet<Perform>();
        this.UpdateUnlocksForSheet<PublicContentSheet>();
        this.UpdateUnlocksForSheet<Recipe>();
        this.UpdateUnlocksForSheet<SecretRecipeBook>();
        this.UpdateUnlocksForSheet<Trait>();
        this.UpdateUnlocksForSheet<TripleTriadCard>();

        // Not implemented:
        // - DescriptionPage: quite complex
        // - QuestAcceptAdditionCondition: ignored

        // For some other day:
        // - FishingSpot
        // - Spearfishing
        // - Adventure (Sightseeing)
        // - MinerFolkloreTome
        // - BotanistFolkloreTome
        // - FishingFolkloreTome
        // - VVD or is that unlocked via quest?
        // - VVDNotebookContents?
        // - FramersKit (is that just an Item?)
        // - ... more?

        // Subrow sheets, which are incompatible with the current Unlock event, since RowRef doesn't carry the SubrowId:
        // - EmjCostume

        // Probably not happening, because it requires fetching data from server:
        // - Achievements
        // - Titles
        // - Bozjan Field Notes
        // - Support/Phantom Jobs, which require to be in Occult Crescent, because it checks the jobs level for != 0
    }

    private void UpdateUnlocksForSheet<T>() where T : struct, IExcelRow<T>
    {
        var unlockedRowIds = this.cachedUnlockedRowIds.GetOrAdd(typeof(T), _ => []);

        foreach (var row in this.dataManager.GetExcelSheet<T>())
        {
            if (unlockedRowIds.Contains(row.RowId))
                continue;

            var rowRef = LuminaUtils.CreateRef<T>(row.RowId);

            if (!this.IsRowRefUnlocked(rowRef))
                continue;

            unlockedRowIds.Add(row.RowId);

            Log.Verbose($"Unlock detected: {typeof(T).Name}#{row.RowId}");

            foreach (var action in Delegate.EnumerateInvocationList(this.Unlock))
            {
                try
                {
                    action((RowRef)rowRef);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception during raise of {handler}", action.Method);
                }
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a <see cref="UnlockState"/> service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IUnlockState>]
#pragma warning restore SA1015
internal class UnlockStatePluginScoped : IInternalDisposableService, IUnlockState
{
    [ServiceManager.ServiceDependency]
    private readonly UnlockState unlockStateService = Service<UnlockState>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockStatePluginScoped"/> class.
    /// </summary>
    internal UnlockStatePluginScoped()
    {
        this.unlockStateService.Unlock += this.UnlockForward;
    }

    /// <inheritdoc/>
    public event IUnlockState.UnlockDelegate? Unlock;

    /// <inheritdoc/>
    public bool IsActionUnlocked(ActionSheet row) => this.unlockStateService.IsActionUnlocked(row);

    /// <inheritdoc/>
    public bool IsAetherCurrentCompFlgSetUnlocked(AetherCurrentCompFlgSet row) => this.unlockStateService.IsAetherCurrentCompFlgSetUnlocked(row);

    /// <inheritdoc/>
    public bool IsAetherCurrentUnlocked(AetherCurrent row) => this.unlockStateService.IsAetherCurrentUnlocked(row);

    /// <inheritdoc/>
    public bool IsAozActionUnlocked(AozAction row) => this.unlockStateService.IsAozActionUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerBgUnlocked(BannerBg row) => this.unlockStateService.IsBannerBgUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerConditionUnlocked(BannerCondition row) => this.unlockStateService.IsBannerConditionUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerDecorationUnlocked(BannerDecoration row) => this.unlockStateService.IsBannerDecorationUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerFacialUnlocked(BannerFacial row) => this.unlockStateService.IsBannerFacialUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerFrameUnlocked(BannerFrame row) => this.unlockStateService.IsBannerFrameUnlocked(row);

    /// <inheritdoc/>
    public bool IsBannerTimelineUnlocked(BannerTimeline row) => this.unlockStateService.IsBannerTimelineUnlocked(row);

    /// <inheritdoc/>
    public bool IsBuddyActionUnlocked(BuddyAction row) => this.unlockStateService.IsBuddyActionUnlocked(row);

    /// <inheritdoc/>
    public bool IsBuddyEquipUnlocked(BuddyEquip row) => this.unlockStateService.IsBuddyEquipUnlocked(row);

    /// <inheritdoc/>
    public bool IsCharaMakeCustomizeUnlocked(CharaMakeCustomize row) => this.unlockStateService.IsCharaMakeCustomizeUnlocked(row);

    /// <inheritdoc/>
    public bool IsChocoboTaxiStandUnlocked(ChocoboTaxiStand row) => this.unlockStateService.IsChocoboTaxiStandUnlocked(row);

    /// <inheritdoc/>
    public bool IsCompanionUnlocked(Companion row) => this.unlockStateService.IsCompanionUnlocked(row);

    /// <inheritdoc/>
    public bool IsCraftActionUnlocked(CraftAction row) => this.unlockStateService.IsCraftActionUnlocked(row);

    /// <inheritdoc/>
    public bool IsCSBonusContentTypeUnlocked(CSBonusContentType row) => this.unlockStateService.IsCSBonusContentTypeUnlocked(row);

    /// <inheritdoc/>
    public bool IsEmoteUnlocked(Emote row) => this.unlockStateService.IsEmoteUnlocked(row);

    /// <inheritdoc/>
    public bool IsEmjVoiceNpcUnlocked(EmjVoiceNpc row) => this.unlockStateService.IsEmjVoiceNpcUnlocked(row);

    /// <inheritdoc/>
    public bool IsEmjCostumeUnlocked(EmjCostume row) => this.unlockStateService.IsEmjCostumeUnlocked(row);

    /// <inheritdoc/>
    public bool IsGeneralActionUnlocked(GeneralAction row) => this.unlockStateService.IsGeneralActionUnlocked(row);

    /// <inheritdoc/>
    public bool IsGlassesUnlocked(Glasses row) => this.unlockStateService.IsGlassesUnlocked(row);

    /// <inheritdoc/>
    public bool IsHowToUnlocked(HowTo row) => this.unlockStateService.IsHowToUnlocked(row);

    /// <inheritdoc/>
    public bool IsInstanceContentUnlocked(InstanceContentSheet row) => this.unlockStateService.IsInstanceContentUnlocked(row);

    /// <inheritdoc/>
    public bool IsItemUnlockable(Item row) => this.unlockStateService.IsItemUnlockable(row);

    /// <inheritdoc/>
    public bool IsItemUnlocked(Item row) => this.unlockStateService.IsItemUnlocked(row);

    /// <inheritdoc/>
    public bool IsMcGuffinUnlocked(McGuffin row) => this.unlockStateService.IsMcGuffinUnlocked(row);

    /// <inheritdoc/>
    public bool IsMJILandmarkUnlocked(MJILandmark row) => this.unlockStateService.IsMJILandmarkUnlocked(row);

    /// <inheritdoc/>
    public bool IsMKDLoreUnlocked(MKDLore row) => this.unlockStateService.IsMKDLoreUnlocked(row);

    /// <inheritdoc/>
    public bool IsMountUnlocked(Mount row) => this.unlockStateService.IsMountUnlocked(row);

    /// <inheritdoc/>
    public bool IsNotebookDivisionUnlocked(NotebookDivision row) => this.unlockStateService.IsNotebookDivisionUnlocked(row);

    /// <inheritdoc/>
    public bool IsOrchestrionUnlocked(Orchestrion row) => this.unlockStateService.IsOrchestrionUnlocked(row);

    /// <inheritdoc/>
    public bool IsOrnamentUnlocked(Ornament row) => this.unlockStateService.IsOrnamentUnlocked(row);

    /// <inheritdoc/>
    public bool IsPerformUnlocked(Perform row) => this.unlockStateService.IsPerformUnlocked(row);

    /// <inheritdoc/>
    public bool IsPublicContentUnlocked(PublicContentSheet row) => this.unlockStateService.IsPublicContentUnlocked(row);

    /// <inheritdoc/>
    public bool IsRecipeUnlocked(Recipe row) => this.unlockStateService.IsRecipeUnlocked(row);

    /// <inheritdoc/>
    public bool IsRowRefUnlocked(RowRef rowRef) => this.unlockStateService.IsRowRefUnlocked(rowRef);

    /// <inheritdoc/>
    public bool IsRowRefUnlocked<T>(RowRef<T> rowRef) where T : struct, IExcelRow<T> => this.unlockStateService.IsRowRefUnlocked(rowRef);

    /// <inheritdoc/>
    public bool IsSecretRecipeBookUnlocked(SecretRecipeBook row) => this.unlockStateService.IsSecretRecipeBookUnlocked(row);

    /// <inheritdoc/>
    public bool IsTraitUnlocked(Trait row) => this.unlockStateService.IsTraitUnlocked(row);

    /// <inheritdoc/>
    public bool IsTripleTriadCardUnlocked(TripleTriadCard row) => this.unlockStateService.IsTripleTriadCardUnlocked(row);

    /// <inheritdoc/>
    public bool IsUnlockLinkUnlocked(uint unlockLink) => this.unlockStateService.IsUnlockLinkUnlocked(unlockLink);

    /// <inheritdoc/>
    public bool IsUnlockLinkUnlocked(ushort unlockLink) => this.unlockStateService.IsUnlockLinkUnlocked(unlockLink);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.unlockStateService.Unlock -= this.UnlockForward;
    }

    private void UnlockForward(RowRef rowRef) => this.Unlock?.Invoke(rowRef);
}

using Dalamud.Data;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using CSPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace Dalamud.Game.PlayerState;

/// <summary>
/// This class represents the state of the players unlocks.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe partial class PlayerState : IInternalDisposableService, IPlayerState
{
    /// <inheritdoc/>
    public bool IsActionUnlocked(Lumina.Excel.Sheets.Action row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink.RowId);
    }

    /// <inheritdoc/>
    public bool IsAetherCurrentUnlocked(AetherCurrent row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsAetherCurrentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsAetherCurrentCompFlgSetUnlocked(AetherCurrentCompFlgSet row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsAetherCurrentZoneComplete(row.RowId);
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
    public bool IsChocoboTaxiUnlocked(ChocoboTaxi row)
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
    public bool IsGeneralActionUnlocked(GeneralAction row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsGlassesUnlocked(Glasses row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsGlassesUnlocked((ushort)row.RowId);
    }

    /// <inheritdoc/>
    public bool IsHowToUnlocked(HowTo row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.Instance()->IsHowToUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsInstanceContentUnlocked(Lumina.Excel.Sheets.InstanceContent row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.IsInstanceContentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public unsafe bool IsItemUnlocked(Item item)
    {
        if (item.ItemAction.RowId == 0)
            return false;

        if (!this.IsLoaded)
            return false;

        // To avoid the ExdModule.GetItemRowById call, which can return null if the excel page
        // is not loaded, we're going to imitate the IsItemActionUnlocked call first:
        switch ((ItemActionType)item.ItemAction.Value.Type)
        {
            case ItemActionType.Companion:
                return UIState.Instance()->IsCompanionUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.BuddyEquip:
                return UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.Mount:
                return CSPlayerState.Instance()->IsMountUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.SecretRecipeBook:
                return CSPlayerState.Instance()->IsSecretRecipeBookUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.UnlockLink:
                return UIState.Instance()->IsUnlockLinkUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.TripleTriadCard when item.AdditionalData.Is<TripleTriadCard>():
                return UIState.Instance()->IsTripleTriadCardUnlocked((ushort)item.AdditionalData.RowId);

            case ItemActionType.FolkloreTome:
                return CSPlayerState.Instance()->IsFolkloreBookUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.OrchestrionRoll when item.AdditionalData.Is<Orchestrion>():
                return CSPlayerState.Instance()->IsOrchestrionRollUnlocked(item.AdditionalData.RowId);

            case ItemActionType.FramersKit:
                return CSPlayerState.Instance()->IsFramersKitUnlocked(item.AdditionalData.RowId);

            case ItemActionType.Ornament:
                return CSPlayerState.Instance()->IsOrnamentUnlocked(item.ItemAction.Value.Data[0]);

            case ItemActionType.Glasses:
                return CSPlayerState.Instance()->IsGlassesUnlocked((ushort)item.AdditionalData.RowId);

            case ItemActionType.CompanySealVouchers:
                return false;
        }

        var row = ExdModule.GetItemRowById(item.RowId);
        return row != null && UIState.Instance()->IsItemActionUnlocked(row) == 1;
    }

    /// <inheritdoc/>
    public bool IsMcGuffinUnlocked(McGuffin row)
    {
        return CSPlayerState.Instance()->IsMcGuffinUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsMJILandmarkUnlocked(MJILandmark row)
    {
        return this.IsUnlockLinkUnlocked(row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsMountUnlocked(Mount row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsMountUnlocked(row.RowId);
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

        return CSPlayerState.Instance()->IsOrchestrionRollUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsOrnamentUnlocked(Ornament row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsPerformUnlocked(Perform row)
    {
        return this.IsUnlockLinkUnlocked((uint)row.UnlockLink);
    }

    /// <inheritdoc/>
    public bool IsPublicContentUnlocked(PublicContent row)
    {
        if (!this.IsLoaded)
            return false;

        return UIState.IsPublicContentUnlocked(row.RowId);
    }

    /// <inheritdoc/>
    public bool IsSecretRecipeBookUnlocked(SecretRecipeBook row)
    {
        if (!this.IsLoaded)
            return false;

        return CSPlayerState.Instance()->IsSecretRecipeBookUnlocked(row.RowId);
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
    public bool IsItemUnlockable(Item item)
    {
        if (item.ItemAction.RowId == 0)
            return false;

        return (ItemActionType)item.ItemAction.Value.Type is
            ItemActionType.Companion or
            ItemActionType.BuddyEquip or
            ItemActionType.Mount or
            ItemActionType.SecretRecipeBook or
            ItemActionType.UnlockLink or
            ItemActionType.TripleTriadCard or
            ItemActionType.FolkloreTome or
            ItemActionType.OrchestrionRoll or
            ItemActionType.FramersKit or
            ItemActionType.Ornament or
            ItemActionType.Glasses;
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

        if (rowRef.TryGetValue<Lumina.Excel.Sheets.Action>(out var actionRow))
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

        if (rowRef.TryGetValue<ChocoboTaxi>(out var chocoboTaxiRow))
            return this.IsChocoboTaxiUnlocked(chocoboTaxiRow);

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

        if (rowRef.TryGetValue<Lumina.Excel.Sheets.InstanceContent>(out var instanceContentRow))
            return this.IsInstanceContentUnlocked(instanceContentRow);

        if (rowRef.TryGetValue<Item>(out var itemRow))
            return this.IsItemUnlocked(itemRow);

        if (rowRef.TryGetValue<MJILandmark>(out var mjiLandmarkRow))
            return this.IsMJILandmarkUnlocked(mjiLandmarkRow);

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

        if (rowRef.TryGetValue<PublicContent>(out var publicContentRow))
            return this.IsPublicContentUnlocked(publicContentRow);

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

    private void UpdateUnlocks(bool fireEvent)
    {
        if (!this.IsLoaded)
            return;

        this.UpdateUnlocksForSheet<Lumina.Excel.Sheets.Action>(fireEvent);
        this.UpdateUnlocksForSheet<AetherCurrent>(fireEvent);
        this.UpdateUnlocksForSheet<AetherCurrentCompFlgSet>(fireEvent);
        this.UpdateUnlocksForSheet<AozAction>(fireEvent);
        this.UpdateUnlocksForSheet<BannerBg>(fireEvent);
        this.UpdateUnlocksForSheet<BannerCondition>(fireEvent);
        this.UpdateUnlocksForSheet<BannerDecoration>(fireEvent);
        this.UpdateUnlocksForSheet<BannerFacial>(fireEvent);
        this.UpdateUnlocksForSheet<BannerFrame>(fireEvent);
        this.UpdateUnlocksForSheet<BannerTimeline>(fireEvent);
        this.UpdateUnlocksForSheet<BuddyAction>(fireEvent);
        this.UpdateUnlocksForSheet<BuddyEquip>(fireEvent);
        this.UpdateUnlocksForSheet<CSBonusContentType>(fireEvent);
        this.UpdateUnlocksForSheet<CharaMakeCustomize>(fireEvent);
        this.UpdateUnlocksForSheet<ChocoboTaxi>(fireEvent);
        this.UpdateUnlocksForSheet<Companion>(fireEvent);
        this.UpdateUnlocksForSheet<CraftAction>(fireEvent);
        this.UpdateUnlocksForSheet<Emote>(fireEvent);
        this.UpdateUnlocksForSheet<GeneralAction>(fireEvent);
        this.UpdateUnlocksForSheet<Glasses>(fireEvent);
        this.UpdateUnlocksForSheet<HowTo>(fireEvent);
        this.UpdateUnlocksForSheet<Lumina.Excel.Sheets.InstanceContent>(fireEvent);
        this.UpdateUnlocksForSheet<Item>(fireEvent);
        this.UpdateUnlocksForSheet<MJILandmark>(fireEvent);
        this.UpdateUnlocksForSheet<McGuffin>(fireEvent);
        this.UpdateUnlocksForSheet<Mount>(fireEvent);
        this.UpdateUnlocksForSheet<NotebookDivision>(fireEvent);
        this.UpdateUnlocksForSheet<Orchestrion>(fireEvent);
        this.UpdateUnlocksForSheet<Ornament>(fireEvent);
        this.UpdateUnlocksForSheet<Perform>(fireEvent);
        this.UpdateUnlocksForSheet<PublicContent>(fireEvent);
        this.UpdateUnlocksForSheet<SecretRecipeBook>(fireEvent);
        this.UpdateUnlocksForSheet<Trait>(fireEvent);
        this.UpdateUnlocksForSheet<TripleTriadCard>(fireEvent);

        // Not implemented:
        // - DescriptionPage: quite complex
        // - QuestAcceptAdditionCondition: ignored

        // For some other day:
        // - FishingSpot
        // - Spearfishing
        // - Adventure (Sightseeing)
        // - Recipes
        // - MinerFolkloreTome
        // - BotanistFolkloreTome
        // - FishingFolkloreTome
        // - VVD or is that unlocked via quest?
        // - VVDNotebookContents?
        // - FramersKit (is that just an Item?)
        // - ... more?

        // Probably not happening, because it requires fetching data from server:
        // - Achievements
        // - Titles
        // - Bozjan Field Notes
    }

    private void UpdateUnlocksForSheet<T>(bool fireEvent = true) where T : struct, IExcelRow<T>
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

            if (fireEvent)
            {
                Log.Verbose("Unlock detected: {row}", $"{typeof(T).Name}#{row.RowId}");

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
}

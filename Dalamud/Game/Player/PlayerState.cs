using System.Collections.Generic;

using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using CSPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;
using GrandCompany = Lumina.Excel.Sheets.GrandCompany;

namespace Dalamud.Game.Player;

/// <summary>
/// This class contains the PlayerState wrappers.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
[ResolveVia<IPlayerState>]
internal unsafe class PlayerState : IServiceType, IPlayerState
{
    [ServiceManager.ServiceConstructor]
    private PlayerState()
    {
    }

    /// <inheritdoc/>
    public bool IsLoaded => CSPlayerState.Instance()->IsLoaded;

    /// <inheritdoc/>
    public string CharacterName => this.IsLoaded ? CSPlayerState.Instance()->CharacterNameString : string.Empty;

    /// <inheritdoc/>
    public uint EntityId => this.IsLoaded ? CSPlayerState.Instance()->EntityId : default;

    /// <inheritdoc/>
    public ulong ContentId => this.IsLoaded ? CSPlayerState.Instance()->ContentId : default;

    /// <inheritdoc/>
    public RowRef<World> CurrentWorld
    {
        get
        {
            var agentLobby = AgentLobby.Instance();
            return agentLobby->IsLoggedIn
                ? LuminaUtils.CreateRef<World>(agentLobby->LobbyData.CurrentWorldId)
                : default;
        }
    }

    /// <inheritdoc/>
    public RowRef<World> HomeWorld
    {
        get
        {
            var agentLobby = AgentLobby.Instance();
            return agentLobby->IsLoggedIn
                ? LuminaUtils.CreateRef<World>(agentLobby->LobbyData.HomeWorldId)
                : default;
        }
    }

    /// <inheritdoc/>
    public Sex Sex => this.IsLoaded ? (Sex)CSPlayerState.Instance()->Sex : default;

    /// <inheritdoc/>
    public RowRef<Race> Race => this.IsLoaded ? LuminaUtils.CreateRef<Race>(CSPlayerState.Instance()->Race) : default;

    /// <inheritdoc/>
    public RowRef<Tribe> Tribe => this.IsLoaded ? LuminaUtils.CreateRef<Tribe>(CSPlayerState.Instance()->Tribe) : default;

    /// <inheritdoc/>
    public RowRef<ClassJob> ClassJob => this.IsLoaded ? LuminaUtils.CreateRef<ClassJob>(CSPlayerState.Instance()->CurrentClassJobId) : default;

    /// <inheritdoc/>
    public short Level => this.IsLoaded ? CSPlayerState.Instance()->CurrentLevel : default;

    /// <inheritdoc/>
    public bool IsLevelSynced => this.IsLoaded && CSPlayerState.Instance()->IsLevelSynced;

    /// <inheritdoc/>
    public short EffectiveLevel => this.IsLoaded ? (this.IsLevelSynced ? CSPlayerState.Instance()->SyncedLevel : CSPlayerState.Instance()->CurrentLevel) : default;

    /// <inheritdoc/>
    public RowRef<GuardianDeity> GuardianDeity => this.IsLoaded ? LuminaUtils.CreateRef<GuardianDeity>(CSPlayerState.Instance()->GuardianDeity) : default;

    /// <inheritdoc/>
    public byte BirthMonth => this.IsLoaded ? CSPlayerState.Instance()->BirthMonth : default;

    /// <inheritdoc/>
    public byte BirthDay => this.IsLoaded ? CSPlayerState.Instance()->BirthDay : default;

    /// <inheritdoc/>
    public RowRef<ClassJob> FirstClass => this.IsLoaded ? LuminaUtils.CreateRef<ClassJob>(CSPlayerState.Instance()->FirstClass) : default;

    /// <inheritdoc/>
    public RowRef<Town> StartTown => this.IsLoaded ? LuminaUtils.CreateRef<Town>(CSPlayerState.Instance()->StartTown) : default;

    /// <inheritdoc/>
    public int BaseStrength => this.IsLoaded ? CSPlayerState.Instance()->BaseStrength : default;

    /// <inheritdoc/>
    public int BaseDexterity => this.IsLoaded ? CSPlayerState.Instance()->BaseDexterity : default;

    /// <inheritdoc/>
    public int BaseVitality => this.IsLoaded ? CSPlayerState.Instance()->BaseVitality : default;

    /// <inheritdoc/>
    public int BaseIntelligence => this.IsLoaded ? CSPlayerState.Instance()->BaseIntelligence : default;

    /// <inheritdoc/>
    public int BaseMind => this.IsLoaded ? CSPlayerState.Instance()->BaseMind : default;

    /// <inheritdoc/>
    public int BasePiety => this.IsLoaded ? CSPlayerState.Instance()->BasePiety : default;

    /// <inheritdoc/>
    public RowRef<GrandCompany> GrandCompany => this.IsLoaded ? LuminaUtils.CreateRef<GrandCompany>(CSPlayerState.Instance()->GrandCompany) : default;

    /// <inheritdoc/>
    public RowRef<Aetheryte> HomeAetheryte => this.IsLoaded ? LuminaUtils.CreateRef<Aetheryte>(CSPlayerState.Instance()->HomeAetheryteId) : default;

    /// <inheritdoc/>
    public IReadOnlyList<RowRef<Aetheryte>> FavoriteAetherytes
    {
        get
        {
            var playerState = CSPlayerState.Instance();
            if (!playerState->IsLoaded)
                return default;

            var count = playerState->FavouriteAetheryteCount;
            if (count == 0)
                return default;

            var array = new RowRef<Aetheryte>[count];

            for (var i = 0; i < count; i++)
                array[i] = LuminaUtils.CreateRef<Aetheryte>(playerState->FavouriteAetherytes[i]);

            return array;
        }
    }

    /// <inheritdoc/>
    public RowRef<Aetheryte> FreeAetheryte => this.IsLoaded ? LuminaUtils.CreateRef<Aetheryte>(CSPlayerState.Instance()->FreeAetheryteId) : default;

    /// <inheritdoc/>
    public uint BaseRestedExperience => this.IsLoaded ? CSPlayerState.Instance()->BaseRestedExperience : default;

    /// <inheritdoc/>
    public short PlayerCommendations => this.IsLoaded ? CSPlayerState.Instance()->PlayerCommendations : default;

    /// <inheritdoc/>
    public byte DeliveryLevel => this.IsLoaded ? CSPlayerState.Instance()->DeliveryLevel : default;

    /// <inheritdoc/>
    public MentorVersion MentorVersion => this.IsLoaded ? (MentorVersion)CSPlayerState.Instance()->MentorVersion : default;

    /// <inheritdoc/>
    public bool IsMentor => this.IsLoaded && CSPlayerState.Instance()->IsMentor();

    /// <inheritdoc/>
    public bool IsBattleMentor => this.IsLoaded && CSPlayerState.Instance()->IsBattleMentor();

    /// <inheritdoc/>
    public bool IsTradeMentor => this.IsLoaded && CSPlayerState.Instance()->IsTradeMentor();

    /// <inheritdoc/>
    public bool IsNovice => this.IsLoaded && CSPlayerState.Instance()->IsNovice();

    /// <inheritdoc/>
    public bool IsReturner => this.IsLoaded && CSPlayerState.Instance()->IsReturner();

    /// <inheritdoc/>
    public int GetAttribute(PlayerAttribute attribute) => this.IsLoaded ? CSPlayerState.Instance()->Attributes[(int)attribute] : default;

    /// <inheritdoc/>
    public byte GetGrandCompanyRank(GrandCompany grandCompany)
    {
        if (!this.IsLoaded)
            return default;

        return grandCompany.RowId switch
        {
            1 => CSPlayerState.Instance()->GCRankMaelstrom,
            2 => CSPlayerState.Instance()->GCRankTwinAdders,
            3 => CSPlayerState.Instance()->GCRankImmortalFlames,
            _ => default,
        };
    }

    /// <inheritdoc/>
    public short GetClassJobLevel(ClassJob classJob)
    {
        if (classJob.ExpArrayIndex == -1)
            return default;

        if (!this.IsLoaded)
            return default;

        return CSPlayerState.Instance()->ClassJobLevels[classJob.ExpArrayIndex];
    }

    /// <inheritdoc/>
    public int GetClassJobExperience(ClassJob classJob)
    {
        if (classJob.ExpArrayIndex == -1)
            return default;

        if (!this.IsLoaded)
            return default;

        return CSPlayerState.Instance()->ClassJobExperience[classJob.ExpArrayIndex];
    }

    /// <inheritdoc/>
    public float GetDesynthesisLevel(ClassJob classJob)
    {
        if (classJob.DohDolJobIndex == -1)
            return default;

        if (!this.IsLoaded)
            return default;

        return CSPlayerState.Instance()->DesynthesisLevels[classJob.DohDolJobIndex] / 100f;
    }
}

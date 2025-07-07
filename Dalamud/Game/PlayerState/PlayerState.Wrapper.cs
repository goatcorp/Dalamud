using Dalamud.Data;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using CSPlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;
using GrandCompany = Lumina.Excel.Sheets.GrandCompany;

namespace Dalamud.Game.PlayerState;

/// <summary>
/// This class contains the PlayerState wrappers.
/// </summary>
internal unsafe partial class PlayerState : IInternalDisposableService, IPlayerState
{
    /// <summary>
    /// Gets a value indicating whether the local character is loaded.
    /// </summary>
    /// <remarks>
    /// This is equivalent with being logged in.<br/>
    /// The actual GameObject will not immediately exist when this changes to true.
    /// </remarks>
    public bool IsLoaded => CSPlayerState.Instance()->IsLoaded == 1;

    /// <summary>
    /// Gets the name of the local character.
    /// </summary>
    public string CharacterName => this.IsLoaded ? CSPlayerState.Instance()->CharacterNameString : string.Empty;

    /// <summary>
    /// Gets the entity ID of the local character.
    /// </summary>
    public uint EntityId => this.IsLoaded ? CSPlayerState.Instance()->EntityId : default;

    /// <summary>
    /// Gets the content ID of the local character.
    /// </summary>
    public ulong ContentId => this.IsLoaded ? CSPlayerState.Instance()->ContentId : default;

    /// <summary>
    /// Gets the World row for the local character's current world.
    /// </summary>
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

    /// <summary>
    /// Gets the World row for the local character's home world.
    /// </summary>
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

    /// <summary>
    /// Gets the sex of the local character.
    /// </summary>
    public Sex Sex => this.IsLoaded ? (Sex)CSPlayerState.Instance()->Sex : default;

    /// <summary>
    /// Gets the Race row for the local character.
    /// </summary>
    public RowRef<Race> Race => this.IsLoaded ? LuminaUtils.CreateRef<Race>(CSPlayerState.Instance()->Race) : default;

    /// <summary>
    /// Gets the Tribe row for the local character.
    /// </summary>
    public RowRef<Tribe> Tribe => this.IsLoaded ? LuminaUtils.CreateRef<Tribe>(CSPlayerState.Instance()->Tribe) : default;

    /// <summary>
    /// Gets the ClassJob row for the local character's current class/job.
    /// </summary>
    public RowRef<ClassJob> ClassJob => this.IsLoaded ? LuminaUtils.CreateRef<ClassJob>(CSPlayerState.Instance()->CurrentClassJobId) : default;

    /// <summary>
    /// Gets the current class/job's level of the local character.
    /// </summary>
    public short Level => this.IsLoaded ? CSPlayerState.Instance()->CurrentLevel : default;

    /// <summary>
    /// Gets a value indicating whether the local character's level is synced.
    /// </summary>
    public bool IsLevelSynced => this.IsLoaded && CSPlayerState.Instance()->IsLevelSynced == 1;

    /// <summary>
    /// Gets the effective level of the local character.
    /// </summary>
    public short EffectiveLevel => this.IsLoaded ? (this.IsLevelSynced ? CSPlayerState.Instance()->SyncedLevel : CSPlayerState.Instance()->CurrentLevel) : default;

    /// <summary>
    /// Gets the GuardianDeity row for the local character.
    /// </summary>
    public RowRef<GuardianDeity> GuardianDeity => this.IsLoaded ? LuminaUtils.CreateRef<GuardianDeity>(CSPlayerState.Instance()->GuardianDeity) : default;

    /// <summary>
    /// Gets the birth month of the local character.
    /// </summary>
    public byte BirthMonth => this.IsLoaded ? CSPlayerState.Instance()->BirthMonth : default;

    /// <summary>
    /// Gets the birth day of the local character.
    /// </summary>
    public byte BirthDay => this.IsLoaded ? CSPlayerState.Instance()->BirthDay : default;

    /// <summary>
    /// Gets the ClassJob row for the local character's starting class.
    /// </summary>
    public RowRef<ClassJob> FirstClass => this.IsLoaded ? LuminaUtils.CreateRef<ClassJob>(CSPlayerState.Instance()->FirstClass) : default;

    /// <summary>
    /// Gets the Town row for the local character's starting town.
    /// </summary>
    public RowRef<Town> StartTown => this.IsLoaded ? LuminaUtils.CreateRef<Town>(CSPlayerState.Instance()->StartTown) : default;

    /// <summary>
    /// Gets the base strength of the local character.
    /// </summary>
    public int BaseStrength => this.IsLoaded ? CSPlayerState.Instance()->BaseStrength : default;

    /// <summary>
    /// Gets the base dexterity of the local character.
    /// </summary>
    public int BaseDexterity => this.IsLoaded ? CSPlayerState.Instance()->BaseDexterity : default;

    /// <summary>
    /// Gets the base vitality of the local character.
    /// </summary>
    public int BaseVitality => this.IsLoaded ? CSPlayerState.Instance()->BaseVitality : default;

    /// <summary>
    /// Gets the base intelligence of the local character.
    /// </summary>
    public int BaseIntelligence => this.IsLoaded ? CSPlayerState.Instance()->BaseIntelligence : default;

    /// <summary>
    /// Gets the base mind of the local character.
    /// </summary>
    public int BaseMind => this.IsLoaded ? CSPlayerState.Instance()->BaseMind : default;

    /// <summary>
    /// Gets the piety mind of the local character.
    /// </summary>
    public int BasePiety => this.IsLoaded ? CSPlayerState.Instance()->BasePiety : default;

    /// <summary>
    /// Gets the GrandCompany row for the local character's current Grand Company affiliation.
    /// </summary>
    public RowRef<GrandCompany> GrandCompany => this.IsLoaded ? LuminaUtils.CreateRef<GrandCompany>(CSPlayerState.Instance()->GrandCompany) : default;

    /// <summary>
    /// Gets the Aetheryte row for the local character's home aetheryte.
    /// </summary>
    public RowRef<Aetheryte> HomeAetheryte => this.IsLoaded ? LuminaUtils.CreateRef<Aetheryte>(CSPlayerState.Instance()->HomeAetheryteId) : default;

    /// <summary>
    /// Gets a span of Aetheryte rows for the local character's favourite aetherytes.
    /// </summary>
    public ReadOnlySpan<RowRef<Aetheryte>> FavouriteAetherytes
    {
        get
        {
            var playerState = CSPlayerState.Instance();
            if (playerState->IsLoaded != 1 || playerState->FavouriteAetheryteCount == 0)
                return [];

            var count = playerState->FavouriteAetheryteCount;
            var array = new RowRef<Aetheryte>[count];

            for (var i = 0; i < count; i++)
                array[i] = LuminaUtils.CreateRef<Aetheryte>(playerState->FavouriteAetherytes[i]);

            return array;
        }
    }

    /// <summary>
    /// Gets the Aetheryte row for the local character's free aetheryte.
    /// </summary>
    public RowRef<Aetheryte> FreeAetheryte => this.IsLoaded ? LuminaUtils.CreateRef<Aetheryte>(CSPlayerState.Instance()->FreeAetheryteId) : default;

    /// <summary>
    /// Gets the amount of received player commendations of the local character.
    /// </summary>
    public uint BaseRestedExperience => this.IsLoaded ? CSPlayerState.Instance()->BaseRestedExperience : default;

    /// <summary>
    /// Gets the amount of received player commendations of the local character.
    /// </summary>
    public short PlayerCommendations => this.IsLoaded ? CSPlayerState.Instance()->PlayerCommendations : default;

    /// <summary>
    /// Gets the Carrier Level of Delivery Moogle Quests of the local character.
    /// </summary>
    public byte DeliveryLevel => this.IsLoaded ? CSPlayerState.Instance()->DeliveryLevel : default;

    /// <summary>
    /// Gets the mentor version of the local character.
    /// </summary>
    public MentorVersion MentorVersion => this.IsLoaded ? (MentorVersion)CSPlayerState.Instance()->MentorVersion : default;

    /// <summary>
    /// Gets the value of an attribute of the local character.
    /// </summary>
    /// <param name="attribute">The attribute to check.</param>
    /// <returns>The value of the specific attribute.</returns>
    public int GetAttribute(PlayerAttribute attribute) => this.IsLoaded ? CSPlayerState.Instance()->Attributes[(int)attribute] : default;

    /// <summary>
    /// Gets the Grand Company rank of the local character.
    /// </summary>
    /// <param name="grandCompany">The Grand Company to check.</param>
    /// <returns>The Grand Company rank of the local character.</returns>
    public byte GetGrandCompanyRank(GrandCompany grandCompany)
    {
        var playerState = CSPlayerState.Instance();
        if (playerState->IsLoaded != 1)
            return default;

        return grandCompany.RowId switch
        {
            1 => playerState->GCRankMaelstrom,
            2 => playerState->GCRankTwinAdders,
            3 => playerState->GCRankImmortalFlames,
            _ => default,
        };
    }

    /// <summary>
    /// Gets the level of the local character's class/job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The level of the requested class/job.</returns>
    public short GetClassJobLevel(ClassJob classJob) => this.IsLoaded ? CSPlayerState.Instance()->ClassJobLevels[classJob.ExpArrayIndex] : default;

    /// <summary>
    /// Gets the experience of the local character's class/job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The experience of the requested class/job.</returns>
    public int GetClassJobExperience(ClassJob classJob)
    {
        var playerState = CSPlayerState.Instance();
        if (playerState->IsLoaded != 1)
            return default;

        return playerState->ClassJobExperience[classJob.ExpArrayIndex];
    }

    /// <summary>
    /// Gets the desynthesis level of the local character's crafter job.
    /// </summary>
    /// <param name="classJob">The ClassJob row to check.</param>
    /// <returns>The desynthesis level of the requested crafter job.</returns>
    public float GetDesynthesisLevel(ClassJob classJob)
    {
        if (classJob.ExpArrayIndex == -1)
            return default;

        var playerState = CSPlayerState.Instance();
        if (playerState->IsLoaded != 1)
            return default;

        return playerState->DesynthesisLevels[classJob.DohDolJobIndex] / 100f;
    }
}

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
}

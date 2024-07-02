using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Possible status icons that are able to show as icon on the Nameplate.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum NameplateStatusIcons
{
    /// <summary>
    /// The status for a player that is disconnecting.
    /// </summary>
    Disconnecting = 061503,

    /// <summary>
    /// The status for a player that is in a duty.
    /// </summary>
    InDuty = 061506,

    /// <summary>
    /// The status for a player that is viewing a cutscene.
    /// </summary>
    ViewingCutscene = 061508,

    /// <summary>
    /// The status for a player that is marked as busy.
    /// </summary>
    Busy = 061509,

    /// <summary>
    /// The status for a player that is afk.
    /// </summary>
    Idle = 061511,

    /// <summary>
    /// The status for a player that is registred for a duty and searching a group.
    /// </summary>
    DutyFinder = 061517,

    /// <summary>
    /// The status for a player that is leader of a party.
    /// </summary>
    PartyLeader = 061521,

    /// <summary>
    /// The status for a player that is member of a party.
    /// </summary>
    PartyMember = 061522,

    /// <summary>
    /// The status for a player that is marked as role-play.
    /// </summary>
    RolePlaying = 061545,

    /// <summary>
    /// The status for a player that makes nice photos.
    /// </summary>
    GroupPose = 061546,

    /// <summary>
    /// The status for new players.
    /// </summary>
    NewAdventurer = 061523,

    /// <summary>
    /// The status for mentors.
    /// </summary>
    Mentor = 061540,

    /// <summary>
    /// The status for PvE mentors.
    /// </summary>
    MentorPvE = 061542,

    /// <summary>
    /// The status for crafting mentors.
    /// </summary>
    MentorCrafting = 061543,

    /// <summary>
    /// The status for PvP mentors.
    /// </summary>
    MentorPvP = 061544,

    /// <summary>
    /// The status for a player that recently took a break playing FFXIV.
    /// </summary>
    Returner = 061547,
}

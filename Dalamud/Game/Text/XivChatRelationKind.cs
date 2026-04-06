namespace Dalamud.Game.Text;

/// <summary>
/// Specifies the relationship for entities involved in a chat log event (e.g., Source or Target).<br/>
/// Used primarily for parsing and coloring chat log messages.
/// </summary>
public enum XivChatRelationKind : byte
{
    /// <summary>No specific relation or unknown entity.</summary>
    None = 0,

    /// <summary>The player currently controlled by the local client.</summary>
    LocalPlayer = 1,

    /// <summary>A player in the same 4-man or 8-man party as the local player.</summary>
    PartyMember = 2,

    /// <summary>A player in the same alliance raid.</summary>
    AllianceMember = 3,

    /// <summary>A player not in the local player's party or alliance.</summary>
    OtherPlayer = 4,

    /// <summary>An enemy entity that is currently in combat with the player or party.</summary>
    EngagedEnemy = 5,

    /// <summary>An enemy entity that is not yet in combat or claimed.</summary>
    UnengagedEnemy = 6,

    /// <summary>An NPC that is friendly or neutral to the player (e.g., EventNPCs).</summary>
    FriendlyNpc = 7,

    /// <summary>A pet (Summoner/Scholar) or companion (Chocobo) belonging to the local player.</summary>
    PetOrCompanion = 8,

    /// <summary>A pet or companion belonging to a member of the local player's party.</summary>
    PetOrCompanionParty = 9,

    /// <summary>A pet or companion belonging to a member of the alliance.</summary>
    PetOrCompanionAlliance = 10,

    /// <summary>A pet or companion belonging to a player not in the party or alliance.</summary>
    PetOrCompanionOther = 11,
}

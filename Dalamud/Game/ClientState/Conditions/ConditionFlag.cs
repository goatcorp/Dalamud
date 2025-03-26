namespace Dalamud.Game.ClientState.Conditions;

/// <summary>
/// Possible state flags (or conditions as they're called internally) that can be set on the local client.
///
/// These come from LogMessage (somewhere) and directly map to each state field managed by the client. As of 5.25, it maps to
/// LogMessage row 7700 and onwards, which can be checked by looking at the Condition sheet and looking at what column 2 maps to.
/// </summary>
public enum ConditionFlag
{
    /// <summary>
    /// Unused.
    /// </summary>
    None = 0,

    /// <summary>
    /// Unable to execute command under normal conditions.
    /// </summary>
    NormalConditions = 1,

    /// <summary>
    /// Unable to execute command while unconscious.
    /// </summary>
    Unconscious = 2,

    /// <summary>
    /// Unable to execute command during an emote.
    /// </summary>
    Emoting = 3,

    /// <summary>
    /// Unable to execute command while mounted.
    /// </summary>
    Mounted = 4,

    /// <summary>
    /// Unable to execute command while crafting.
    /// </summary>
    Crafting = 5,

    /// <summary>
    /// Unable to execute command while gathering.
    /// </summary>
    Gathering = 6,

    /// <summary>
    /// Unable to execute command while melding materia.
    /// </summary>
    MeldingMateria = 7,

    /// <summary>
    /// Unable to execute command while operating a siege machine.
    /// </summary>
    OperatingSiegeMachine = 8,

    /// <summary>
    /// Unable to execute command while carrying an object.
    /// </summary>
    CarryingObject = 9,

    /// <summary>
    /// Unable to execute command while mounted.
    /// </summary>
    Mounted2 = 10,

    /// <summary>
    /// Unable to execute command while in that position.
    /// </summary>
    InThatPosition = 11,

    /// <summary>
    /// Unable to execute command while chocobo racing.
    /// </summary>
    ChocoboRacing = 12,

    /// <summary>
    /// Unable to execute command while playing a mini-game.
    /// </summary>
    PlayingMiniGame = 13,

    /// <summary>
    /// Unable to execute command while playing Lord of Verminion.
    /// </summary>
    PlayingLordOfVerminion = 14,

    /// <summary>
    /// Unable to execute command while participating in a custom match.
    /// </summary>
    ParticipatingInCustomMatch = 15,

    /// <summary>
    /// Unable to execute command while performing.
    /// </summary>
    Performing = 16,

    // Unknown17 = 17,
    // Unknown18 = 18,
    // Unknown19 = 19,
    // Unknown20 = 20,
    // Unknown21 = 21,
    // Unknown22 = 22,
    // Unknown23 = 23,
    // Unknown24 = 24,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    Occupied = 25,

    /// <summary>
    /// Unable to execute command during combat.
    /// </summary>
    InCombat = 26,

    /// <summary>
    /// Unable to execute command while casting.
    /// </summary>
    Casting = 27,

    /// <summary>
    /// Unable to execute command while suffering status affliction.
    /// </summary>
    SufferingStatusAffliction = 28,

    /// <summary>
    /// Unable to execute command while suffering status affliction.
    /// </summary>
    SufferingStatusAffliction2 = 29,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    Occupied30 = 30,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    // todo: not sure if this is used for other event states/???
    OccupiedInEvent = 31,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    OccupiedInQuestEvent = 32,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    Occupied33 = 33,

    /// <summary>
    /// Unable to execute command while bound by duty.
    /// </summary>
    BoundByDuty = 34,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    OccupiedInCutSceneEvent = 35,

    /// <summary>
    /// Unable to execute command while in a dueling area.
    /// </summary>
    InDuelingArea = 36,

    /// <summary>
    /// Unable to execute command while a trade is open.
    /// </summary>
    TradeOpen = 37,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    Occupied38 = 38,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    Occupied39 = 39,

    /// <summary>
    /// Unable to execute command while crafting.
    /// </summary>
    Crafting40 = 40,

    /// <summary>
    /// Unable to execute command while preparing to craft.
    /// </summary>
    PreparingToCraft = 41,

    /// <summary>
    /// Unable to execute command while gathering.
    /// </summary>
    Gathering42 = 42,

    /// <summary>
    /// Unable to execute command while fishing.
    /// </summary>
    Fishing = 43,

    // Unknown44 = 44,

    /// <summary>
    /// Unable to execute command while between areas.
    /// </summary>
    BetweenAreas = 45,

    /// <summary>
    /// Unable to execute command while stealthed.
    /// </summary>
    Stealthed = 46,

    // Unknown47 = 47,

    /// <summary>
    /// Unable to execute command while jumping.
    /// </summary>
    Jumping = 48,

    /// <summary>
    /// Unable to execute command while auto-run is active.
    /// </summary>
    AutorunActive = 49,

    /// <summary>
    /// Unable to execute command while occupied.
    /// </summary>
    // todo: used for other shits?
    OccupiedSummoningBell = 50,

    /// <summary>
    /// Unable to execute command while between areas.
    /// </summary>
    BetweenAreas51 = 51,

    /// <summary>
    /// Unable to execute command due to system error.
    /// </summary>
    SystemError = 52,

    /// <summary>
    /// Unable to execute command while logging out.
    /// </summary>
    LoggingOut = 53,

    /// <summary>
    /// Unable to execute command at this location.
    /// </summary>
    ConditionLocation = 54,

    /// <summary>
    /// Unable to execute command while waiting for duty.
    /// </summary>
    WaitingForDuty = 55,

    /// <summary>
    /// Unable to execute command while bound by duty.
    /// </summary>
    BoundByDuty56 = 56,

    /// <summary>
    /// Unable to execute command at this time.
    /// </summary>
    Unknown57 = 57,

    /// <summary>
    /// Unable to execute command while watching a cutscene.
    /// </summary>
    WatchingCutscene = 58,

    /// <summary>
    /// Unable to execute command while waiting for Duty Finder.
    /// </summary>
    WaitingForDutyFinder = 59,

    /// <summary>
    /// Unable to execute command while creating a character.
    /// </summary>
    CreatingCharacter = 60,

    /// <summary>
    /// Unable to execute command while jumping.
    /// </summary>
    Jumping61 = 61,

    /// <summary>
    /// Unable to execute command while the PvP display is active.
    /// </summary>
    PvPDisplayActive = 62,

    /// <summary>
    /// Unable to execute command while suffering status affliction.
    /// </summary>
    SufferingStatusAffliction63 = 63,

    /// <summary>
    /// Unable to execute command while mounting.
    /// </summary>
    Mounting = 64,

    /// <summary>
    /// Unable to execute command while carrying an item.
    /// </summary>
    CarryingItem = 65,

    /// <summary>
    /// Unable to execute command while using the Party Finder.
    /// </summary>
    UsingPartyFinder = 66,

    /// <summary>
    /// Unable to execute command while using housing functions.
    /// </summary>
    UsingHousingFunctions = 67,

    /// <summary>
    /// Unable to execute command while transformed.
    /// </summary>
    Transformed = 68,

    /// <summary>
    /// Unable to execute command while on the free trial.
    /// </summary>
    OnFreeTrial = 69,

    /// <summary>
    /// Unable to execute command while being moved.
    /// </summary>
    BeingMoved = 70,

    /// <summary>
    /// Unable to execute command while mounting.
    /// </summary>
    Mounting71 = 71,

    /// <summary>
    /// Unable to execute command while suffering status affliction.
    /// </summary>
    SufferingStatusAffliction72 = 72,

    /// <summary>
    /// Unable to execute command while suffering status affliction.
    /// </summary>
    SufferingStatusAffliction73 = 73,

    /// <summary>
    /// Unable to execute command while registering for a race or match.
    /// </summary>
    RegisteringForRaceOrMatch = 74,

    /// <summary>
    /// Unable to execute command while waiting for a race or match.
    /// </summary>
    WaitingForRaceOrMatch = 75,

    /// <summary>
    /// Unable to execute command while waiting for a Triple Triad match.
    /// </summary>
    WaitingForTripleTriadMatch = 76,

    /// <summary>
    /// Unable to execute command while in flight.
    /// </summary>
    InFlight = 77,

    /// <summary>
    /// Unable to execute command while watching a cutscene.
    /// </summary>
    WatchingCutscene78 = 78,

    /// <summary>
    /// Unable to execute command while delving into a deep dungeon.
    /// </summary>
    InDeepDungeon = 79,

    /// <summary>
    /// Unable to execute command while swimming.
    /// </summary>
    Swimming = 80,

    /// <summary>
    /// Unable to execute command while diving.
    /// </summary>
    Diving = 81,

    /// <summary>
    /// Unable to execute command while registering for a Triple Triad match.
    /// </summary>
    RegisteringForTripleTriadMatch = 82,

    /// <summary>
    /// Unable to execute command while waiting for a Triple Triad match.
    /// </summary>
    WaitingForTripleTriadMatch83 = 83,

    /// <summary>
    /// Unable to execute command while participating in a cross-world party or alliance.
    /// </summary>
    ParticipatingInCrossWorldPartyOrAlliance = 84,

    // Unknown85 = 85,

    /// <summary>
    /// Unable to execute command while playing duty record.
    /// </summary>
    DutyRecorderPlayback = 86,

    /// <summary>
    /// Unable to execute command while casting.
    /// </summary>
    Casting87 = 87,

    /// <summary>
    /// Unable to execute command in this state.
    /// </summary>
    InThisState88 = 88,

    /// <summary>
    /// Unable to execute command in this state.
    /// </summary>
    InThisState89 = 89,

    /// <summary>
    /// Unable to execute command while role-playing.
    /// </summary>
    RolePlaying = 90,

    /// <summary>
    /// Unable to execute command while bound by duty.
    /// </summary>
    [Obsolete("Use InDutyQueue")]
    BoundToDuty97 = 91,
    
    /// <summary>
    /// Unable to execute command while bound by duty.
    /// Specifically triggered when you are in a queue for a duty but not inside a duty.
    /// </summary>
    InDutyQueue = 91,

    /// <summary>
    /// Unable to execute command while readying to visit another World.
    /// </summary>
    ReadyingVisitOtherWorld = 92,

    /// <summary>
    /// Unable to execute command while waiting to visit another World.
    /// </summary>
    WaitingToVisitOtherWorld = 93,

    /// <summary>
    /// Unable to execute command while using a parasol.
    /// </summary>
    UsingParasol = 94,

    /// <summary>
    /// Unable to execute command while bound by duty.
    /// </summary>
    BoundByDuty95 = 95,

    /// <summary>
    /// Cannot execute at this time.
    /// </summary>
    Unknown96 = 96,

    /// <summary>
    /// Unable to execute command while wearing a guise.
    /// </summary>
    Disguised = 97,

    /// <summary>
    /// Unable to execute command while recruiting for a non-cross-world party.
    /// </summary>
    RecruitingWorldOnly = 98,

    /// <summary>
    /// Command unavailable in this location.
    /// </summary>
    Unknown99 = 99,

    /// <summary>
    /// Unable to execute command while editing a portrait.
    /// </summary>
    EditingPortrait = 100,
}

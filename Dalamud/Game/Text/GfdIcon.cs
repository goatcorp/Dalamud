namespace Dalamud.Game.Text;

/// <summary>Enumeration of icons defined in the gfdata.gfd file.</summary>
public enum GfdIcon
{
    /// <summary>The up dpad button.</summary>
    DpadUp = 1,

    /// <summary>The down dpad button.</summary>
    DpadDown = 2,

    /// <summary>The left dpad button.</summary>
    DpadLeft = 3,

    /// <summary>The right dpad button.</summary>
    DpadRight = 4,

    /// <summary>The up and down dpad buttons.</summary>
    DpadUpDown = 5,

    /// <summary>The left and right dpad buttons.</summary>
    DpadLeftRight = 6,

    /// <summary>All dpad buttons.</summary>
    DpadAll = 7,

    /// <summary>The B button in Xbox gamepad layout.</summary>
    XboxB = 8,

    /// <summary>The A button in Xbox gamepad layout.</summary>
    XboxA = 9,

    /// <summary>The X button in Xbox gamepad layout.</summary>
    XboxX = 10,

    /// <summary>The Y button in Xbox gamepad layout.</summary>
    XboxY = 11,

    /// <summary>The Circle button in DualShock gamepad layout.</summary>
    DualShockCircle = XboxB,

    /// <summary>The Cross button in DualShock gamepad layout.</summary>
    DualShockCross = XboxA,

    /// <summary>The Square button in DualShock gamepad layout.</summary>
    DualShockSquare = XboxX,

    /// <summary>The Triangle button in DualShock gamepad layout.</summary>
    DualShockTriangle = XboxY,

    /// <summary>The left bumper in Xbox gamepad layout.</summary>
    LBumper = 12,

    /// <summary>The right bumper in Xbox gamepad layout.</summary>
    RBumper = 13,

    /// <summary>The left trigger in Xbox gamepad layout.</summary>
    LTrigger = 14,

    /// <summary>The right trigger in Xbox gamepad layout. </summary>
    RTrigger = 15,

    /// <summary>The left thumb click in Xbox gamepad layout.</summary>
    LThumbClick = 16,

    /// <summary>The right thumb click in Xbox gamepad layout.</summary>
    RThumbClick = 17,

    /// <summary>The L1 button in DualShock gamepad layout.</summary>
    L1 = LBumper,

    /// <summary>The R1 button in DualShock gamepad layout.</summary>
    R1 = RBumper,

    /// <summary>The L2 button in DualShock gamepad layout.</summary>
    L2 = LTrigger,

    /// <summary>The R2 button in DualShock gamepad layout.</summary>
    R2 = RTrigger,

    /// <summary>The L3 button in DualShock gamepad layout.</summary>
    L3 = LThumbClick,

    /// <summary>The R3 button in DualShock gamepad layout.</summary>
    R3 = RThumbClick,

    /// <summary>The Start button in Xbox 360 gamepad layout.</summary>
    Xbox360Start = 18,

    /// <summary>The Back button in Xbox 360 gamepad layout.</summary>
    Xbox360Back = 19,

    /// <summary>The Start button in Xbox Series X|S gamepad layout.</summary>
    XboxXsMenu = Xbox360Start,

    /// <summary>The View button in Xbox Series X|S gamepad layout.</summary>
    XboxXsView = Xbox360Back,

    /// <summary>The Start button in DualShock gamepad layout.</summary>
    DualShockStart = Xbox360Start,

    /// <summary>The Select button in DualShock gamepad layout.</summary>
    DualShockSelect = Xbox360Back,

    /// <summary>The Options button in DualShock with Touchpad gamepad layout.</summary>
    DualShockWithTouchpadOptions = Xbox360Start,

    /// <summary>The Pad button in DualShock with Touchpad gamepad layout.</summary>
    DualShockWithTouchpadPadButton = Xbox360Back,

    /// <summary>The left stick on a gamepad.</summary>
    LStick = 20,

    /// <summary>Vertical movement of the left stick on a gamepad.</summary>
    LStickVertical = 21,

    /// <summary>Horizontal movement of the left stick on a gamepad.</summary>
    LStickHorizontal = 22,

    /// <summary>The right stick on a gamepad.</summary>
    RStick = 23,

    /// <summary>Vertical movement of the right stick on a gamepad.</summary>
    RStickVertical = 24,

    /// <summary>Horizontal movement of the right stick on a gamepad.</summary>
    RStickHorizontal = 25,

    /// <summary>Icon for La Noscea used in Teleport dialog.</summary>
    LaNoscea = 51,

    /// <summary>Icon for The Black Shroud used in Teleport dialog.</summary>
    TheBlackShroud = 52,

    /// <summary>Icon for Thanalan used in Teleport dialog.</summary>
    Thanalan = 53,

    /// <summary>Icon marking the beginning of an auto-translated phrase.</summary>
    AutoTranslateBegin = 54,

    /// <summary>Icon marking the end of an auto-translated phrase.</summary>
    AutoTranslateEnd = 55,

    /// <summary>Icon indicating that something is fire aspected.</summary>
    FireAspected = 56,

    /// <summary>Icon indicating that something is ice aspected.</summary>
    IceAspected = 57,

    /// <summary>Icon indicating that something is wind aspected.</summary>
    WindAspected = 58,

    /// <summary>Icon indicating that something is earth aspected.</summary>
    EarthAspected = 59,

    /// <summary>Icon indicating that something is thunder aspected.</summary>
    ThunderAspected = 60,

    /// <summary>Icon indicating that something is water aspected.</summary>
    WaterAspected = 61,

    /// <summary>Icon indicating level sync in progress.</summary>
    LevelSync = 62,

    /// <summary>Icon indicating that there is a problem.</summary>
    Problem = 63,

    /// <summary>Icon for Ishgard and Surrounding Areas and Coerthas used in Teleport dialog.</summary>
    Coerthas = 64,

    /// <summary>Icon for generic destinations used in Teleport dialog.</summary>
    Aetheryte = 65,

    /// <summary>Icon for residential areas used in Teleport dialog.</summary>
    MiniAetheryte = 66,

    /// <summary>Icon for Free Teleport Destination in Teleport dialog.</summary>
    FreeDestination = 67,

    /// <summary>Icon for Favorite Teleport Destination used in Teleport dialog.</summary>
    FavoriteDestination = 68,

    /// <summary>An empty icon.</summary>
    Empty = 69,

    /// <summary>A green bullet marking that there is an update, such as on fellowships.</summary>
    UpdateBullet = 70,

    /// <summary>An icon depicting a sword facing left up, indicating that the player's weapon is drawn.</summary>
    WeaponDrawn = 71,

    /// <summary>An icon depicting a sword facing left down, indicating that the player's weapon is sheathed.</summary>
    WeaponSheathed = 72,

    /// <summary>A dice icon, used for /random command.</summary>
    Dice = 73,

    /// <summary>An icon indicating that the player is complete with aether current attunements.</summary>
    AetherCurrentsAttunementComplete = 74,

    /// <summary>An icon indicating that the player is incomplete with aether current attunements.</summary>
    AetherCurrentsAttunementIncomplete = 75,

    /// <summary>A denied icon that looks like Target to Ignore 1.</summary>
    Denied = 76,

    /// <summary>An icon depicting a sproute, indicating that a player is a new player.</summary>
    Sprout = 77,

    /// <summary>An icon depicting a crown, indicating that a player is a mentor.</summary>
    Mentor = 78,

    /// <summary>An icon depicting a crown with a sword, indicating that a player is a DoW/M mentor.</summary>
    MentorDoWDoM = 79,

    /// <summary>An icon depicting a crown with a hammer, indicating that a player is a DoH mentor.</summary>
    MentorDoH = 80,

    /// <summary>An icon depicting a crown with a cyan flag, indicating that a player is a PvP mentor.</summary>
    MentorPvP = 81,

    /// <summary>An icon depicting a blue shield, indicating the role of tank.</summary>
    Tank = 82,

    /// <summary>An icon depicting a green cross, indicating the role of healer.</summary>
    Healer = 83,

    /// <summary>An icon depicting a red sword, indicating the role of DPS.</summary>
    Dps = 84,

    /// <summary>An icon depicting a purple anvil and ruler, indicating the role of crafter.</summary>
    Crafter = 85,

    /// <summary>An icon depicting a yellow fish and leaf, indicating the role of gatherer.</summary>
    Gatherer = 86,

    /// <summary>An icon depicting a grey torso, indicating a generic role.</summary>
    GenericRole = 87,

    /// <summary>An icon depicting connected circles, indicating that a player is from other world(server).</summary>
    CrossWorld = 88,

    /// <summary>An icon indicating a FATE where the objective is to stop the adds from respawning.</summary>
    FateMobRush = 89,

    /// <summary>An icon indicating a FATE where the objective is to take the select targets down.</summary>
    FateBoss = 90,

    /// <summary>An icon indicating a FATE where the objective is to gather and submit items to a NPC.</summary>
    FateItemSubmit = 91,

    /// <summary>An icon indicating a FATE where the objective is to protect a stationary NPC.</summary>
    FateGuardStationary = 92,

    /// <summary>An icon indicating a FATE where the objective is to protect a mobile NPC.</summary>
    FateGuardMobile = 93,

    /// <summary>An icon indicating a FATE where idk.</summary>
    FateWithShamshir = 94, // TODO: what's this?

    /// <summary>An icon depicting a crown on sprouts, indicating that a player is a returner.</summary>
    Returner = 95,

    /// <summary>Icon for The Far East, Hingashi, and Kugane used in Teleport dialog.</summary>
    Hingashi = 96,

    /// <summary>Icon for Gyr Abania used in Teleport dialog.</summary>
    GyrAbania = 97,

    /// <summary>An icon indicating a FATE where idk.</summary>
    FateWithTail = 98, // TODO: what's this?

    /// <summary>An icon indicating that an experience points bonus is in effect.</summary>
    ExpBonus = 99,

    /// <summary>An icon depicting seven colorful orbs around an exclamation mark.</summary>
    ExclamationMarkWithSevenOrbs = 100, // TODO: what's this?

    /// <summary>An icon depicting an exclamation mark on a card.</summary>
    ExclamationMarkOnCard = 101, // TODO: what's this?

    /// <summary>An icon indicating a Notorious Monster is up.</summary>
    NotoriousMonster = 102,

    /// <summary>An icon indicating that duty record is in progress.</summary>
    DutyRecorder = 103,

    /// <summary>An icon indicating that an alarm has been set.</summary>
    Alarm = 104,

    /// <summary>An icon indicating that something is placed above the player, on a map.</summary>
    RelativeLocationUp = 105,

    /// <summary>An icon indicating that something is placed below the player, on a map.</summary>
    RelativeLocationDown = 106,

    /// <summary>Icon for Crystarium used in Teleport dialog.</summary>
    TheCrystarium = 107,

    /// <summary>An icon depicting an exclamation mark on a yellow triangle on a crown.</summary>
    MentorNotWorthy = 108,

    /// <summary>An icon indicating a FATE where idk.</summary>
    FateWithMushrooms = 109, // TODO: what's this?

    /// <summary>An icon depicting a rhombus covering another rhombus.</summary>
    NestedRhombus = 110, // TODO: what's this?

    /// <summary>An icon indicating a FATE where idk.</summary>
    FateCrafting = 111, // TODO: what's this?

    /// <summary>An icon indicating that something's about the game's anniversary.</summary>
    GameAnniversary = 112,

    /// <summary>Icon for The Northern Empty used in Teleport dialog.</summary>
    TheNorthernEmpty = 113,

    /// <summary>Icon for Radz-at-Han and Thavnair used in Teleport dialog.</summary>
    Thavnair = 114,

    /// <summary>Icon for Garlemald used in Teleport dialog.</summary>
    Garlemald = 115,

    /// <summary>An icon indicating an island.</summary>
    IslandSantuary = 116, // TODO: what's this?

    /// <summary>An icon indicating that something is physical.</summary>
    Physical = 117,

    /// <summary>An icon indicating that something is magical.</summary>
    Magical = 118,

    /// <summary>An icon indicating that something is poisonous.</summary>
    Poisonous = 119, // TODO: what's this?

    /// <summary>An icon depicting a gold star with a warning icon.</summary>
    GoldStarWithWarning = 120, // TODO: what's this?

    /// <summary>An icon depicting a blue star.</summary>
    BlueStar = 121, // TODO: what's this?

    /// <summary>An icon depicting a blue star with a warning icon.</summary>
    BlueStarWithWarning = 122, // TODO: what's this?

    /// <summary>Another empty icon.</summary>
    Empty2 = 123,

    /// <summary>An icon indicating that a player's network connection with the servers is unstable.</summary>
    UnstableConnection = 124,

    /// <summary>An icon indicating that a player is busy.</summary>
    Busy = 125,

    /// <summary>An icon depicting a gold ARR icon.</summary>
    GoldArrIcon = 126,

    /// <summary>An icon indicating that a player is up for role playing.</summary>
    RolePlaying = 127,

    /// <summary>A gladiator icon.</summary>
    Gladiator = 128,

    /// <summary>A pugilist icon.</summary>
    Pugilist = 129,

    /// <summary>A marauder icon.</summary>
    Marauder = 130,

    /// <summary>A lancer icon.</summary>
    Lancer = 131,

    /// <summary>An archer icon.</summary>
    Archer = 132,

    /// <summary>A conjurer icon.</summary>
    Conjurer = 133,

    /// <summary>A thaumaturge icon.</summary>
    Thaumaturge = 134,

    /// <summary>A carpenter icon.</summary>
    Carpenter = 135,

    /// <summary>A blacksmith icon.</summary>
    Blacksmith = 136,

    /// <summary>An armorer icon.</summary>
    Armorer = 137,

    /// <summary>A goldsmith icon.</summary>
    Goldsmith = 138,

    /// <summary>A leatherworker icon.</summary>
    Leatherworker = 139,

    /// <summary>A weaver icon.</summary>
    Weaver = 140,

    /// <summary>An alchemist icon.</summary>
    Alchemist = 141,

    /// <summary>A culinarian icon.</summary>
    Culinarian = 142,

    /// <summary>A miner icon.</summary>
    Miner = 143,

    /// <summary>A botanist icon.</summary>
    Botanist = 144,

    /// <summary>A fisher icon.</summary>
    Fisher = 145,

    /// <summary>A paladin icon.</summary>
    Paladin = 146,

    /// <summary>A monk icon.</summary>
    Monk = 147,

    /// <summary>A warrior icon.</summary>
    Warrior = 148,

    /// <summary>A dragoon icon.</summary>
    Dragoon = 149,

    /// <summary>A bard icon.</summary>
    Bard = 150,

    /// <summary>A white mage icon.</summary>
    WhiteMage = 151,

    /// <summary>A black mage icon.</summary>
    BlackMage = 152,

    /// <summary>An arcanist icon.</summary>
    Arcanist = 153,

    /// <summary>A summoner icon.</summary>
    Summoner = 154,

    /// <summary>A scholar icon.</summary>
    Scholar = 155,

    /// <summary>A rogue icon.</summary>
    Rogue = 156,

    /// <summary>A ninja icon.</summary>
    Ninja = 157,

    /// <summary>A machinist icon.</summary>
    Machinist = 158,

    /// <summary>A dark knight icon.</summary>
    DarkKnight = 159,

    /// <summary>An astrologian icon.</summary>
    Astrologian = 160,

    /// <summary>A samurai icon.</summary>
    Samurai = 161,

    /// <summary>A red mage icon.</summary>
    RedMage = 162,

    /// <summary>A blue mage icon.</summary>
    BlueMage = 163,

    /// <summary>A gunbreaker icon.</summary>
    Gunbreaker = 164,

    /// <summary>A dancer icon.</summary>
    Dancer = 165,

    /// <summary>A reaper icon.</summary>
    Reaper = 166,

    /// <summary>A sage icon.</summary>
    Sage = 167,

    /// <summary>An icon indicating that a player is currently in queue for something.</summary>
    WaitingForDutyFinder = 168,
}

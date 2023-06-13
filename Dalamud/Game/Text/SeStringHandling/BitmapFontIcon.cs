namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// This class represents special icons that can appear in chat naturally or as IconPayloads.
/// </summary>
public enum BitmapFontIcon : uint
{
    /// <summary>
    /// No icon.
    /// </summary>
    None = 0,

    /// <summary>
    /// The controller D-pad up icon.
    /// </summary>
    ControllerDPadUp = 1,

    /// <summary>
    /// The controller D-pad down icon.
    /// </summary>
    ControllerDPadDown = 2,

    /// <summary>
    /// The controller D-pad left icon.
    /// </summary>
    ControllerDPadLeft = 3,

    /// <summary>
    /// The controller D-pad right icon.
    /// </summary>
    ControllerDPadRight = 4,

    /// <summary>
    /// The controller D-pad up/down icon.
    /// </summary>
    ControllerDPadUpDown = 5,

    /// <summary>
    /// The controller D-pad left/right icon.
    /// </summary>
    ControllerDPadLeftRight = 6,

    /// <summary>
    /// The controller D-pad all directions icon.
    /// </summary>
    ControllerDPadAll = 7,

    /// <summary>
    /// The controller button 0 icon (Xbox: B, PlayStation: Circle).
    /// </summary>
    ControllerButton0 = 8,

    /// <summary>
    /// The controller button 1 icon (XBox: A, PlayStation: Cross).
    /// </summary>
    ControllerButton1 = 9,

    /// <summary>
    /// The controller button 2 icon (XBox: X, PlayStation: Square).
    /// </summary>
    ControllerButton2 = 10,

    /// <summary>
    /// The controller button 3 icon (BBox: Y, PlayStation: Triangle).
    /// </summary>
    ControllerButton3 = 11,

    /// <summary>
    /// The controller left shoulder button icon.
    /// </summary>
    ControllerShoulderLeft = 12,

    /// <summary>
    /// The controller right shoulder button icon.
    /// </summary>
    ControllerShoulderRight = 13,

    /// <summary>
    /// The controller left trigger button icon.
    /// </summary>
    ControllerTriggerLeft = 14,

    /// <summary>
    /// The controller right trigger button icon.
    /// </summary>
    ControllerTriggerRight = 15,

    /// <summary>
    /// The controller left analog stick in icon.
    /// </summary>
    ControllerAnalogLeftStickIn = 16,

    /// <summary>
    /// The controller right analog stick in icon.
    /// </summary>
    ControllerAnalogRightStickIn = 17,

    /// <summary>
    /// The controller start button icon.
    /// </summary>
    ControllerStart = 18,

    /// <summary>
    /// The controller back button icon.
    /// </summary>
    ControllerBack = 19,

    /// <summary>
    /// The controller left analog stick icon.
    /// </summary>
    ControllerAnalogLeftStick = 20,

    /// <summary>
    /// The controller left analog stick up/down icon.
    /// </summary>
    ControllerAnalogLeftStickUpDown = 21,

    /// <summary>
    /// The controller left analog stick left/right icon.
    /// </summary>
    ControllerAnalogLeftStickLeftRight = 22,

    /// <summary>
    /// The controller right analog stick icon.
    /// </summary>
    ControllerAnalogRightStick = 23,

    /// <summary>
    /// The controller right analog stick up/down icon.
    /// </summary>
    ControllerAnalogRightStickUpDown = 24,

    /// <summary>
    /// The controller right analog stick left/right icon.
    /// </summary>
    ControllerAnalogRightStickLeftRight = 25,

    /// <summary>
    /// The La Noscea region icon.
    /// </summary>
    LaNoscea = 51,

    /// <summary>
    /// The Black Shroud region icon.
    /// </summary>
    BlackShroud = 52,

    /// <summary>
    /// The Thanalan region icon.
    /// </summary>
    Thanalan = 53,

    /// <summary>
    /// The auto translate begin icon.
    /// </summary>
    AutoTranslateBegin = 54,

    /// <summary>
    /// The auto translate end icon.
    /// </summary>
    AutoTranslateEnd = 55,

    /// <summary>
    /// The fire element icon.
    /// </summary>
    ElementFire = 56,

    /// <summary>
    /// The ice element icon.
    /// </summary>
    ElementIce = 57,

    /// <summary>
    /// The wind element icon.
    /// </summary>
    ElementWind = 58,

    /// <summary>
    /// The earth element icon.
    /// </summary>
    ElementEarth = 59,

    /// <summary>
    /// The lightning element icon.
    /// </summary>
    ElementLightning = 60,

    /// <summary>
    /// The water element icon.
    /// </summary>
    ElementWater = 61,

    /// <summary>
    /// The level sync icon.
    /// </summary>
    LevelSync = 62,

    /// <summary>
    /// The warning icon.
    /// </summary>
    Warning = 63,

    /// <summary>
    /// The Ishgard region icon.
    /// </summary>
    Ishgard = 64,

    /// <summary>
    /// The Aetheryte icon.
    /// </summary>
    Aetheryte = 65,

    /// <summary>
    /// The Aethernet icon.
    /// </summary>
    Aethernet = 66,

    /// <summary>
    /// The gold star icon.
    /// </summary>
    GoldStar = 67,

    /// <summary>
    /// The silver star icon.
    /// </summary>
    SilverStar = 68,

    /// <summary>
    /// The green dot icon.
    /// </summary>
    GreenDot = 70,

    /// <summary>
    /// The unsheathed sword icon.
    /// </summary>
    SwordUnsheathed = 71,

    /// <summary>
    /// The sheathed sword icon.
    /// </summary>
    SwordSheathed = 72,

    /// <summary>
    /// The dice icon.
    /// </summary>
    Dice = 73,

    /// <summary>
    /// The flyable zone icon.
    /// </summary>
    FlyZone = 74,

    /// <summary>
    /// The no-flying zone icon.
    /// </summary>
    FlyZoneLocked = 75,

    /// <summary>
    /// The no-circle/prohibited icon.
    /// </summary>
    NoCircle = 76,

    /// <summary>
    /// The sprout icon.
    /// </summary>
    NewAdventurer = 77,

    /// <summary>
    /// The mentor icon.
    /// </summary>
    Mentor = 78,

    /// <summary>
    /// The PvE mentor icon.
    /// </summary>
    MentorPvE = 79,

    /// <summary>
    /// The crafting mentor icon.
    /// </summary>
    MentorCrafting = 80,

    /// <summary>
    /// The PvP mentor icon.
    /// </summary>
    MentorPvP = 81,

    /// <summary>
    /// The tank role icon.
    /// </summary>
    Tank = 82,

    /// <summary>
    /// The healer role icon.
    /// </summary>
    Healer = 83,

    /// <summary>
    /// The DPS role icon.
    /// </summary>
    DPS = 84,

    /// <summary>
    /// The crafter role icon.
    /// </summary>
    Crafter = 85,

    /// <summary>
    /// The gatherer role icon.
    /// </summary>
    Gatherer = 86,

    /// <summary>
    /// The "any" role icon.
    /// </summary>
    AnyClass = 87,

    /// <summary>
    /// The cross-world icon.
    /// </summary>
    CrossWorld = 88,

    /// <summary>
    /// The slay type Fate icon.
    /// </summary>
    FateSlay = 89,

    /// <summary>
    /// The boss type Fate icon.
    /// </summary>
    FateBoss = 90,

    /// <summary>
    /// The gather type Fate icon.
    /// </summary>
    FateGather = 91,

    /// <summary>
    /// The defend type Fate icon.
    /// </summary>
    FateDefend = 92,

    /// <summary>
    /// The escort type Fate icon.
    /// </summary>
    FateEscort = 93,

    /// <summary>
    /// The special type 1 Fate icon.
    /// </summary>
    FateSpecial1 = 94,

    /// <summary>
    /// The returner icon.
    /// </summary>
    Returner = 95,

    /// <summary>
    /// The Far-East region icon.
    /// </summary>
    FarEast = 96,

    /// <summary>
    /// The Gyr Albania region icon.
    /// </summary>
    GyrAbania = 97,

    /// <summary>
    /// The special type 2 Fate icon.
    /// </summary>
    FateSpecial2 = 98,

    /// <summary>
    /// The priority world icon.
    /// </summary>
    PriorityWorld = 99,

    /// <summary>
    /// The elemental level icon.
    /// </summary>
    ElementalLevel = 100,

    /// <summary>
    /// The exclamation rectangle icon.
    /// </summary>
    ExclamationRectangle = 101,

    /// <summary>
    /// The notorious monster icon.
    /// </summary>
    NotoriousMonster = 102,

    /// <summary>
    /// The recording icon.
    /// </summary>
    Recording = 103,

    /// <summary>
    /// The alarm icon.
    /// </summary>
    Alarm = 104,

    /// <summary>
    /// The arrow up icon.
    /// </summary>
    ArrowUp = 105,

    /// <summary>
    /// The arrow down icon.
    /// </summary>
    ArrowDown = 106,

    /// <summary>
    /// The Crystarium region icon.
    /// </summary>
    Crystarium = 107,

    /// <summary>
    /// The mentor problem icon.
    /// </summary>
    MentorProblem = 108,

    /// <summary>
    /// The unknown gold type Fate icon.
    /// </summary>
    FateUnknownGold = 109,

    /// <summary>
    /// The orange diamond icon.
    /// </summary>
    OrangeDiamond = 110,

    /// <summary>
    /// The crafting type Fate icon.
    /// </summary>
    FateCrafting = 111,

    /// <summary>
    /// The Fan Festival logo.
    /// </summary>
    FanFestival = 112,

    /// <summary>
    /// The Sharlayan region icon.
    /// </summary>
    Sharlayan = 113,

    /// <summary>
    /// The Ilsabard region icon.
    /// </summary>
    Ilsabard = 114,

    /// <summary>
    /// The Garlemald region icon.
    /// </summary>
    Garlemald = 115,

    /// <summary>
    /// The Island Sanctuary icon.
    /// </summary>
    IslandSanctuary = 116,
    
    /// <summary>
    /// The Physical Damage icon.
    /// </summary>
    DamagePhysical = 117,

    /// <summary>
    /// The Magical Damage icon.
    /// </summary>
    DamageMagical = 118,

    /// <summary>
    /// The Special Damage icon.
    /// </summary>
    DamageSpecial = 119,

    /// <summary>
    /// A gold star icon with an orange exclamation mark.
    /// </summary>
    GoldStarProblem = 120,

    /// <summary>
    /// A blue star icon.
    /// </summary>
    BlueStar = 121,

    /// <summary>
    /// A blue star icon with an orange exclamation mark.
    /// </summary>
    BlueStarProblem = 121,

    /// <summary>
    /// The Disconnecting icon.
    /// </summary>
    Disconnecting = 124,

    /// <summary>
    /// The Do Not Disturb icon.
    /// </summary>
    DoNotDisturb = 125,

    /// <summary>
    /// The Meteor icon.
    /// </summary>
    Meteor = 126,

    /// <summary>
    /// The Role Playing icon.
    /// </summary>
    RolePlaying = 127,

    /// <summary>
    /// The Gladiator icon.
    /// </summary>
    Gladiator = 128,

    /// <summary>
    /// The Pugilist icon.
    /// </summary>
    Pugilist = 129,

    /// <summary>
    /// The Marauder icon.
    /// </summary>
    Marauder = 130,

    /// <summary>
    /// The Lancer icon.
    /// </summary>
    Lancer = 131,

    /// <summary>
    /// The Archer icon.
    /// </summary>
    Archer = 132,

    /// <summary>
    /// The Conjurer icon.
    /// </summary>
    Conjurer = 133,

    /// <summary>
    /// The Thaumaturge icon.
    /// </summary>
    Thaumaturge = 134,

    /// <summary>
    /// The Carpenter icon.
    /// </summary>
    Carpenter = 135,

    /// <summary>
    /// The Blacksmith icon.
    /// </summary>
    Blacksmith = 136,

    /// <summary>
    /// The Armorer icon.
    /// </summary>
    Armorer = 137,

    /// <summary>
    /// The Goldsmith icon.
    /// </summary>
    Goldsmith = 138,

    /// <summary>
    /// The Leatherworker icon.
    /// </summary>
    Leatherworker = 139,

    /// <summary>
    /// The Weaver icon.
    /// </summary>
    Weaver = 140,

    /// <summary>
    /// The Alchemist icon.
    /// </summary>
    Alchemist = 131,

    /// <summary>
    /// The Culinarian icon.
    /// </summary>
    Culinarian = 132,

    /// <summary>
    /// The Miner icon.
    /// </summary>
    Miner = 143,

    /// <summary>
    /// The Botanist icon.
    /// </summary>
    Botanist = 144,

    /// <summary>
    /// The Fisher icon.
    /// </summary>
    Fisher = 145,

    /// <summary>
    /// The Paladin icon.
    /// </summary>
    Paladin = 146,

    /// <summary>
    /// The Monk icon.
    /// </summary>
    Monk = 147,

    /// <summary>
    /// The Warrior icon.
    /// </summary>
    Warrior = 148,

    /// <summary>
    /// The Dragoon icon.
    /// </summary>
    Dragoon = 149,

    /// <summary>
    /// The Bard icon.
    /// </summary>
    Bard = 150,

    /// <summary>
    /// The White Mage icon.
    /// </summary>
    WhiteMage = 151,

    /// <summary>
    /// The Black Mage icon.
    /// </summary>
    BlackMage = 152,

    /// <summary>
    /// The Arcanist icon.
    /// </summary>
    Arcanist = 153,

    /// <summary>
    /// The Summoner icon.
    /// </summary>
    Summoner = 154,

    /// <summary>
    /// The Scholar icon.
    /// </summary>
    Scholar = 155,

    /// <summary>
    /// The Rogue icon.
    /// </summary>
    Rogue = 156,

    /// <summary>
    /// The Ninja icon.
    /// </summary>
    Ninja = 157,

    /// <summary>
    /// The Machinist icon.
    /// </summary>
    Machinist = 158,

    /// <summary>
    /// The Dark Knight icon.
    /// </summary>
    DarkKnight = 159,

    /// <summary>
    /// The Astrologian icon.
    /// </summary>
    Astrologian = 160,

    /// <summary>
    /// The Samurai icon.
    /// </summary>
    Samurai = 161,

    /// <summary>
    /// The Red Mage icon.
    /// </summary>
    RedMage = 162,

    /// <summary>
    /// The Blue Mage icon.
    /// </summary>
    BlueMage = 163,

    /// <summary>
    /// The Gunbreaker icon.
    /// </summary>
    Gunbreaker = 164,

    /// <summary>
    /// The Dancer icon.
    /// </summary>
    Dancer = 165,

    /// <summary>
    /// The Reaper icon.
    /// </summary>
    Reaper = 166,

    /// <summary>
    /// The Sage icon.
    /// </summary>
    Sage = 167,

    /// <summary>
    /// The Waiting For Duty Finder icon.
    /// </summary>
    WaitingForDutyFinder = 168,
}

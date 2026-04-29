namespace Dalamud.Game.Gui;

/// <summary>
/// ActionKinds used in AgentActionDetail.
/// These describe the possible kinds of actions being hovered.
/// </summary>
public enum HoverActionKind
{
    /// <summary>
    /// No action is hovered.
    /// </summary>
    None = 0,

    /// <summary>
    /// A regular action is hovered.
    /// </summary>
    Action = 30,

    /// <summary>
    /// A crafting action is hovered.
    /// </summary>
    CraftingAction = 31,

    /// <summary>
    /// A general action is hovered.
    /// </summary>
    GeneralAction = 32,

    /// <summary>
    /// A companion order type of action is hovered.
    /// </summary>
    CompanionOrder = 33, // Game Term: BuddyOrder

    /// <summary>
    /// A main command type of action is hovered.
    /// </summary>
    MainCommand = 34,

    /// <summary>
    /// An extras command type of action is hovered.
    /// </summary>
    ExtraCommand = 35,

    /// <summary>
    /// A companion action is hovered.
    /// </summary>
    Companion = 36,

    /// <summary>
    /// A pet order type of action is hovered.
    /// </summary>
    PetOrder = 37,

    /// <summary>
    /// A trait is hovered.
    /// </summary>
    Trait = 38,

    /// <summary>
    /// A buddy action is hovered.
    /// </summary>
    BuddyAction = 39,

    /// <summary>
    /// A company action is hovered.
    /// </summary>
    CompanyAction = 40,

    /// <summary>
    /// A mount is hovered.
    /// </summary>
    Mount = 41,

    /// <summary>
    /// A chocobo race action is hovered.
    /// </summary>
    ChocoboRaceAction = 42,

    /// <summary>
    /// A chocobo race item is hovered.
    /// </summary>
    ChocoboRaceItem = 43,

    /// <summary>
    /// A deep dungeon equipment is hovered.
    /// </summary>
    DeepDungeonEquipment = 44,

    /// <summary>
    /// A deep dungeon equipment 2 is hovered.
    /// </summary>
    DeepDungeonEquipment2 = 45,

    /// <summary>
    /// A deep dungeon item is hovered.
    /// </summary>
    DeepDungeonItem = 46,

    /// <summary>
    /// A quick chat is hovered.
    /// </summary>
    QuickChat = 47,

    /// <summary>
    /// An action combo route is hovered.
    /// </summary>
    ActionComboRoute = 48,

    /// <summary>
    /// A pvp trait is hovered.
    /// </summary>
    PvPSelectTrait = 49,

    /// <summary>
    /// A squadron action is hovered.
    /// </summary>
    BgcArmyAction = 50,

    /// <summary>
    /// A perform action is hovered.
    /// </summary>
    Perform = 51,

    /// <summary>
    /// A deep dungeon magic stone is hovered.
    /// </summary>
    DeepDungeonMagicStone = 52,

    /// <summary>
    /// A deep dungeon demiclone is hovered.
    /// </summary>
    DeepDungeonDemiclone = 53,

    DeepDungeon4GimmickEffect = 54,

    /// <summary>
    /// An eureka magia action is hovered.
    /// </summary>
    EurekaMagiaAction = 55,

    /// <summary>
    /// An island sanctuary temporary item is hovered.
    /// </summary>
    MYCTemporaryItem = 56,

    /// <summary>
    /// An ornament is hovered.
    /// </summary>
    Ornament = 57,

    /// <summary>
    /// Glasses are hovered.
    /// </summary>
    Glasses = 58,

    PhantomAction = 59,

    /// <summary>
    /// Phantom Job Trait is hovered.
    /// </summary>
    MKDTrait = 60,
}

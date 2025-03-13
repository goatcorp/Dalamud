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
    Action = 28,

    /// <summary>
    /// A crafting action is hovered.
    /// </summary>
    CraftingAction = 29,

    /// <summary>
    /// A general action is hovered.
    /// </summary>
    GeneralAction = 30,

    /// <summary>
    /// A companion order type of action is hovered.
    /// </summary>
    CompanionOrder = 31, // Game Term: BuddyOrder

    /// <summary>
    /// A main command type of action is hovered.
    /// </summary>
    MainCommand = 32,

    /// <summary>
    /// An extras command type of action is hovered.
    /// </summary>
    ExtraCommand = 33,

    /// <summary>
    /// A companion action is hovered.
    /// </summary>
    Companion = 34,

    /// <summary>
    /// A pet order type of action is hovered.
    /// </summary>
    PetOrder = 35,

    /// <summary>
    /// A trait is hovered.
    /// </summary>
    Trait = 36,

    /// <summary>
    /// A buddy action is hovered.
    /// </summary>
    BuddyAction = 37,

    /// <summary>
    /// A company action is hovered.
    /// </summary>
    CompanyAction = 38,

    /// <summary>
    /// A mount is hovered.
    /// </summary>
    Mount = 39,

    /// <summary>
    /// A chocobo race action is hovered.
    /// </summary>
    ChocoboRaceAction = 40,

    /// <summary>
    /// A chocobo race item is hovered.
    /// </summary>
    ChocoboRaceItem = 41,

    /// <summary>
    /// A deep dungeon equipment is hovered.
    /// </summary>
    DeepDungeonEquipment = 42,

    /// <summary>
    /// A deep dungeon equipment 2 is hovered.
    /// </summary>
    DeepDungeonEquipment2 = 43,

    /// <summary>
    /// A deep dungeon item is hovered.
    /// </summary>
    DeepDungeonItem = 44,

    /// <summary>
    /// A quick chat is hovered.
    /// </summary>
    QuickChat = 45,

    /// <summary>
    /// An action combo route is hovered.
    /// </summary>
    ActionComboRoute = 46,

    /// <summary>
    /// A pvp trait is hovered.
    /// </summary>
    PvPSelectTrait = 47,

    /// <summary>
    /// A squadron action is hovered.
    /// </summary>
    BgcArmyAction = 48,

    /// <summary>
    /// A perform action is hovered.
    /// </summary>
    Perform = 49,

    /// <summary>
    /// A deep dungeon magic stone is hovered.
    /// </summary>
    DeepDungeonMagicStone = 50,

    /// <summary>
    /// A deep dungeon demiclone is hovered.
    /// </summary>
    DeepDungeonDemiclone = 51,

    /// <summary>
    /// An eureka magia action is hovered.
    /// </summary>
    EurekaMagiaAction = 52,

    /// <summary>
    /// An island sanctuary temporary item is hovered.
    /// </summary>
    MYCTemporaryItem = 53,

    /// <summary>
    /// An ornament is hovered.
    /// </summary>
    Ornament = 54,

    /// <summary>
    /// Glasses are hovered.
    /// </summary>
    Glasses = 55,
}

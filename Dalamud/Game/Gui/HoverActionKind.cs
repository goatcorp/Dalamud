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
    Action = 29,

    /// <summary>
    /// A crafting action is hovered.
    /// </summary>
    CraftingAction = 30,

    /// <summary>
    /// A general action is hovered.
    /// </summary>
    GeneralAction = 31,

    /// <summary>
    /// A companion order type of action is hovered.
    /// </summary>
    CompanionOrder = 32, // Game Term: BuddyOrder

    /// <summary>
    /// A main command type of action is hovered.
    /// </summary>
    MainCommand = 33,

    /// <summary>
    /// An extras command type of action is hovered.
    /// </summary>
    ExtraCommand = 34,

    /// <summary>
    /// A companion action is hovered.
    /// </summary>
    Companion = 35,

    /// <summary>
    /// A pet order type of action is hovered.
    /// </summary>
    PetOrder = 36,

    /// <summary>
    /// A trait is hovered.
    /// </summary>
    Trait = 37,

    /// <summary>
    /// A buddy action is hovered.
    /// </summary>
    BuddyAction = 38,

    /// <summary>
    /// A company action is hovered.
    /// </summary>
    CompanyAction = 39,

    /// <summary>
    /// A mount is hovered.
    /// </summary>
    Mount = 40,

    /// <summary>
    /// A chocobo race action is hovered.
    /// </summary>
    ChocoboRaceAction = 41,

    /// <summary>
    /// A chocobo race item is hovered.
    /// </summary>
    ChocoboRaceItem = 42,

    /// <summary>
    /// A deep dungeon equipment is hovered.
    /// </summary>
    DeepDungeonEquipment = 43,

    /// <summary>
    /// A deep dungeon equipment 2 is hovered.
    /// </summary>
    DeepDungeonEquipment2 = 44,

    /// <summary>
    /// A deep dungeon item is hovered.
    /// </summary>
    DeepDungeonItem = 45,

    /// <summary>
    /// A quick chat is hovered.
    /// </summary>
    QuickChat = 46,

    /// <summary>
    /// An action combo route is hovered.
    /// </summary>
    ActionComboRoute = 47,

    /// <summary>
    /// A pvp trait is hovered.
    /// </summary>
    PvPSelectTrait = 48,

    /// <summary>
    /// A squadron action is hovered.
    /// </summary>
    BgcArmyAction = 49,

    /// <summary>
    /// A perform action is hovered.
    /// </summary>
    Perform = 50,

    /// <summary>
    /// A deep dungeon magic stone is hovered.
    /// </summary>
    DeepDungeonMagicStone = 51,

    /// <summary>
    /// A deep dungeon demiclone is hovered.
    /// </summary>
    DeepDungeonDemiclone = 52,

    /// <summary>
    /// An eureka magia action is hovered.
    /// </summary>
    EurekaMagiaAction = 53,

    /// <summary>
    /// An island sanctuary temporary item is hovered.
    /// </summary>
    MYCTemporaryItem = 54,

    /// <summary>
    /// An ornament is hovered.
    /// </summary>
    Ornament = 55,

    /// <summary>
    /// Glasses are hovered.
    /// </summary>
    Glasses = 56,

    /// <summary>
    /// Phantom Job Trait is hovered.
    /// </summary>
    MKDTrait = 58,
}

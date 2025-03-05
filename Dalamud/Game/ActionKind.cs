namespace Dalamud.Game;

/// <summary>
/// Enum describing possible action kinds.
/// </summary>
public enum ActionKind
{
    /// <summary>
    /// A Trait.
    /// </summary>
    Trait = 0,

    /// <summary>
    /// An Action.
    /// </summary>
    Action = 1,

    /// <summary>
    /// A usable Item.
    /// </summary>
    Item = 2, // does not work?

    /// <summary>
    /// A usable EventItem.
    /// </summary>
    EventItem = 3, // does not work?

    /// <summary>
    /// An EventAction.
    /// </summary>
    EventAction = 4,

    /// <summary>
    /// A GeneralAction.
    /// </summary>
    GeneralAction = 5,

    /// <summary>
    /// A BuddyAction.
    /// </summary>
    BuddyAction = 6,

    /// <summary>
    /// A MainCommand.
    /// </summary>
    MainCommand = 7,

    /// <summary>
    /// A Companion.
    /// </summary>
    Companion = 8, // unresolved?!

    /// <summary>
    /// A CraftAction.
    /// </summary>
    CraftAction = 9,

    /// <summary>
    /// An Action (again).
    /// </summary>
    Action2 = 10, // what's the difference?

    /// <summary>
    /// A PetAction.
    /// </summary>
    PetAction = 11,

    /// <summary>
    /// A CompanyAction.
    /// </summary>
    CompanyAction = 12,

    /// <summary>
    /// A Mount.
    /// </summary>
    Mount = 13,

    // 14-18 unused

    /// <summary>
    /// A BgcArmyAction.
    /// </summary>
    BgcArmyAction = 19,

    /// <summary>
    /// An Ornament.
    /// </summary>
    Ornament = 20,
}

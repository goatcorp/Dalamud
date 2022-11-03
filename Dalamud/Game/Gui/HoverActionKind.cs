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
    Action = 21,

    /// <summary>
    /// A general action is hovered.
    /// </summary>
    GeneralAction = 23,

    /// <summary>
    /// A companion order type of action is hovered.
    /// </summary>
    CompanionOrder = 24,

    /// <summary>
    /// A main command type of action is hovered.
    /// </summary>
    MainCommand = 25,

    /// <summary>
    /// An extras command type of action is hovered.
    /// </summary>
    ExtraCommand = 26,

    /// <summary>
    /// A pet order type of action is hovered.
    /// </summary>
    PetOrder = 28,

    /// <summary>
    /// A trait is hovered.
    /// </summary>
    Trait = 29,
}

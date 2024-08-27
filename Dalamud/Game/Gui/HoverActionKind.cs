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
    /// A general action is hovered.
    /// </summary>
    GeneralAction = 30,

    /// <summary>
    /// A companion order type of action is hovered.
    /// </summary>
    CompanionOrder = 31,

    /// <summary>
    /// A main command type of action is hovered.
    /// </summary>
    MainCommand = 32,

    /// <summary>
    /// An extras command type of action is hovered.
    /// </summary>
    ExtraCommand = 33,

    /// <summary>
    /// A pet order type of action is hovered.
    /// </summary>
    PetOrder = 35,

    /// <summary>
    /// A trait is hovered.
    /// </summary>
    Trait = 36,
}

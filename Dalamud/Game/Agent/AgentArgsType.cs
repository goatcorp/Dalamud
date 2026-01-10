namespace Dalamud.Game.Agent;

/// <summary>
/// Enumeration for available AgentLifecycle arg data.
/// </summary>
public enum AgentArgsType
{
    /// <summary>
    /// Generic arg type that contains no meaningful data.
    /// </summary>
    Generic,

    /// <summary>
    /// Contains argument data for ReceiveEvent.
    /// </summary>
    ReceiveEvent,

    /// <summary>
    /// Contains argument data for GameEvent.
    /// </summary>
    GameEvent,

    /// <summary>
    /// Contains argument data for LevelChange.
    /// </summary>
    LevelChange,

    /// <summary>
    /// Contains argument data for ClassJobChange.
    /// </summary>
    ClassJobChange,
}

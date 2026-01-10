namespace Dalamud.Game.Agent;

/// <summary>
/// Enumeration for available AgentLifecycle events.
/// </summary>
public enum AgentEvent
{
    /// <summary>
    /// An event that is fired before the agent processes its Receive Event Function.
    /// </summary>
    PreReceiveEvent,

    /// <summary>
    /// An event that is fired after the agent has processed its Receive Event Function.
    /// </summary>
    PostReceiveEvent,

    /// <summary>
    /// An event that is fired before the agent processes its Filtered Receive Event Function.
    /// </summary>
    PreReceiveFilteredEvent,

    /// <summary>
    /// An event that is fired after the agent has processed its Filtered Receive Event Function.
    /// </summary>
    PostReceiveFilteredEvent,

    /// <summary>
    /// An event that is fired before the agent processes its Show Function.
    /// </summary>
    PreShow,

    /// <summary>
    /// An event that is fired after the agent has processed its Show Function.
    /// </summary>
    PostShow,

    /// <summary>
    /// An event that is fired before the agent processes its Hide Function.
    /// </summary>
    PreHide,

    /// <summary>
    /// An event that is fired after the agent has processed its Hide Function.
    /// </summary>
    PostHide,

    /// <summary>
    /// An event that is fired before the agent processes its Update Function.
    /// </summary>
    PreUpdate,

    /// <summary>
    /// An event that is fired after the agent has processed its Update Function.
    /// </summary>
    PostUpdate,

    /// <summary>
    /// An event that is fired before the agent processes its Game Event Function.
    /// </summary>
    PreGameEvent,

    /// <summary>
    /// An event that is fired after the agent has processed its Game Event Function.
    /// </summary>
    PostGameEvent,

    /// <summary>
    /// An event that is fired before the agent processes its Game Event Function.
    /// </summary>
    PreLevelChange,

    /// <summary>
    /// An event that is fired after the agent has processed its Level Change Function.
    /// </summary>
    PostLevelChange,

    /// <summary>
    /// An event that is fired before the agent processes its ClassJob Change Function.
    /// </summary>
    PreClassJobChange,

    /// <summary>
    /// An event that is fired after the agent has processed its ClassJob Change Function.
    /// </summary>
    PostClassJobChange,
}

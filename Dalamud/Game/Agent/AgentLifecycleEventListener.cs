using Dalamud.Plugin.Services;

namespace Dalamud.Game.Agent;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates.
/// </summary>
public class AgentLifecycleEventListener
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventListener"/> class.
    /// </summary>
    /// <param name="eventType">Event type to listen for.</param>
    /// <param name="agentId">Agent id to listen for.</param>
    /// <param name="functionDelegate">Delegate to invoke.</param>
    internal AgentLifecycleEventListener(AgentEvent eventType, uint agentId, IAgentLifecycle.AgentEventDelegate functionDelegate)
    {
        this.EventType = eventType;
        this.AgentId = agentId;
        this.FunctionDelegate = functionDelegate;
    }

    /// <summary>
    /// Gets the agentId of the agent this listener is looking for.
    /// uint.MaxValue if it wants to be called for any agent.
    /// </summary>
    public uint AgentId { get; init; }

    /// <summary>
    /// Gets the event type this listener is looking for.
    /// </summary>
    public AgentEvent EventType { get; init; }

    /// <summary>
    /// Gets the delegate this listener invokes.
    /// </summary>
    public IAgentLifecycle.AgentEventDelegate FunctionDelegate { get; init; }
}

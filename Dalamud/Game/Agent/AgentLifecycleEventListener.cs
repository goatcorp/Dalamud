using Dalamud.Plugin.Services;

namespace Dalamud.Game.Agent;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates.
/// </summary>
internal class AgentLifecycleEventListener : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentLifecycleEventListener"/> class.
    /// </summary>
    /// <param name="service">The <see cref="AgentLifecycle"/> service.</param>
    /// <param name="eventType">Event type to listen for.</param>
    /// <param name="agentId">Agent id to listen for.</param>
    /// <param name="functionDelegate">Delegate to invoke.</param>
    internal AgentLifecycleEventListener(AgentLifecycle service, AgentEvent eventType, AgentId agentId, IAgentLifecycle.AgentEventDelegate functionDelegate)
    {
        this.Service = service;
        this.EventType = eventType;
        this.AgentId = agentId;
        this.FunctionDelegate = functionDelegate;
    }

    /// <summary>
    /// Gets the <see cref="AgentLifecycle"/> service.
    /// </summary>
    public AgentLifecycle Service { get; init; }

    /// <summary>
    /// Gets the agentId of the agent this listener is looking for.
    /// uint.MaxValue if it wants to be called for any agent.
    /// </summary>
    public AgentId AgentId { get; init; }

    /// <summary>
    /// Gets the event type this listener is looking for.
    /// </summary>
    public AgentEvent EventType { get; init; }

    /// <summary>
    /// Gets the delegate this listener invokes.
    /// </summary>
    public IAgentLifecycle.AgentEventDelegate FunctionDelegate { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the listener is requested to be cleared.
    /// </summary>
    internal bool IsRequestedToClear { get; set; }

    /// <summary>
    /// Unregisters the event listener from the <see cref="AgentLifecycle"/> service.
    /// </summary>
    public void Dispose()
    {
        if (!this.IsRequestedToClear)
            this.Service.UnregisterListener(this);
    }
}

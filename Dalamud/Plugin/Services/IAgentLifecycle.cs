using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for in-game agent lifecycles.
/// </summary>
public interface IAgentLifecycle : IDalamudService
{
    /// <summary>
    /// Delegate for receiving agent lifecycle event messages.
    /// </summary>
    /// <param name="type">The event type that triggered the message.</param>
    /// <param name="args">Information about what agent triggered the message.</param>
    public delegate void AgentEventDelegate(AgentEvent type, AgentArgs args);

    /// <summary>
    /// Register a listener that will trigger on the specified event and any of the specified agent.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="agentIds">Agent IDs that will trigger the handler to be invoked.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AgentEvent eventType, IEnumerable<AgentId> agentIds, AgentEventDelegate handler);

    /// <summary>
    /// Register a listener that will trigger on the specified event only for the specified agent.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="agentId">The agent ID that will trigger the handler to be invoked.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AgentEvent eventType, AgentId agentId, AgentEventDelegate handler);

    /// <summary>
    /// Register a listener that will trigger on the specified event for any agent.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AgentEvent eventType, AgentEventDelegate handler);

    /// <summary>
    /// Unregister listener from specified event type and specified agent IDs.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and agent IDs will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="agentIds">Agent IDs to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AgentEvent eventType, IEnumerable<AgentId> agentIds, [Optional] AgentEventDelegate handler);

    /// <summary>
    /// Unregister all listeners for the specified event type and agent ID.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and agents will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="agentId">Agent id to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AgentEvent eventType, AgentId agentId, [Optional] AgentEventDelegate handler);

    /// <summary>
    /// Unregister an event type handler.<br/>This will only remove a handler that is added via <see cref="RegisterListener(AgentEvent, AgentEventDelegate)"/>.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and agents will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AgentEvent eventType, [Optional] AgentEventDelegate handler);

    /// <summary>
    /// Unregister all events that use the specified handlers.
    /// </summary>
    /// <param name="handlers">Handlers to remove.</param>
    void UnregisterListener(params AgentEventDelegate[] handlers);

    /// <summary>
    /// Resolves an agents virtual table address back to the original unmodified table address.
    /// </summary>
    /// <param name="virtualTableAddress">The address of a modified agents virtual table.</param>
    /// <returns>The address of the agents original virtual table.</returns>
    nint GetOriginalVirtualTable(nint virtualTableAddress);
}

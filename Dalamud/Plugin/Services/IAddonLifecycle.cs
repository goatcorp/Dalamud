using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
public interface IAddonLifecycle
{
    /// <summary>
    /// Delegate for receiving addon lifecycle event messages.
    /// </summary>
    /// <param name="type">The event type that triggered the message.</param>
    /// <param name="args">Information about what addon triggered the message.</param>
    public delegate void AddonEventDelegate(AddonEvent type, AddonArgs args);
    
    /// <summary>
    /// Register a listener that will trigger on the specified event and any of the specified addons.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="addonNames">Addon names that will trigger the handler to be invoked.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AddonEvent eventType, IEnumerable<string> addonNames, AddonEventDelegate handler);
    
    /// <summary>
    /// Register a listener that will trigger on the specified event only for the specified addon.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="addonName">The addon name that will trigger the handler to be invoked.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AddonEvent eventType, string addonName, AddonEventDelegate handler);
    
    /// <summary>
    /// Register a listener that will trigger on the specified event for any addon.
    /// </summary>
    /// <param name="eventType">Event type to trigger on.</param>
    /// <param name="handler">The handler to invoke.</param>
    void RegisterListener(AddonEvent eventType, AddonEventDelegate handler);
    
    /// <summary>
    /// Unregister listener from specified event type and specified addon names.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and addon names will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="addonNames">Addon names to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AddonEvent eventType, IEnumerable<string> addonNames, [Optional] AddonEventDelegate handler);
    
    /// <summary>
    /// Unregister all listeners for the specified event type and addon name.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and addons will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="addonName">Addon name to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AddonEvent eventType, string addonName, [Optional] AddonEventDelegate handler);
    
    /// <summary>
    /// Unregister an event type handler.<br/>This will only remove a handler that is added via <see cref="RegisterListener(AddonEvent, AddonEventDelegate)"/>.
    /// </summary>
    /// <remarks>
    /// If a specific handler is not provided, all handlers for the event type and addons will be unregistered.
    /// </remarks>
    /// <param name="eventType">Event type to deregister.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    void UnregisterListener(AddonEvent eventType, [Optional] AddonEventDelegate handler);
    
    /// <summary>
    /// Unregister all events that use the specified handlers.
    /// </summary>
    /// <param name="handlers">Handlers to remove.</param>
    void UnregisterListener(params AddonEventDelegate[] handlers);
}

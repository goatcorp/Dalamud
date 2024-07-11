using Dalamud.Plugin.Services;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates.
/// </summary>
internal class AddonLifecycleEventListener
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleEventListener"/> class.
    /// </summary>
    /// <param name="eventType">Event type to listen for.</param>
    /// <param name="addonName">Addon name to listen for.</param>
    /// <param name="functionDelegate">Delegate to invoke.</param>
    internal AddonLifecycleEventListener(AddonEvent eventType, string addonName, IAddonLifecycle.AddonEventDelegate functionDelegate)
    {
        this.EventType = eventType;
        this.AddonName = addonName;
        this.FunctionDelegate = functionDelegate;
    }

    /// <summary>
    /// Gets the name of the addon this listener is looking for.
    /// string.Empty if it wants to be called for any addon.
    /// </summary>
    public string AddonName { get; init; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this event has been unregistered.
    /// </summary>
    public bool Removed { get; set; }
    
    /// <summary>
    /// Gets the event type this listener is looking for.
    /// </summary>
    public AddonEvent EventType { get; init; }
    
    /// <summary>
    /// Gets the delegate this listener invokes.
    /// </summary>
    public IAddonLifecycle.AddonEventDelegate FunctionDelegate { get; init; }
}

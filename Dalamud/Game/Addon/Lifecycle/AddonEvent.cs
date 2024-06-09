namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle events.
/// </summary>
public enum AddonEvent
{
    /// <summary>
    /// Event that is fired before an addon begins it's setup process.
    /// </summary>
    PreSetup,

    /// <summary>
    /// Event that is fired after an addon has completed it's setup process.
    /// </summary>
    PostSetup,

    /// <summary>
    /// Event that is fired before an addon begins update.
    /// </summary>
    PreUpdate,

    /// <summary>
    /// Event that is fired after an addon has completed update.
    /// </summary>
    PostUpdate,

    /// <summary>
    /// Event that is fired before an addon begins draw.
    /// </summary>
    PreDraw,

    /// <summary>
    /// Event that is fired after an addon has completed draw.
    /// </summary>
    PostDraw,

    /// <summary>
    /// Event that is fired before an addon is finalized.
    /// </summary>
    PreFinalize,
    
    /// <summary>
    /// Event that is fired before an addon begins a requested update.
    /// </summary>
    PreRequestedUpdate,
    
    /// <summary>
    /// Event that is fired after an addon finishes a requested update.
    /// </summary>
    PostRequestedUpdate,
    
    /// <summary>
    /// Event that is fired before an addon begins a refresh.
    /// </summary>
    PreRefresh,
    
    /// <summary>
    /// Event that is fired after an addon has finished a refresh.
    /// </summary>
    PostRefresh,
    
    /// <summary>
    /// Event that is fired before an addon begins processing an event.
    /// </summary>
    PreReceiveEvent,
    
    /// <summary>
    /// Event that is fired after an addon has processed an event.
    /// </summary>
    PostReceiveEvent,
    
    /// <summary>
    /// Event that is fired before an addon begins showing.
    /// </summary>
    PreShow,
    
    /// <summary>
    /// Event that is fired after an addon has completed showing.
    /// </summary>
    PostShow,
    
    /// <summary>
    /// Event that is fired before and addon begins hiding.
    /// </summary>
    PreHide,
    
    /// <summary>
    /// Event that is fired after an addon has completed hiding.
    /// </summary>
    PostHide,
}

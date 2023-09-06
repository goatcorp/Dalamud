namespace Dalamud.Game.AddonLifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle events
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
    
    // // Events not implemented yet.
    // /// <summary>
    // /// Event that is fired right before an addon is set to shown.
    // /// </summary>
    // PreShow,
    //
    // /// <summary>
    // /// Event that is fired after an addon has been shown.
    // /// </summary>
    // PostShow,
    //
    // /// <summary>
    // /// Event that is fired right before an addon is set to hidden.
    // /// </summary>
    // PreHide,
    //
    // /// <summary>
    // /// Event that is fired after an addon has been hidden.
    // /// </summary>
    // PostHide,
    //
    // /// <summary>
    // /// Event that is fired before an addon begins update.
    // /// </summary>
    // PreUpdate,
    //
    // /// <summary>
    // /// Event that is fired after an addon has completed update.
    // /// </summary>
    // PostUpdate,
    //
    // /// <summary>
    // /// Event that is fired before an addon begins draw.
    // /// </summary>
    // PreDraw,
    //
    // /// <summary>
    // /// Event that is fired after an addon has completed draw.
    // /// </summary>
    // PostDraw,
    
    /// <summary>
    /// Event that is fired before an addon is finalized.
    /// </summary>
    PreFinalize,
}

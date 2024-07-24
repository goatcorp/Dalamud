namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle events.
/// </summary>
public enum AddonEvent
{
    /// <summary>
    /// Event that is fired before an addon begins its setup process.
    /// </summary>
    /// <remarks>Setup occurs when an addon is constructing its ui node lists, and registering event callbacks.</remarks>
    /// <remarks>PreSetup can be used to modify the AtkValues that are used to set the initial values of the ui nodes.</remarks>
    PreSetup,

    /// <summary>
    /// Event that is fired after an addon has completed its setup process.
    /// </summary>
    /// <remarks>Setup occurs when an addon is constructing its ui node lists, and registering event callbacks.</remarks>
    /// <remarks>PostSetup can be used to read data from an addons AtkValues, or other data in the UI structure,
    /// also useful for adding custom elements to the now initialized node lists.</remarks>
    PostSetup,

    /// <summary>
    /// Event that is fired before an addon begins update.
    /// </summary>
    /// <remarks>Update occurs every frame that an addon is loaded. Regardless of visibility.</remarks>
    PreUpdate,

    /// <summary>
    /// Event that is fired after an addon has completed update.
    /// </summary>
    /// <remarks>Update occurs every frame that an addon is loaded. Regardless of visibility.</remarks>
    PostUpdate,

    /// <summary>
    /// Event that is fired before an addon begins draw.
    /// </summary>
    /// <remarks>Draw occurs every frame that an addon is visible or actively drawing on the screen.</remarks>
    PreDraw,

    /// <summary>
    /// Event that is fired after an addon has completed draw.
    /// </summary>
    /// <remarks>Draw occurs every frame that an addon is visible or actively drawing on the screen.</remarks>
    PostDraw,

    /// <summary>
    /// Event that is fired before an addon is finalized.
    /// </summary>
    /// <remarks>Finalize occurs when an addon is destructing its ui node data, and freeing any allocated memory.</remarks>
    PreFinalize,
    
    /// <summary>
    /// Event that is fired before an addon begins a requested update.
    /// </summary>
    /// <remarks>RequestedUpdate generally occurs when the server sends new data to the client and needs an addon to update to reflect the changed data.</remarks>
    /// <remarks>PreRequestedUpdate can be used to modify the data that the UI will then use to display the newly acquired data.</remarks>
    PreRequestedUpdate,
    
    /// <summary>
    /// Event that is fired after an addon finishes a requested update.
    /// </summary>
    /// <remarks>RequestedUpdate generally occurs when the server sends new data to the client and needs an addon to update to reflect the changed data.</remarks>
    PostRequestedUpdate,
    
    /// <summary>
    /// Event that is fired before an addon begins a refresh.
    /// </summary>
    /// <remarks>Refresh generally occurs in response to a user interaction, such as changing tabs.</remarks>
    /// <remarks>PreRefresh can be used to modify the data that the UI will then use to display its data.</remarks>
    PreRefresh,
    
    /// <summary>
    /// Event that is fired after an addon has finished a refresh.
    /// </summary>
    /// <remarks>Refresh generally occurs in response to a user interaction, such as changing tabs.</remarks>
    PostRefresh,
    
    /// <summary>
    /// Event that is fired before an addon begins processing an event.
    /// </summary>
    /// <remarks>ReceiveEvent occurs from user triggered input events, such as mousing over elements or clicking a button.</remarks>
    /// <remarks><br/><em>Note, this event is only valid for addons that implement custom behavior when interacted with.</em></remarks>
    PreReceiveEvent,
    
    /// <summary>
    /// Event that is fired after an addon has processed an event.
    /// </summary>
    /// <remarks>ReceiveEvent occurs from user triggered input events, such as mousing over elements or clicking a button.</remarks>
    PostReceiveEvent,
}

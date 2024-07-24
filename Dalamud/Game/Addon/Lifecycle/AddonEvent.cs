using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle events.
/// </summary>
public enum AddonEvent
{
    /// <summary>
    /// An event that is fired prior to an addon being setup with its implementation of
    /// <see cref="AtkUnitBase.OnSetup"/>. This event is useful for modifying the initial data contained within
    /// <see cref="AddonSetupArgs.AtkValueSpan"/> prior to the addon being created.
    /// </summary>
    /// <seealso cref="AddonSetupArgs"/>
    PreSetup,
    
    /// <summary>
    /// An event that is fired after an addon has finished its initial setup. This event is particularly useful for
    /// developers seeking to add custom elements to now-initialized and populated node lists, as well as reading data
    /// placed in the AtkValues by the game during the setup process.
    /// See <see cref="PreSetup"/> for more information.
    /// </summary>
    PostSetup,

    /// <summary>
    /// An event that is fired before an addon begins its update cycle via <see cref="AtkUnitBase.Update"/>. This event
    /// is fired every frame that an update is loaded, regardless of visibility.
    /// </summary>
    /// <seealso cref="AddonUpdateArgs"/>
    PreUpdate,

    /// <summary>
    /// An event that is fired after an addon has finished its update.
    /// See <see cref="PreUpdate"/> for more information.
    /// </summary>
    PostUpdate,

    /// <summary>
    /// An event that is fired before an addon begins drawing to screen via <see cref="AtkUnitBase.Draw"/>. Unlike
    /// <see cref="PreUpdate"/>, this event is only fired if an addon is visible or otherwise drawing to screen.
    /// </summary>
    /// <seealso cref="AddonDrawArgs"/>
    PreDraw,

    /// <summary>
    /// An event that is fired after an addon has finished its draw to screen.
    /// See <see cref="PreDraw"/> for more information.
    /// </summary>
    PostDraw,

    /// <summary>
    /// An event that is fired immediately before an addon is finalized via <see cref="AtkUnitBase.Finalize"/> and
    /// destroyed. After this event, the addon will destruct its UI node data as well as free any allocated memory.
    /// This event can be used for cleanup and tracking tasks.
    /// </summary>
    /// <remarks>
    /// This event is <em>NOT</em> fired when the addon is being hidden, but tends to be fired when it's being properly
    /// closed.
    /// <br />
    /// As this is part of the destruction process for an addon, this event does not have an associated Post event.
    /// </remarks>
    /// <seealso cref="AddonFinalizeArgs"/>
    PreFinalize,
    
    /// <summary>
    /// An event that is fired before a call to <see cref="AtkUnitBase.OnRequestedUpdate"/> is made in response to a
    /// change in the subscribed <see cref="AddonRequestedUpdateArgs.NumberArrayData"/> or
    /// <see cref="AddonRequestedUpdateArgs.StringArrayData"/> backing this addon. This generally occurs in response to
    /// receiving data from the game server, but can happen in other cases as well. This event is useful for modifying
    /// the data received before it's passed to the UI for display. Contrast to <see cref="PreRefresh"/> which tends to
    /// be in response to <em>client-driven</em> interactions.
    /// </summary>
    /// <seealso cref="AddonRequestedUpdateArgs"/>
    /// <seealso cref="PostRequestedUpdate"/>
    /// <example>
    /// A developer would use this event to intercept free company information after it's received from the server, but
    /// before it's displayed to the user. This would allow the developer to add user-driven notes or other information
    /// to the Free Company's overview.
    /// </example>
    PreRequestedUpdate,
    
    /// <summary>
    /// An event that is fired after an addon has finished processing an <c>ArrayData</c> update.
    /// See <see cref="PreRequestedUpdate"/> for more information.
    /// </summary>
    PostRequestedUpdate,
    
    /// <summary>
    /// An event that is fired before an addon calls its <see cref="AtkUnitManager.RefreshAddon"/> method. Refreshes are
    /// generally triggered in response to certain user interactions such as changing tabs, and are primarily used to
    /// update the <c>AtkValue</c>s present in this addon. Contrast to <see cref="PreRequestedUpdate"/> which is called
    /// in response to <c>ArrayData</c> updates.</summary>
    /// <seealso cref="AddonRefreshArgs"/>
    /// <seealso cref="PostRefresh"/>
    PreRefresh,
    
    /// <summary>
    /// An event that is fired after an addon has finished its refresh.
    /// See <see cref="PreRefresh"/> for more information.
    /// </summary>
    PostRefresh,
    
    /// <summary>
    /// An event that is fired before an addon begins processing a user-driven event via
    /// <see cref="AtkEventListener.ReceiveEvent"/>, such as mousing over an element or clicking a button. This event
    /// is only valid for addons that actually override the <c>ReceiveEvent</c> method of the underlying
    /// AtkEventListener.
    /// </summary>
    /// <seealso cref="AddonReceiveEventArgs"/>
    /// <seealso cref="PostReceiveEvent"/>
    PreReceiveEvent,
    
    /// <summary>
    /// An event that is fired after an addon finishes calling its <see cref="AtkEventListener.ReceiveEvent"/> method.
    /// See <see cref="PreReceiveEvent"/> for more information.
    /// </summary>
    PostReceiveEvent,
}

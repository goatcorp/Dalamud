using System.Collections.Generic;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates for Addon_OnReceiveEvent.
/// Multiple addons may use the same ReceiveEvent function, this helper makes sure that those addon events are handled properly.
/// </summary>
internal unsafe class AddonLifecycleReceiveEventListener : IDisposable
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleReceiveEventListener"/> class.
    /// </summary>
    /// <param name="service">AddonLifecycle service instance.</param>
    /// <param name="addonName">Initial Addon Requesting this listener.</param>
    /// <param name="receiveEventAddress">Address of Addon's ReceiveEvent function.</param>
    internal AddonLifecycleReceiveEventListener(AddonLifecycle service, string addonName, nint receiveEventAddress)
    {
        this.AddonLifecycle = service;
        this.AddonNames = new List<string> { addonName };
        this.Hook = Hook<AddonReceiveEventDelegate>.FromAddress(receiveEventAddress, this.OnReceiveEvent);
    }

    /// <summary>
    /// Addon Receive Event Function delegate.
    /// </summary>
    /// <param name="addon">Addon Pointer.</param>
    /// <param name="eventType">Event Type.</param>
    /// <param name="eventParam">Unique Event ID.</param>
    /// <param name="atkEvent">Event Data.</param>
    /// <param name="a5">Unknown.</param>
    public delegate void AddonReceiveEventDelegate(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);

    /// <summary>
    /// Gets the list of addons that use this receive event hook.
    /// </summary>
    public List<string> AddonNames { get; init; }
    
    /// <summary>
    /// Gets the address of the registered hook.
    /// </summary>
    public nint HookAddress => this.Hook.Address;
    
    /// <summary>
    /// Gets the contained hook for these addons.
    /// </summary>
    public Hook<AddonReceiveEventDelegate> Hook { get; init; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Hook.Dispose();
    }

    private void OnReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint data)
    {
        try
        {
            this.AddonLifecycle.InvokeListeners(AddonEvent.PreReceiveEvent, new AddonReceiveEventArgs
            {
                Addon = (nint)addon, 
                AtkEventType = (byte)eventType,
                EventParam = eventParam,
                AtkEvent = (nint)atkEvent,
                Data = data,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnReceiveEvent pre-receiveEvent invoke.");
        }
        
        try
        {
            this.Hook.Original(addon, eventType, eventParam, atkEvent, data);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.AddonLifecycle.InvokeListeners(AddonEvent.PostReceiveEvent, new AddonReceiveEventArgs
            {
                Addon = (nint)addon, 
                AtkEventType = (byte)eventType,
                EventParam = eventParam,
                AtkEvent = (nint)atkEvent,
                Data = data,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonRefresh post-receiveEvent invoke.");
        }
    }
}

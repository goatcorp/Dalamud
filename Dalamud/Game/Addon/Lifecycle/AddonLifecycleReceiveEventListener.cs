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

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecyclePooledArgs argsPool = Service<AddonLifecyclePooledArgs>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleReceiveEventListener"/> class.
    /// </summary>
    /// <param name="service">AddonLifecycle service instance.</param>
    /// <param name="addonName">Initial Addon Requesting this listener.</param>
    /// <param name="receiveEventAddress">Address of Addon's ReceiveEvent function.</param>
    internal AddonLifecycleReceiveEventListener(AddonLifecycle service, string addonName, nint receiveEventAddress)
    {
        this.AddonLifecycle = service;
        this.AddonNames = [addonName];
        this.FunctionAddress = receiveEventAddress;
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
    /// Gets the address of the ReceiveEvent function as provided by the vtable on setup.
    /// </summary>
    public nint FunctionAddress { get; init; }
    
    /// <summary>
    /// Gets the contained hook for these addons.
    /// </summary>
    public Hook<AddonReceiveEventDelegate>? Hook { get; private set; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <summary>
    /// Try to hook and enable this receive event handler.
    /// </summary>
    public void TryEnable()
    {
        this.Hook ??= Hook<AddonReceiveEventDelegate>.FromAddress(this.FunctionAddress, this.OnReceiveEvent);
        this.Hook?.Enable();
    }
    
    /// <summary>
    /// Disable the hook for this receive event handler.
    /// </summary>
    public void Disable()
    {
        this.Hook?.Disable();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Hook?.Dispose();
    }

    private void OnReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint data)
    {
        // Check that we didn't get here through a call to another addons handler.
        var addonName = addon->NameString;
        if (!this.AddonNames.Contains(addonName))
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, data);
            return;
        }

        using var returner = this.argsPool.Rent(out AddonReceiveEventArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.AtkEventType = (byte)eventType;
        arg.EventParam = eventParam;
        arg.AtkEvent = (IntPtr)atkEvent;
        arg.Data = data;
        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PreReceiveEvent, arg);
        eventType = (AtkEventType)arg.AtkEventType;
        eventParam = arg.EventParam;
        atkEvent = (AtkEvent*)arg.AtkEvent;
        data = arg.Data;
        
        try
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, data);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PostReceiveEvent, arg);
    }
}

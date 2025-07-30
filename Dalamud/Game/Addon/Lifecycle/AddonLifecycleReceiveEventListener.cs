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
    public Hook<AtkUnitBase.Delegates.ReceiveEvent>? Hook { get; private set; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <summary>
    /// Try to hook and enable this receive event handler.
    /// </summary>
    public void TryEnable()
    {
        this.Hook ??= Hook<AtkUnitBase.Delegates.ReceiveEvent>.FromAddress(this.FunctionAddress, this.OnReceiveEvent);
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

    private void OnReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        // Check that we didn't get here through a call to another addons handler.
        var addonName = addon->NameString;
        if (!this.AddonNames.Contains(addonName))
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
            return;
        }

        using var returner = this.argsPool.Rent(out AddonReceiveEventArgs arg);
        arg.Addon = (nint)addon;
        arg.AtkEventType = (byte)eventType;
        arg.EventParam = eventParam;
        arg.AtkEvent = (IntPtr)atkEvent;
        arg.Data = (nint)atkEventData;
        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PreReceiveEvent, arg);
        eventType = (AtkEventType)arg.AtkEventType;
        eventParam = arg.EventParam;
        atkEvent = (AtkEvent*)arg.AtkEvent;
        atkEventData = (AtkEventData*)arg.Data;
        
        try
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PostReceiveEvent, arg);
    }
}

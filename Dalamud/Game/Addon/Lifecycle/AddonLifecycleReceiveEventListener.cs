using System.Collections.Generic;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates for Addon_OnReceiveEvent.
/// Multiple addons may use the same ReceiveEvent function, this helper makes sure that those addon events are handled properly.
/// </summary>
internal unsafe class AddonLifecycleReceiveEventListener : IDisposable
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    // Note: these can be sourced from ObjectPool of appropriate types instead, but since we don't import that NuGet
    // package, and these events are always called from the main thread, this is fine.
#pragma warning disable CS0618 // Type or member is obsolete
    // TODO: turn constructors of these internal
    private readonly AddonReceiveEventArgs recyclingReceiveEventArgs = new();
#pragma warning restore CS0618 // Type or member is obsolete

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
    public nint HookAddress => this.Hook?.Address ?? nint.Zero;
    
    /// <summary>
    /// Gets the contained hook for these addons.
    /// </summary>
    public Hook<AddonReceiveEventDelegate>? Hook { get; init; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Hook?.Dispose();
    }

    private void OnReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint data)
    {
        // Check that we didn't get here through a call to another addons handler.
        var addonName = MemoryHelper.ReadString((nint)addon->Name, 0x20);
        if (!this.AddonNames.Contains(addonName))
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, data);
            return;
        }

        this.recyclingReceiveEventArgs.AddonInternal = (nint)addon;
        this.recyclingReceiveEventArgs.AtkEventType = (byte)eventType;
        this.recyclingReceiveEventArgs.EventParam = eventParam;
        this.recyclingReceiveEventArgs.AtkEvent = (IntPtr)atkEvent;
        this.recyclingReceiveEventArgs.Data = data;
        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PreReceiveEvent, this.recyclingReceiveEventArgs);
        eventType = (AtkEventType)this.recyclingReceiveEventArgs.AtkEventType;
        eventParam = this.recyclingReceiveEventArgs.EventParam;
        atkEvent = (AtkEvent*)this.recyclingReceiveEventArgs.AtkEvent;
        data = this.recyclingReceiveEventArgs.Data;
        
        try
        {
            this.Hook!.Original(addon, eventType, eventParam, atkEvent, data);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PostReceiveEvent, this.recyclingReceiveEventArgs);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonLifecycle : IInternalDisposableService
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecyclePooledArgs argsPool = Service<AddonLifecyclePooledArgs>.Get();

    private readonly nint disallowedReceiveEventAddress;
    
    private readonly AddonLifecycleAddressResolver address;
    private readonly CallHook<AddonSetupDelegate> onAddonSetupHook;
    private readonly CallHook<AddonSetupDelegate> onAddonSetup2Hook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;
    private readonly CallHook<AddonDrawDelegate> onAddonDrawHook;
    private readonly CallHook<AddonUpdateDelegate> onAddonUpdateHook;
    private readonly Hook<AddonOnRefreshDelegate> onAddonRefreshHook;
    private readonly CallHook<AddonOnRequestedUpdateDelegate> onAddonRequestedUpdateHook;

    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(TargetSigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);

        // We want value of the function pointer at vFunc[2]
        this.disallowedReceiveEventAddress = ((nint*)this.address.AtkEventListener)![2];

        this.onAddonSetupHook = new CallHook<AddonSetupDelegate>(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonSetup2Hook = new CallHook<AddonSetupDelegate>(this.address.AddonSetup2, this.OnAddonSetup);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonFinalize, this.OnAddonFinalize);
        this.onAddonDrawHook = new CallHook<AddonDrawDelegate>(this.address.AddonDraw, this.OnAddonDraw);
        this.onAddonUpdateHook = new CallHook<AddonUpdateDelegate>(this.address.AddonUpdate, this.OnAddonUpdate);
        this.onAddonRefreshHook = Hook<AddonOnRefreshDelegate>.FromAddress(this.address.AddonOnRefresh, this.OnAddonRefresh);
        this.onAddonRequestedUpdateHook = new CallHook<AddonOnRequestedUpdateDelegate>(this.address.AddonOnRequestedUpdate, this.OnRequestedUpdate);

        this.onAddonSetupHook.Enable();
        this.onAddonSetup2Hook.Enable();
        this.onAddonFinalizeHook.Enable();
        this.onAddonDrawHook.Enable();
        this.onAddonUpdateHook.Enable();
        this.onAddonRefreshHook.Enable();
        this.onAddonRequestedUpdateHook.Enable();
    }

    private delegate void AddonSetupDelegate(AtkUnitBase* addon, uint valueCount, AtkValue* values);

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private delegate void AddonDrawDelegate(AtkUnitBase* addon);

    private delegate void AddonUpdateDelegate(AtkUnitBase* addon, float delta);

    private delegate void AddonOnRequestedUpdateDelegate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private delegate byte AddonOnRefreshDelegate(AtkUnitManager* unitManager, AtkUnitBase* addon, uint valueCount, AtkValue* values);
    
    /// <summary>
    /// Gets a list of all AddonLifecycle ReceiveEvent Listener Hooks.
    /// </summary>
    internal List<AddonLifecycleReceiveEventListener> ReceiveEventListeners { get; } = new();
    
    /// <summary>
    /// Gets a list of all AddonLifecycle Event Listeners.
    /// </summary>
    internal List<AddonLifecycleEventListener> EventListeners { get; } = new();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onAddonSetupHook.Dispose();
        this.onAddonSetup2Hook.Dispose();
        this.onAddonFinalizeHook.Dispose();
        this.onAddonDrawHook.Dispose();
        this.onAddonUpdateHook.Dispose();
        this.onAddonRefreshHook.Dispose();
        this.onAddonRequestedUpdateHook.Dispose();

        foreach (var receiveEventListener in this.ReceiveEventListeners)
        {
            receiveEventListener.Dispose();
        }
    }

    /// <summary>
    /// Register a listener for the target event and addon.
    /// </summary>
    /// <param name="listener">The listener to register.</param>
    internal void RegisterListener(AddonLifecycleEventListener listener)
    {
        this.framework.RunOnTick(() =>
        {
            this.EventListeners.Add(listener);
            
            // If we want receive event messages have an already active addon, enable the receive event hook.
            // If the addon isn't active yet, we'll grab the hook when it sets up.
            if (listener is { EventType: AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent })
            {
                if (this.ReceiveEventListeners.FirstOrDefault(listeners => listeners.AddonNames.Contains(listener.AddonName)) is { } receiveEventListener)
                {
                    receiveEventListener.Hook?.Enable();
                }
            }
        });
    }

    /// <summary>
    /// Unregisters the listener from events.
    /// </summary>
    /// <param name="listener">The listener to unregister.</param>
    internal void UnregisterListener(AddonLifecycleEventListener listener)
    {
        // Set removed state to true immediately, then lazily remove it from the EventListeners list on next Framework Update.
        listener.Removed = true;
        
        this.framework.RunOnTick(() =>
        {
            this.EventListeners.Remove(listener);
            
            // If we are disabling an ReceiveEvent listener, check if we should disable the hook.
            if (listener is { EventType: AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent })
            {
                // Get the ReceiveEvent Listener for this addon
                if (this.ReceiveEventListeners.FirstOrDefault(listeners => listeners.AddonNames.Contains(listener.AddonName)) is { } receiveEventListener)
                {
                    // If there are no other listeners listening for this event, disable the hook.
                    if (!this.EventListeners.Any(listeners => listeners.AddonName.Contains(listener.AddonName) && listener.EventType is AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent))
                    {
                        receiveEventListener.Hook?.Disable();
                    }
                }
            }
        });
    }

    /// <summary>
    /// Invoke listeners for the specified event type.
    /// </summary>
    /// <param name="eventType">Event Type.</param>
    /// <param name="args">AddonArgs.</param>
    /// <param name="blame">What to blame on errors.</param>
    internal void InvokeListenersSafely(AddonEvent eventType, AddonArgs args, [CallerMemberName] string blame = "")
    {
        // Do not use linq; this is a high-traffic function, and more heap allocations avoided, the better.
        foreach (var listener in this.EventListeners)
        {
            if (listener.EventType != eventType)
                continue;

            // If the listener is pending removal, and is waiting until the next Framework Update, don't invoke listener.
            if (listener.Removed)
                continue;
            
            // Match on string.empty for listeners that want events for all addons.
            if (!string.IsNullOrWhiteSpace(listener.AddonName) && !args.IsAddon(listener.AddonName))
                continue;

            try
            {
                listener.FunctionDelegate.Invoke(eventType, args);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Exception in {blame} during {eventType} invoke.");
            }
        }
    }

    private void RegisterReceiveEventHook(AtkUnitBase* addon)
    {
        // Hook the addon's ReceiveEvent function here, but only enable the hook if we have an active listener.
        // Disallows hooking the core internal event handler.
        var addonName = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);
        var receiveEventAddress = (nint)addon->VTable->ReceiveEvent;
        if (receiveEventAddress != this.disallowedReceiveEventAddress)
        {
            // If we have a ReceiveEvent listener already made for this hook address, add this addon's name to that handler.
            if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.HookAddress == receiveEventAddress) is { } existingListener)
            {
                if (!existingListener.AddonNames.Contains(addonName))
                {
                    existingListener.AddonNames.Add(addonName);
                }
            }

            // Else, we have an addon that we don't have the ReceiveEvent for yet, make it.
            else
            {
                this.ReceiveEventListeners.Add(new AddonLifecycleReceiveEventListener(this, addonName, receiveEventAddress));
            }

            // If we have an active listener for this addon already, we need to activate this hook.
            if (this.EventListeners.Any(listener => (listener.EventType is AddonEvent.PostReceiveEvent or AddonEvent.PreReceiveEvent) && listener.AddonName == addonName))
            {
                if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.AddonNames.Contains(addonName)) is { } receiveEventListener)
                {
                    receiveEventListener.Hook?.Enable();
                }
            }
        }
    }

    private void UnregisterReceiveEventHook(string addonName)
    {
        // Remove this addons ReceiveEvent Registration
        if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.AddonNames.Contains(addonName)) is { } eventListener)
        {
            eventListener.AddonNames.Remove(addonName);

            // If there are no more listeners let's remove and dispose.
            if (eventListener.AddonNames.Count is 0)
            {
                this.ReceiveEventListeners.Remove(eventListener);
                eventListener.Dispose();
            }
        }
    }

    private void OnAddonSetup(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        try
        {
            this.RegisterReceiveEventHook(addon);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup ReceiveEvent Registration.");
        }

        using var returner = this.argsPool.Rent(out AddonSetupArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.AtkValueCount = valueCount;
        arg.AtkValues = (nint)values;
        this.InvokeListenersSafely(AddonEvent.PreSetup, arg);
        valueCount = arg.AtkValueCount;
        values = (AtkValue*)arg.AtkValues;

        try
        {
            addon->OnSetup(valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonSetup. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostSetup, arg);
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            var addonName = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase[0]->Name);
            this.UnregisterReceiveEventHook(addonName);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonFinalize ReceiveEvent Removal.");
        }

        using var returner = this.argsPool.Rent(out AddonFinalizeArgs arg);
        arg.AddonInternal = (nint)atkUnitBase[0];
        this.InvokeListenersSafely(AddonEvent.PreFinalize, arg);

        try
        {
            this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonFinalize. This may be a bug in the game or another plugin hooking this method.");
        }
    }

    private void OnAddonDraw(AtkUnitBase* addon)
    {
        using var returner = this.argsPool.Rent(out AddonDrawArgs arg);
        arg.AddonInternal = (nint)addon;
        this.InvokeListenersSafely(AddonEvent.PreDraw, arg);

        try
        {
            addon->Draw();
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonDraw. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostDraw, arg);
    }

    private void OnAddonUpdate(AtkUnitBase* addon, float delta)
    {
        using var returner = this.argsPool.Rent(out AddonUpdateArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.TimeDeltaInternal = delta;
        this.InvokeListenersSafely(AddonEvent.PreUpdate, arg);

        try
        {
            addon->Update(delta);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostUpdate, arg);
    }

    private byte OnAddonRefresh(AtkUnitManager* atkUnitManager, AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        byte result = 0;

        using var returner = this.argsPool.Rent(out AddonRefreshArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.AtkValueCount = valueCount;
        arg.AtkValues = (nint)values;
        this.InvokeListenersSafely(AddonEvent.PreRefresh, arg);
        valueCount = arg.AtkValueCount;
        values = (AtkValue*)arg.AtkValues;

        try
        {
            result = this.onAddonRefreshHook.Original(atkUnitManager, addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRefresh. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostRefresh, arg);
        return result;
    }

    private void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        using var returner = this.argsPool.Rent(out AddonRequestedUpdateArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.NumberArrayData = (nint)numberArrayData;
        arg.StringArrayData = (nint)stringArrayData;
        this.InvokeListenersSafely(AddonEvent.PreRequestedUpdate, arg);
        numberArrayData = (NumberArrayData**)arg.NumberArrayData;
        stringArrayData = (StringArrayData**)arg.StringArrayData;

        try
        {
            addon->OnUpdate(numberArrayData, stringArrayData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRequestedUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostRequestedUpdate, arg);
    }
}

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IAddonLifecycle>]
#pragma warning restore SA1015
internal class AddonLifecyclePluginScoped : IInternalDisposableService, IAddonLifecycle
{
    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycleService = Service<AddonLifecycle>.Get();

    private readonly List<AddonLifecycleEventListener> eventListeners = new();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var listener in this.eventListeners)
        {
            this.addonLifecycleService.UnregisterListener(listener);
        }
    }

    /// <inheritdoc/>
    public void RegisterListener(AddonEvent eventType, IEnumerable<string> addonNames, IAddonLifecycle.AddonEventDelegate handler)
    {
        foreach (var addonName in addonNames)
        {
            this.RegisterListener(eventType, addonName, handler);
        }
    }

    /// <inheritdoc/>
    public void RegisterListener(AddonEvent eventType, string addonName, IAddonLifecycle.AddonEventDelegate handler)
    {
        var listener = new AddonLifecycleEventListener(eventType, addonName, handler);
        this.eventListeners.Add(listener);
        this.addonLifecycleService.RegisterListener(listener);
    }

    /// <inheritdoc/>
    public void RegisterListener(AddonEvent eventType, IAddonLifecycle.AddonEventDelegate handler)
    {
        this.RegisterListener(eventType, string.Empty, handler);
    }

    /// <inheritdoc/>
    public void UnregisterListener(AddonEvent eventType, IEnumerable<string> addonNames, IAddonLifecycle.AddonEventDelegate? handler = null)
    {
        foreach (var addonName in addonNames)
        {
            this.UnregisterListener(eventType, addonName, handler);
        }
    }

    /// <inheritdoc/>
    public void UnregisterListener(AddonEvent eventType, string addonName, IAddonLifecycle.AddonEventDelegate? handler = null)
    {
        this.eventListeners.RemoveAll(entry =>
        {
            if (entry.EventType != eventType) return false;
            if (entry.AddonName != addonName) return false;
            if (handler is not null && entry.FunctionDelegate != handler) return false;

            this.addonLifecycleService.UnregisterListener(entry);
            return true;
        });
    }

    /// <inheritdoc/>
    public void UnregisterListener(AddonEvent eventType, IAddonLifecycle.AddonEventDelegate? handler = null)
    {
        this.UnregisterListener(eventType, string.Empty, handler);
    }

    /// <inheritdoc/>
    public void UnregisterListener(params IAddonLifecycle.AddonEventDelegate[] handlers)
    {
        foreach (var handler in handlers)
        {
            this.eventListeners.RemoveAll(entry =>
            {
                if (entry.FunctionDelegate != handler) return false;
            
                this.addonLifecycleService.UnregisterListener(entry);
                return true;
            });
        }
    }
}

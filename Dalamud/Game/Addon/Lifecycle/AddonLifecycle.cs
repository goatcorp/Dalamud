using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
internal unsafe class AddonLifecycle : IDisposable, IServiceType
{
    /// <summary>
    /// List of all AddonLifecycle ReceiveEvent Listener Hooks.
    /// </summary>
    internal readonly List<AddonLifecycleReceiveEventListener> ReceiveEventListeners = new();
    
    /// <summary>
    /// List of all AddonLifecycle Event Listeners.
    /// </summary>
    internal readonly List<AddonLifecycleEventListener> EventListeners = new();
    
    private static readonly ModuleLog Log = new("AddonLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly nint disallowedReceiveEventAddress;
    
    private readonly AddonLifecycleAddressResolver address;
    private readonly CallHook<AddonSetupDelegate> onAddonSetupHook;
    private readonly CallHook<AddonSetupDelegate> onAddonSetup2Hook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;
    private readonly CallHook<AddonDrawDelegate> onAddonDrawHook;
    private readonly CallHook<AddonUpdateDelegate> onAddonUpdateHook;
    private readonly Hook<AddonOnRefreshDelegate> onAddonRefreshHook;
    private readonly CallHook<AddonOnRequestedUpdateDelegate> onAddonRequestedUpdateHook;

    private readonly ConcurrentBag<AddonLifecycleEventListener> newEventListeners = new();
    private readonly ConcurrentBag<AddonLifecycleEventListener> removeEventListeners = new();

    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(TargetSigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);

        // We want value of the function pointer at vFunc[2]
        this.disallowedReceiveEventAddress = ((nint*)this.address.AtkEventListener)![2];
        
        this.framework.Update += this.OnFrameworkUpdate;

        this.onAddonSetupHook = new CallHook<AddonSetupDelegate>(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonSetup2Hook = new CallHook<AddonSetupDelegate>(this.address.AddonSetup2, this.OnAddonSetup);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonFinalize, this.OnAddonFinalize);
        this.onAddonDrawHook = new CallHook<AddonDrawDelegate>(this.address.AddonDraw, this.OnAddonDraw);
        this.onAddonUpdateHook = new CallHook<AddonUpdateDelegate>(this.address.AddonUpdate, this.OnAddonUpdate);
        this.onAddonRefreshHook = Hook<AddonOnRefreshDelegate>.FromAddress(this.address.AddonOnRefresh, this.OnAddonRefresh);
        this.onAddonRequestedUpdateHook = new CallHook<AddonOnRequestedUpdateDelegate>(this.address.AddonOnRequestedUpdate, this.OnRequestedUpdate);
    }

    private delegate void AddonSetupDelegate(AtkUnitBase* addon, uint valueCount, AtkValue* values);

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private delegate void AddonDrawDelegate(AtkUnitBase* addon);

    private delegate void AddonUpdateDelegate(AtkUnitBase* addon, float delta);

    private delegate void AddonOnRequestedUpdateDelegate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private delegate byte AddonOnRefreshDelegate(AtkUnitManager* unitManager, AtkUnitBase* addon, uint valueCount, AtkValue* values);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.framework.Update -= this.OnFrameworkUpdate;

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
        this.newEventListeners.Add(listener);
    }

    /// <summary>
    /// Unregisters the listener from events.
    /// </summary>
    /// <param name="listener">The listener to unregister.</param>
    internal void UnregisterListener(AddonLifecycleEventListener listener)
    {
        this.removeEventListeners.Add(listener);
    }

    /// <summary>
    /// Invoke listeners for the specified event type.
    /// </summary>
    /// <param name="eventType">Event Type.</param>
    /// <param name="args">AddonArgs.</param>
    internal void InvokeListeners(AddonEvent eventType, AddonArgs args)
    {
        // Match on string.empty for listeners that want events for all addons.
        foreach (var listener in this.EventListeners.Where(listener => listener.EventType == eventType && (listener.AddonName == args.AddonName || listener.AddonName == string.Empty)))
        {
            listener.FunctionDelegate.Invoke(eventType, args);
        }
    }

    // Used to prevent concurrency issues if plugins try to register during iteration of listeners.
    private void OnFrameworkUpdate(IFramework unused)
    {
        if (this.newEventListeners.Any())
        {
            foreach (var toAddListener in this.newEventListeners)
            {
                this.EventListeners.Add(toAddListener);

                // If we want receive event messages have an already active addon, enable the receive event hook.
                // If the addon isn't active yet, we'll grab the hook when it sets up.
                if (toAddListener is { EventType: AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent })
                {
                    if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.AddonNames.Contains(toAddListener.AddonName)) is { } receiveEventListener)
                    {
                        receiveEventListener.Hook?.Enable();
                    }
                }
            }
            
            this.newEventListeners.Clear();
        }

        if (this.removeEventListeners.Any())
        {
            foreach (var toRemoveListener in this.removeEventListeners)
            {
                this.EventListeners.Remove(toRemoveListener);
                
                // If we are disabling an ReceiveEvent listener, check if we should disable the hook.
                if (toRemoveListener is { EventType: AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent })
                {
                    // Get the ReceiveEvent Listener for this addon
                    if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.AddonNames.Contains(toRemoveListener.AddonName)) is { } receiveEventListener)
                    {
                        // If there are no other listeners listening for this event, disable the hook.
                        if (!this.EventListeners.Any(listener => listener.AddonName.Contains(toRemoveListener.AddonName) && listener.EventType is AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent))
                        {
                            receiveEventListener.Hook?.Disable();
                        }
                    }
                }
            }

            this.removeEventListeners.Clear();
        }
    }

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onAddonSetupHook.Enable();
        this.onAddonSetup2Hook.Enable();
        this.onAddonFinalizeHook.Enable();
        this.onAddonDrawHook.Enable();
        this.onAddonUpdateHook.Enable();
        this.onAddonRefreshHook.Enable();
        this.onAddonRequestedUpdateHook.Enable();
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
        
        try
        {
            this.InvokeListeners(AddonEvent.PreSetup, new AddonSetupArgs
            {
                Addon = (nint)addon, 
                AtkValueCount = valueCount,
                AtkValues = (nint)values,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup pre-setup invoke.");
        }

        try
        {
            addon->OnSetup(valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonSetup. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.InvokeListeners(AddonEvent.PostSetup, new AddonSetupArgs
            {
                Addon = (nint)addon, 
                AtkValueCount = valueCount,
                AtkValues = (nint)values,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup post-setup invoke.");
        }
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
        
        try
        {
            this.InvokeListeners(AddonEvent.PreFinalize, new AddonFinalizeArgs { Addon = (nint)atkUnitBase[0] });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonFinalize pre-finalize invoke.");
        }

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
        try
        {
            this.InvokeListeners(AddonEvent.PreDraw, new AddonDrawArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonDraw pre-draw invoke.");
        }

        try
        {
            addon->Draw();
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonDraw. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.InvokeListeners(AddonEvent.PostDraw, new AddonDrawArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonDraw post-draw invoke.");
        }
    }

    private void OnAddonUpdate(AtkUnitBase* addon, float delta)
    {
        try
        {
            this.InvokeListeners(AddonEvent.PreUpdate, new AddonUpdateArgs { Addon = (nint)addon, TimeDelta = delta });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonUpdate pre-update invoke.");
        }

        try
        {
            addon->Update(delta);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.InvokeListeners(AddonEvent.PostUpdate, new AddonUpdateArgs { Addon = (nint)addon, TimeDelta = delta });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonUpdate post-update invoke.");
        }
    }

    private byte OnAddonRefresh(AtkUnitManager* atkUnitManager, AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        byte result = 0;
        
        try
        {
            this.InvokeListeners(AddonEvent.PreRefresh, new AddonRefreshArgs
            {
                Addon = (nint)addon, 
                AtkValueCount = valueCount,
                AtkValues = (nint)values,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonRefresh pre-refresh invoke.");
        }

        try
        {
            result = this.onAddonRefreshHook.Original(atkUnitManager, addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRefresh. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.InvokeListeners(AddonEvent.PostRefresh, new AddonRefreshArgs
            {
                Addon = (nint)addon, 
                AtkValueCount = valueCount,
                AtkValues = (nint)values,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonRefresh post-refresh invoke.");
        }

        return result;
    }

    private void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        try
        {
            this.InvokeListeners(AddonEvent.PreRequestedUpdate, new AddonRequestedUpdateArgs
            {
                Addon = (nint)addon,
                NumberArrayData = (nint)numberArrayData,
                StringArrayData = (nint)stringArrayData,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnRequestedUpdate pre-requestedUpdate invoke.");
        }

        try
        {
            addon->OnUpdate(numberArrayData, stringArrayData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRequestedUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        try
        {
            this.InvokeListeners(AddonEvent.PostRequestedUpdate, new AddonRequestedUpdateArgs
            {
                Addon = (nint)addon,
                NumberArrayData = (nint)numberArrayData,
                StringArrayData = (nint)stringArrayData,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnRequestedUpdate post-requestedUpdate invoke.");
        }
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
internal class AddonLifecyclePluginScoped : IDisposable, IServiceType, IAddonLifecycle
{
    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycleService = Service<AddonLifecycle>.Get();

    private readonly List<AddonLifecycleEventListener> eventListeners = new();

    /// <inheritdoc/>
    public void Dispose()
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.AddonLifecycle;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonLifecycle : IDisposable, IServiceType
{
    private static readonly ModuleLog Log = new("AddonLifecycle");
    
    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();
    
    private readonly AddonLifecycleAddressResolver address;
    private readonly Hook<AddonSetupDelegate> onAddonSetupHook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;
    private readonly CallHook<AddonDrawDelegate> onAddonDrawHook;
    private readonly CallHook<AddonUpdateDelegate> onAddonUpdateHook;

    private readonly ConcurrentBag<AddonLifecycleEventListener> newEventListeners = new();
    private readonly ConcurrentBag<AddonLifecycleEventListener> removeEventListeners = new();
    private readonly List<AddonLifecycleEventListener> eventListeners = new();
    
    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(SigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);
        
        this.framework.Update += this.OnFrameworkUpdate;

        this.onAddonSetupHook = Hook<AddonSetupDelegate>.FromAddress(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonFinalize, this.OnAddonFinalize);
        this.onAddonDrawHook = new CallHook<AddonDrawDelegate>(this.address.AddonDraw, this.OnAddonDraw);
        this.onAddonUpdateHook = new CallHook<AddonUpdateDelegate>(this.address.AddonUpdate, this.OnAddonUpdate);
    }

    private delegate nint AddonSetupDelegate(AtkUnitBase* addon);

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private delegate void AddonDrawDelegate(AtkUnitBase* addon);

    private delegate void AddonUpdateDelegate(AtkUnitBase* addon, float delta);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.framework.Update -= this.OnFrameworkUpdate;
        
        this.onAddonSetupHook.Dispose();
        this.onAddonFinalizeHook.Dispose();
        this.onAddonDrawHook.Dispose();
        this.onAddonUpdateHook.Dispose();
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
        
    // Used to prevent concurrency issues if plugins try to register during iteration of listeners.
    private void OnFrameworkUpdate(Framework unused)
    {
        if (this.newEventListeners.Any())
        {
            this.eventListeners.AddRange(this.newEventListeners);
            this.newEventListeners.Clear();
        }

        if (this.removeEventListeners.Any())
        {
            foreach (var toRemoveListener in this.removeEventListeners)
            {
                this.eventListeners.Remove(toRemoveListener);
            }
            
            this.removeEventListeners.Clear();
        }
    }
    
    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onAddonSetupHook.Enable();
        this.onAddonFinalizeHook.Enable();
        this.onAddonDrawHook.Enable();
        this.onAddonUpdateHook.Enable();
    }
    
    private void InvokeListeners(AddonEvent eventType, IAddonLifecycle.AddonArgs args)
    {
        // Match on string.empty for listeners that want events for all addons.
        foreach (var listener in this.eventListeners.Where(listener => listener.EventType == eventType && (listener.AddonName == args.AddonName || listener.AddonName == string.Empty)))
        {
            listener.FunctionDelegate.Invoke(eventType, args);
        }
    }

    private nint OnAddonSetup(AtkUnitBase* addon)
    {
        if (addon is null)
            return this.onAddonSetupHook.Original(addon);

        try
        {
            this.InvokeListeners(AddonEvent.PreSetup, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup pre-setup invoke.");
        }

        var result = this.onAddonSetupHook.Original(addon);

        try
        {
            this.InvokeListeners(AddonEvent.PostSetup, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup post-setup invoke.");
        }

        return result;
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        if (atkUnitBase is null || atkUnitBase[0] is null)
        {
            this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);
            return;
        }
        
        try
        {
            this.InvokeListeners(AddonEvent.PreFinalize, new IAddonLifecycle.AddonArgs { Addon = (nint)atkUnitBase[0] });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonFinalize pre-finalize invoke.");
        }

        this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);
    }
    
    private void OnAddonDraw(AtkUnitBase* addon)
    {
        try
        {
            this.InvokeListeners(AddonEvent.PreDraw, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonDraw pre-draw invoke.");
        }
        
        addon->Draw();

        try
        {
            this.InvokeListeners(AddonEvent.PostDraw, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
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
            this.InvokeListeners(AddonEvent.PreUpdate, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonUpdate pre-update invoke.");
        }

        addon->Update(delta);

        try
        {
            this.InvokeListeners(AddonEvent.PostUpdate, new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonUpdate post-update invoke.");
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
    private static readonly ModuleLog Log = new("AddonLifecycle:PluginScoped");
    
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
        // This style is simpler to read imo. If the handler is null we want all entries,
        // if they specified a handler then only the specific entries with that handler.
        var targetListeners = this.eventListeners
                                  .Where(entry => entry.EventType == eventType)
                                  .Where(entry => entry.AddonName == addonName)
                                  .Where(entry => handler is null || entry.FunctionDelegate == handler)
                                  .ToArray(); // Make a copy so we don't mutate this list while removing entries.

        foreach (var listener in targetListeners)
        {
            this.addonLifecycleService.UnregisterListener(listener);
            this.eventListeners.Remove(listener);
        }
    }
    
    /// <inheritdoc/>
    public void UnregisterListener(AddonEvent eventType, IAddonLifecycle.AddonEventDelegate? handler = null)
    {
        this.UnregisterListener(eventType, string.Empty, handler);
    }
    
    /// <inheritdoc/>
    public void UnregisterListener(IAddonLifecycle.AddonEventDelegate handler, params IAddonLifecycle.AddonEventDelegate[] handlers)
    {
        foreach (var listener in this.eventListeners.Where(entry => entry.FunctionDelegate == handler).ToArray())
        {
            this.addonLifecycleService.UnregisterListener(listener);
            this.eventListeners.Remove(listener);
        }

        foreach (var handlerParma in handlers)
        {
            foreach (var listener in this.eventListeners.Where(entry => entry.FunctionDelegate == handlerParma).ToArray())
            {
                this.addonLifecycleService.UnregisterListener(listener);
                this.eventListeners.Remove(listener);
            }
        }
    }
}

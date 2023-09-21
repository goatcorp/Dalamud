using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Addon.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon;

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
    private readonly Hook<AddonOnRefreshDelegate> onAddonRefreshHook;
    private readonly CallHook<AddonOnRequestedUpdateDelegate> onAddonRequestedUpdateHook;

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
        this.onAddonRefreshHook = Hook<AddonOnRefreshDelegate>.FromAddress(this.address.AddonOnRefresh, this.OnAddonRefresh);
        this.onAddonRequestedUpdateHook = new CallHook<AddonOnRequestedUpdateDelegate>(this.address.AddonOnRequestedUpdate, this.OnRequestedUpdate);
    }

    private delegate nint AddonSetupDelegate(AtkUnitBase* addon);

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
        this.onAddonFinalizeHook.Dispose();
        this.onAddonDrawHook.Dispose();
        this.onAddonUpdateHook.Dispose();
        this.onAddonRefreshHook.Dispose();
        this.onAddonRequestedUpdateHook.Dispose();
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
        this.onAddonRefreshHook.Enable();
        this.onAddonRequestedUpdateHook.Enable();
    }

    private void InvokeListeners(AddonEvent eventType, IAddonArgs args)
    {
        // Match on string.empty for listeners that want events for all addons.
        foreach (var listener in this.eventListeners.Where(listener => listener.EventType == eventType && (listener.AddonName == args.AddonName || listener.AddonName == string.Empty)))
        {
            listener.FunctionDelegate.Invoke(eventType, args);
        }
    }

    private nint OnAddonSetup(AtkUnitBase* addon)
    {
        try
        {
            this.InvokeListeners(AddonEvent.PreSetup, new AddonSetupArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup pre-setup invoke.");
        }

        var result = this.onAddonSetupHook.Original(addon);

        try
        {
            this.InvokeListeners(AddonEvent.PostSetup, new AddonSetupArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup post-setup invoke.");
        }

        return result;
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            this.InvokeListeners(AddonEvent.PreFinalize, new AddonFinalizeArgs { Addon = (nint)atkUnitBase[0] });
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
            this.InvokeListeners(AddonEvent.PreDraw, new AddonDrawArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonDraw pre-draw invoke.");
        }

        addon->Draw();

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

        addon->Update(delta);

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

        var result = this.onAddonRefreshHook.Original(atkUnitManager, addon, valueCount, values);

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

        addon->OnUpdate(numberArrayData, stringArrayData);

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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
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
    
    private readonly AddonLifecycleAddressResolver address;
    
    private readonly CallHook<AtkUnitBase.Delegates.OnSetup> onAddonSetupHook;
    private readonly CallHook<AtkUnitBase.Delegates.OnSetup> onAddonSetup2Hook;
    private readonly CallHook<AtkUnitBase.Delegates.Draw> onAddonDrawHook;
    private readonly CallHook<AtkUnitBase.Delegates.Update> onAddonUpdateHook;
    private readonly CallHook<AtkUnitBase.Delegates.OnRequestedUpdate> onAddonRequestedUpdateHook;
    
    private readonly Hook<AddonOnRefreshDelegate> onAddonRefreshHook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;

    private readonly AtkUnitBase* atkUnitBaseRoot;
    
    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(TargetSigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);

        this.atkUnitBaseRoot = this.address.AtkUnitBase;

        this.onAddonSetupHook = new CallHook<AtkUnitBase.Delegates.OnSetup>(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonSetup2Hook = new CallHook<AtkUnitBase.Delegates.OnSetup>(this.address.AddonSetup2, this.OnAddonSetup);
        this.onAddonDrawHook = new CallHook<AtkUnitBase.Delegates.Draw>(this.address.AddonDraw, this.OnAddonDraw);
        this.onAddonUpdateHook = new CallHook<AtkUnitBase.Delegates.Update>(this.address.AddonUpdate, this.OnAddonUpdate);
        this.onAddonRequestedUpdateHook = new CallHook<AtkUnitBase.Delegates.OnRequestedUpdate>(this.address.AddonOnRequestedUpdate, this.OnRequestedUpdate);
        
        this.onAddonRefreshHook = Hook<AddonOnRefreshDelegate>.FromAddress(this.address.AddonOnRefresh, this.OnAddonRefresh);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonFinalize, this.OnAddonFinalize);

        this.onAddonSetupHook.Enable();
        this.onAddonSetup2Hook.Enable();
        this.onAddonDrawHook.Enable();
        this.onAddonUpdateHook.Enable();
        this.onAddonRequestedUpdateHook.Enable();
        
        this.onAddonRefreshHook.Enable();
        this.onAddonFinalizeHook.Enable();
    }

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private delegate byte AddonOnRefreshDelegate(AtkUnitManager* unitManager, AtkUnitBase* addon, uint valueCount, AtkValue* values);

    /// <summary>
    /// Gets a list of all AddonLifecycle ReceiveEvent Listener Hooks.
    /// </summary>
    internal List<AddonLifecycleReceiveEventListener> ReceiveEventListeners { get; } = [];
    
    /// <summary>
    /// Gets a list of all AddonLifecycle Event Listeners.
    /// </summary>
    internal List<AddonLifecycleEventListener> EventListeners { get; } = [];

    /// <summary>
    /// Gets a list of all AddonLifecycle Show Hooks.
    /// </summary>
    internal List<Hook<AtkUnitBase.Delegates.Show>> ShowHooks { get; } = [];
    
    /// <summary>
    /// Gets a list of all AddonLifecycle Hide Hooks.
    /// </summary> 
    internal List<Hook<AtkUnitBase.Delegates.Hide>> HideHooks { get; } = [];

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
            
            // If we want to receive event messages have an already active addon, enable the receive event hook.
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

    private void RegisterSpecialEventHooks(AtkUnitBase* addon)
    {
        // Hook the addon's ReceiveEvent function here, but only enable the hook if we have an active listener.
        // Disallows hooking the core internal event handler.
        var addonName = addon->NameString;
        var receiveEventAddress = (nint)addon->VirtualTable->ReceiveEvent;
        if (receiveEventAddress != (nint)this.atkUnitBaseRoot->VirtualTable->ReceiveEvent)
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
        
        // Hook the addon's Show and Hide functions here, but only enable the hook if we have an active listener.
        // Disallows hooking the core internal event handler for AtkUnitBase.Show/AtkUnitBase.Hide
        // It's fine to enable these hooks immediately, as we don't expect them to be called anywhere near the same rate as ReceiveEvent
        var showAddress = (nint)addon->VirtualTable->Show;
        if (showAddress != (nint)this.atkUnitBaseRoot->VirtualTable->Show && this.ShowHooks.All(hook => hook.Address != showAddress))
        {
            var showHook = Hook<AtkUnitBase.Delegates.Show>.FromAddress(showAddress, this.OnAddonShow);
            Log.Debug($"Hooking {addonName}.Show");
            showHook.Enable();
            
            this.ShowHooks.Add(showHook);
        }
        
        var hideAddress = (nint)addon->VirtualTable->Hide;
        if (hideAddress != (nint)this.atkUnitBaseRoot->VirtualTable->Hide && this.HideHooks.All(hook => hook.Address != hideAddress))
        {
            var hideHook = Hook<AtkUnitBase.Delegates.Hide>.FromAddress(hideAddress, this.OnAddonHide);
            Log.Debug($"Hooking {addonName}.Hide");
            hideHook.Enable();
            
            this.HideHooks.Add(hideHook);
        }
    }

    private void UnregisterSpecialEventHooks(AtkUnitBase* addon)
    {
        // Remove this addons ReceiveEvent Registration
        if (this.ReceiveEventListeners.FirstOrDefault(listener => listener.AddonNames.Contains(addon->NameString)) is { } eventListener)
        {
            eventListener.AddonNames.Remove(addon->NameString);

            // If there are no more listeners let's remove and dispose.
            if (eventListener.AddonNames.Count is 0)
            {
                this.ReceiveEventListeners.Remove(eventListener);
                eventListener.Dispose();
            }
        }

        if (this.ShowHooks.FirstOrDefault(hook => hook.Address == (nint)addon->VirtualTable->Show) is { } showHook)
        {
            showHook.Dispose();
            this.ShowHooks.Remove(showHook);
        }
        
        if (this.HideHooks.FirstOrDefault(hook => hook.Address == (nint)addon->VirtualTable->Hide) is { } hideHook)
        {
            hideHook.Dispose();
            this.HideHooks.Remove(hideHook);
        }
    }

    private void OnAddonSetup(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        try
        {
            this.RegisterSpecialEventHooks(addon);
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
            var addon = atkUnitBase[0];
            this.UnregisterSpecialEventHooks(addon);
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

    private void OnAddonShow(AtkUnitBase* addon, bool openSilently, uint unsetShowHideFlags)
    {
        Log.Debug($"{addon->NameString}.Show Invoked");
        
        using var returner = this.argsPool.Rent(out AddonShowArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.OpenSilently = openSilently;
        arg.UnsetShowHideFlags = unsetShowHideFlags;
        this.InvokeListenersSafely(AddonEvent.PreShow, arg);
        openSilently = arg.OpenSilently;
        unsetShowHideFlags = arg.UnsetShowHideFlags;
        
        try
        {
            // This should be fine, because we can only land in this function if an addon that we actually hooked is called.
            this.ShowHooks.First(hook => hook.Address == (nint)addon->VirtualTable->Show).Original(addon, openSilently, unsetShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonShow. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostShow, arg);
    }

    private void OnAddonHide(AtkUnitBase* addon, bool unknown, bool callCallback, uint setShowHideFlags)
    {
        Log.Debug($"{addon->NameString}.Hide Invoked");
        
        using var returner = this.argsPool.Rent(out AddonHideArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.Unknown = unknown;
        arg.CallHideCallback = callCallback;
        arg.SetShowHideFlags = setShowHideFlags;
        this.InvokeListenersSafely(AddonEvent.PreHide, arg);
        unknown = arg.Unknown;
        callCallback = arg.CallHideCallback;
        setShowHideFlags = arg.SetShowHideFlags;
        
        try
        {
            // This should be fine, because we can only land in this function if an addon that we actually hooked is called.
            this.HideHooks.First(hook => hook.Address == (nint)addon->VirtualTable->Hide).Original(addon, unknown, callCallback, setShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonHide. This may be a bug in the game or another plugin hooking this method.");
        }

        this.InvokeListenersSafely(AddonEvent.PostHide, arg);
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
            addon->OnRequestedUpdate(numberArrayData, stringArrayData);
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

    private readonly List<AddonLifecycleEventListener> eventListeners = [];

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

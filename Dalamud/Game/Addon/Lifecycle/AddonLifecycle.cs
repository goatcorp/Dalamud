using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonLifecycle : IInternalDisposableService
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly Dictionary<string, AddonVirtualTable> modifiedTables = [];

    private Hook<AtkUnitBase.Delegates.Initialize>? onInitializeAddonHook;

    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(TargetSigScanner sigScanner)
    {
        this.onInitializeAddonHook = Hook<AtkUnitBase.Delegates.Initialize>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->Initialize, this.OnAddonInitialize);
        this.onInitializeAddonHook.Enable();
    }

    /// <summary>
    /// Gets a list of all AddonLifecycle Event Listeners.
    /// </summary>
    internal Dictionary<AddonEvent, List<AddonLifecycleEventListener>> EventListeners { get; } = [];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onInitializeAddonHook?.Dispose();
        this.onInitializeAddonHook = null;

        this.framework.RunOnFrameworkThread(() =>
        {
            foreach (var virtualTable in this.modifiedTables.Values)
            {
                virtualTable.Dispose();
            }
        });
    }

    /// <summary>
    /// Register a listener for the target event and addon.
    /// </summary>
    /// <param name="listener">The listener to register.</param>
    internal void RegisterListener(AddonLifecycleEventListener listener)
    {
        this.EventListeners.TryAdd(listener.EventType, [ listener ]);
        this.EventListeners[listener.EventType].Add(listener);
    }

    /// <summary>
    /// Unregisters the listener from events.
    /// </summary>
    /// <param name="listener">The listener to unregister.</param>
    internal void UnregisterListener(AddonLifecycleEventListener listener)
    {
        if (this.EventListeners.TryGetValue(listener.EventType, out var listenerList))
        {
            listenerList.Remove(listener);
        }
    }

    /// <summary>
    /// Invoke listeners for the specified event type.
    /// </summary>
    /// <param name="eventType">Event Type.</param>
    /// <param name="args">AddonArgs.</param>
    /// <param name="blame">What to blame on errors.</param>
    internal void InvokeListenersSafely(AddonEvent eventType, AddonArgs args, [CallerMemberName] string blame = "")
    {
        // Early return if we don't have any listeners of this type
        if (!this.EventListeners.TryGetValue(eventType, out var listenerList)) return;

        // Do not use linq; this is a high-traffic function, and more heap allocations avoided, the better.
        foreach (var listener in listenerList)
        {
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

    private void OnAddonInitialize(AtkUnitBase* addon)
    {
        try
        {
            this.LogInitialize(addon->NameString);

            if (!this.modifiedTables.ContainsKey(addon->NameString))
            {
                // AddonVirtualTable class handles creating the virtual table, and overriding each of the tracked virtual functions
                var managedVirtualTableEntry = new AddonVirtualTable(addon, this)
                {
                    // This event is invoked when the game itself has disposed of an addon
                    // We can use this to know when to remove our virtual table entry
                    OnAddonFinalized = () => this.modifiedTables.Remove(addon->NameString),
                };

                this.modifiedTables.Add(addon->NameString,  managedVirtualTableEntry);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in AddonLifecycle during OnAddonInitialize.");
        }

        this.onInitializeAddonHook!.Original(addon);
    }

    [Conditional("DEBUG")]
    private void LogInitialize(string addonName)
    {
        Log.Debug($"Initializing {addonName}");
    }
}

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
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

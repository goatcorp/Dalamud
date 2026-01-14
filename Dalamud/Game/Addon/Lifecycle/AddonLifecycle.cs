using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    /// <summary>
    /// Gets a list of all allocated addon virtual tables.
    /// </summary>
    public static readonly List<AddonVirtualTable> AllocatedTables = [];

    private static readonly ModuleLog Log = ModuleLog.Create<AddonLifecycle>();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private Hook<AtkUnitBase.Delegates.Initialize>? onInitializeAddonHook;
    private bool isInvokingListeners;

    [ServiceManager.ServiceConstructor]
    private AddonLifecycle()
    {
        this.onInitializeAddonHook = Hook<AtkUnitBase.Delegates.Initialize>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->Initialize, this.OnAddonInitialize);
        this.onInitializeAddonHook.Enable();
    }

    /// <summary>
    /// Gets a list of all AddonLifecycle Event Listeners.
    /// </summary> <br/>
    /// Mapping is: EventType -> AddonName -> ListenerList
    internal Dictionary<AddonEvent, Dictionary<string, HashSet<AddonLifecycleEventListener>>> EventListeners { get; } = [];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onInitializeAddonHook?.Dispose();
        this.onInitializeAddonHook = null;

        AllocatedTables.ForEach(entry => entry.Dispose());
        AllocatedTables.Clear();
    }

    /// <summary>
    /// Resolves a virtual table address to the original virtual table address.
    /// </summary>
    /// <param name="tableAddress">The modified address to resolve.</param>
    /// <returns>The original address.</returns>
    internal static AtkUnitBase.AtkUnitBaseVirtualTable* GetOriginalVirtualTable(AtkUnitBase.AtkUnitBaseVirtualTable* tableAddress)
    {
        var matchedTable = AllocatedTables.FirstOrDefault(table => table.ModifiedVirtualTable == tableAddress);
        if (matchedTable == null)
        {
            return null;
        }

        return matchedTable.OriginalVirtualTable;
    }

    /// <summary>
    /// Register a listener for the target event and addon.
    /// </summary>
    /// <param name="listener">The listener to register.</param>
    internal void RegisterListener(AddonLifecycleEventListener listener)
    {
        if (this.isInvokingListeners)
        {
            this.framework.RunOnTick(() => this.RegisterListenerMethod(listener));
        }
        else
        {
            this.framework.RunOnFrameworkThread(() => this.RegisterListenerMethod(listener));
        }
    }

    /// <summary>
    /// Unregisters the listener from events.
    /// </summary>
    /// <param name="listener">The listener to unregister.</param>
    internal void UnregisterListener(AddonLifecycleEventListener listener)
    {
        if (this.isInvokingListeners)
        {
            this.framework.RunOnTick(() => this.UnregisterListenerMethod(listener));
        }
        else
        {
            this.framework.RunOnFrameworkThread(() => this.UnregisterListenerMethod(listener));
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
        this.isInvokingListeners = true;

        // Early return if we don't have any listeners of this type
        if (!this.EventListeners.TryGetValue(eventType, out var addonListeners)) return;

        // Handle listeners for this event type that don't care which addon is triggering it
        if (addonListeners.TryGetValue(string.Empty, out var globalListeners))
        {
            foreach (var listener in globalListeners)
            {
                try
                {
                    listener.FunctionDelegate.Invoke(eventType, args);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Exception in {blame} during {eventType} invoke, for global addon event listener.");
                }
            }
        }

        // Handle listeners that are listening for this addon and event type specifically
        if (addonListeners.TryGetValue(args.AddonName, out var addonListener))
        {
            foreach (var listener in addonListener)
            {
                try
                {
                    listener.FunctionDelegate.Invoke(eventType, args);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Exception in {blame} during {eventType} invoke, for specific addon {args.AddonName}.");
                }
            }
        }

        this.isInvokingListeners = false;
    }

    private void RegisterListenerMethod(AddonLifecycleEventListener listener)
    {
        if (!this.EventListeners.ContainsKey(listener.EventType))
        {
            if (!this.EventListeners.TryAdd(listener.EventType, []))
            {
                return;
            }
        }

        // Note: string.Empty is a valid addon name, as that will trigger on any addon for this event type
        if (!this.EventListeners[listener.EventType].ContainsKey(listener.AddonName))
        {
            if (!this.EventListeners[listener.EventType].TryAdd(listener.AddonName, []))
            {
                return;
            }
        }

        this.EventListeners[listener.EventType][listener.AddonName].Add(listener);
    }

    private void UnregisterListenerMethod(AddonLifecycleEventListener listener)
    {
        if (this.EventListeners.TryGetValue(listener.EventType, out var addonListeners))
        {
            if (addonListeners.TryGetValue(listener.AddonName, out var addonListener))
            {
                addonListener.Remove(listener);
            }
        }
    }

    private void OnAddonInitialize(AtkUnitBase* addon)
    {
        try
        {
            this.LogInitialize(addon->NameString);

            // AddonVirtualTable class handles creating the virtual table, and overriding each of the tracked virtual functions
            AllocatedTables.Add(new AddonVirtualTable(addon, this));
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

    /// <inheritdoc/>
    public unsafe nint GetOriginalVirtualTable(nint virtualTableAddress)
        => (nint)AddonLifecycle.GetOriginalVirtualTable((AtkUnitBase.AtkUnitBaseVirtualTable*)virtualTableAddress);
}

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Agent;

/// <summary>
/// This class provides events for in-game agent lifecycles.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class AgentLifecycle : IInternalDisposableService
{
    /// <summary>
    /// Gets a list of all allocated agent virtual tables.
    /// </summary>
    public static readonly List<AgentVirtualTable> AllocatedTables = [];

    private static readonly ModuleLog Log = new("AgentLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private Hook<AgentModule.Delegates.Ctor>? onInitializeAgentsHook;
    private bool isInvokingListeners;

    [ServiceManager.ServiceConstructor]
    private AgentLifecycle()
    {
        var agentModuleInstance = AgentModule.Instance();

        // Hook is only used to determine appropriate timing for replacing Agent Virtual Tables
        // If the agent module is already initialized, then we can replace the tables safely.
        if (agentModuleInstance is null)
        {
            this.onInitializeAgentsHook = Hook<AgentModule.Delegates.Ctor>.FromAddress((nint)AgentModule.MemberFunctionPointers.Ctor, this.OnAgentModuleInitialize);
            this.onInitializeAgentsHook.Enable();
        }
        else
        {
            // For safety because this might be injected async, we will make sure we are on the main thread first.
            this.framework.RunOnFrameworkThread(() => this.ReplaceVirtualTables(agentModuleInstance));
        }
    }

    /// <summary>
    /// Gets a list of all AgentLifecycle Event Listeners.
    /// </summary> <br/>
    /// Mapping is: EventType -> ListenerList
    internal Dictionary<AgentEvent, Dictionary<AgentId, HashSet<AgentLifecycleEventListener>>> EventListeners { get; } = [];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onInitializeAgentsHook?.Dispose();
        this.onInitializeAgentsHook = null;

        AllocatedTables.ForEach(entry => entry.Dispose());
        AllocatedTables.Clear();
    }

    /// <summary>
    /// Resolves a virtual table address to the original virtual table address.
    /// </summary>
    /// <param name="tableAddress">The modified address to resolve.</param>
    /// <returns>The original address.</returns>
    internal static AgentInterface.AgentInterfaceVirtualTable* GetOriginalVirtualTable(AgentInterface.AgentInterfaceVirtualTable* tableAddress)
    {
        var matchedTable = AllocatedTables.FirstOrDefault(table => table.ModifiedVirtualTable == tableAddress);
        if (matchedTable == null)
        {
            return null;
        }

        return matchedTable.OriginalVirtualTable;
    }

    /// <summary>
    /// Register a listener for the target event and agent.
    /// </summary>
    /// <param name="listener">The listener to register.</param>
    internal void RegisterListener(AgentLifecycleEventListener listener)
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
    internal void UnregisterListener(AgentLifecycleEventListener listener)
    {
        listener.IsRequestedToClear = true;
        
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
    /// <param name="args">AgentARgs.</param>
    /// <param name="blame">What to blame on errors.</param>
    internal void InvokeListenersSafely(AgentEvent eventType, AgentArgs args, [CallerMemberName] string blame = "")
    {
        this.isInvokingListeners = true;

        // Early return if we don't have any listeners of this type
        if (!this.EventListeners.TryGetValue(eventType, out var agentListeners)) return;

        // Handle listeners for this event type that don't care which agent is triggering it
        if (agentListeners.TryGetValue((AgentId)uint.MaxValue, out var globalListeners))
        {
            foreach (var listener in globalListeners)
            {
                if (listener.IsRequestedToClear) continue;
                
                try
                {
                    listener.FunctionDelegate.Invoke(eventType, args);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Exception in {blame} during {eventType} invoke, for global agent event listener.");
                }
            }
        }

        // Handle listeners that are listening for this agent and event type specifically
        if (agentListeners.TryGetValue(args.AgentId, out var agentListener))
        {
            foreach (var listener in agentListener)
            {
                if (listener.IsRequestedToClear) continue;
                
                try
                {
                    listener.FunctionDelegate.Invoke(eventType, args);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Exception in {blame} during {eventType} invoke, for specific agent {args.AgentId}.");
                }
            }
        }

        this.isInvokingListeners = false;
    }

    private void OnAgentModuleInitialize(AgentModule* thisPtr, UIModule* uiModule)
    {
        this.onInitializeAgentsHook!.Original(thisPtr, uiModule);

        try
        {
            this.ReplaceVirtualTables(thisPtr);

            // We don't need this hook anymore, it did its job!
            this.onInitializeAgentsHook!.Dispose();
            this.onInitializeAgentsHook = null;
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in AgentLifecycle during AgentModule Ctor.");
        }
    }

    private void RegisterListenerMethod(AgentLifecycleEventListener listener)
    {
        if (!this.EventListeners.ContainsKey(listener.EventType))
        {
            if (!this.EventListeners.TryAdd(listener.EventType, []))
            {
                return;
            }
        }

        // Note: uint.MaxValue is a valid agent id, as that will trigger on any agent for this event type
        if (!this.EventListeners[listener.EventType].ContainsKey(listener.AgentId))
        {
            if (!this.EventListeners[listener.EventType].TryAdd(listener.AgentId, []))
            {
                return;
            }
        }

        this.EventListeners[listener.EventType][listener.AgentId].Add(listener);
    }

    private void UnregisterListenerMethod(AgentLifecycleEventListener listener)
    {
        if (this.EventListeners.TryGetValue(listener.EventType, out var agentListeners))
        {
            if (agentListeners.TryGetValue(listener.AgentId, out var agentListener))
            {
                agentListener.Remove(listener);
            }
        }
    }

    private void ReplaceVirtualTables(AgentModule* agentModule)
    {
        foreach (uint index in Enumerable.Range(0, agentModule->Agents.Length))
        {
            try
            {
                var agentPointer = agentModule->Agents.GetPointer((int)index);

                if (agentPointer is null)
                {
                    Log.Warning("Null Agent Found?");
                    continue;
                }

                // AgentVirtualTable class handles creating the virtual table, and overriding each of the tracked virtual functions
                AllocatedTables.Add(new AgentVirtualTable(agentPointer->Value, (AgentId)index, this));
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in AgentLifecycle during ReplaceVirtualTables.");
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a AgentLifecycle service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IAgentLifecycle>]
#pragma warning restore SA1015
internal class AgentLifecyclePluginScoped : IInternalDisposableService, IAgentLifecycle
{
    [ServiceManager.ServiceDependency]
    private readonly AgentLifecycle agentLifecycleService = Service<AgentLifecycle>.Get();

    private readonly List<AgentLifecycleEventListener> eventListeners = [];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var listener in this.eventListeners)
        {
            this.agentLifecycleService.UnregisterListener(listener);
        }
    }

    /// <inheritdoc/>
    public void RegisterListener(AgentEvent eventType, IEnumerable<AgentId> agentIds, IAgentLifecycle.AgentEventDelegate handler)
    {
        foreach (var agentId in agentIds)
        {
            this.RegisterListener(eventType, agentId, handler);
        }
    }

    /// <inheritdoc/>
    public void RegisterListener(AgentEvent eventType, AgentId agentId, IAgentLifecycle.AgentEventDelegate handler)
    {
        var listener = new AgentLifecycleEventListener(eventType, agentId, handler);
        this.eventListeners.Add(listener);
        this.agentLifecycleService.RegisterListener(listener);
    }

    /// <inheritdoc/>
    public void RegisterListener(AgentEvent eventType, IAgentLifecycle.AgentEventDelegate handler)
    {
        this.RegisterListener(eventType, (AgentId)uint.MaxValue, handler);
    }

    /// <inheritdoc/>
    public void UnregisterListener(AgentEvent eventType, IEnumerable<AgentId> agentIds, IAgentLifecycle.AgentEventDelegate? handler = null)
    {
        foreach (var agentId in agentIds)
        {
            this.UnregisterListener(eventType, agentId, handler);
        }
    }

    /// <inheritdoc/>
    public void UnregisterListener(AgentEvent eventType, AgentId agentId, IAgentLifecycle.AgentEventDelegate? handler = null)
    {
        this.eventListeners.RemoveAll(entry =>
        {
            if (entry.EventType != eventType) return false;
            if (entry.AgentId != agentId) return false;
            if (handler is not null && entry.FunctionDelegate != handler) return false;

            this.agentLifecycleService.UnregisterListener(entry);
            return true;
        });
    }

    /// <inheritdoc/>
    public void UnregisterListener(AgentEvent eventType, IAgentLifecycle.AgentEventDelegate? handler = null)
    {
        this.UnregisterListener(eventType, (AgentId)uint.MaxValue, handler);
    }

    /// <inheritdoc/>
    public void UnregisterListener(params IAgentLifecycle.AgentEventDelegate[] handlers)
    {
        foreach (var handler in handlers)
        {
            this.eventListeners.RemoveAll(entry =>
            {
                if (entry.FunctionDelegate != handler) return false;

                this.agentLifecycleService.UnregisterListener(entry);
                return true;
            });
        }
    }

    /// <inheritdoc/>
    public unsafe nint GetOriginalVirtualTable(nint virtualTableAddress)
        => (nint)AgentLifecycle.GetOriginalVirtualTable((AgentInterface.AgentInterfaceVirtualTable*)virtualTableAddress);
}

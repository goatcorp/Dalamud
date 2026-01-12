using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Agent;

/// <summary>
/// Represents a class that holds references to an agents original and modified virtual table entries.
/// </summary>
internal unsafe class AgentVirtualTable : IDisposable
{
    // This need to be at minimum the largest virtual table size of all agents
    // Copying extra entries is not problematic, and is considered safe.
    private const int VirtualTableEntryCount = 60;

    private const bool EnableLogging = false;

    private static readonly ModuleLog Log = new("AgentVT");

    private readonly AgentLifecycle lifecycleService;

    private readonly AgentId agentId;

    // Each agent gets its own set of args that are used to mutate the original call when used in pre-calls
    private readonly AgentReceiveEventArgs receiveEventArgs = new();
    private readonly AgentReceiveEventArgs filteredReceiveEventArgs = new();
    private readonly AgentArgs showArgs = new();
    private readonly AgentArgs hideArgs = new();
    private readonly AgentArgs updateArgs = new();
    private readonly AgentGameEventArgs gameEventArgs = new();
    private readonly AgentLevelChangeArgs levelChangeArgs = new();
    private readonly AgentClassJobChangeArgs classJobChangeArgs = new();

    private readonly AgentInterface* agentInterface;

    // Pinned Function Delegates, as these functions get assigned to an unmanaged virtual table,
    // the CLR needs to know they are in use, or it will invalidate them causing random crashing.
    private readonly AgentInterface.Delegates.ReceiveEvent receiveEventFunction;
    private readonly AgentInterface.Delegates.ReceiveEvent2 filteredReceiveEventFunction;
    private readonly AgentInterface.Delegates.Show showFunction;
    private readonly AgentInterface.Delegates.Hide hideFunction;
    private readonly AgentInterface.Delegates.Update updateFunction;
    private readonly AgentInterface.Delegates.OnGameEvent gameEventFunction;
    private readonly AgentInterface.Delegates.OnLevelChange levelChangeFunction;
    private readonly AgentInterface.Delegates.OnClassJobChange classJobChangeFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentVirtualTable"/> class.
    /// </summary>
    /// <param name="agent">AgentInterface* for the agent to replace the table of.</param>
    /// <param name="agentId">Agent ID.</param>
    /// <param name="lifecycleService">Reference to AgentLifecycle service to callback and invoke listeners.</param>
    internal AgentVirtualTable(AgentInterface* agent, AgentId agentId, AgentLifecycle lifecycleService)
    {
        this.agentInterface = agent;
        this.agentId = agentId;
        this.lifecycleService = lifecycleService;

        // Save original virtual table
        this.OriginalVirtualTable = agent->VirtualTable;

        // Create copy of original table
        // Note this will copy any derived/overriden functions that this specific agent has.
        // Note: currently there are 16 virtual functions, but there's no harm in copying more for when they add new virtual functions to the game
        this.ModifiedVirtualTable = (AgentInterface.AgentInterfaceVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * VirtualTableEntryCount, 8);
        NativeMemory.Copy(agent->VirtualTable, this.ModifiedVirtualTable, 0x8 * VirtualTableEntryCount);

        // Overwrite the agents existing virtual table with our own
        agent->VirtualTable = this.ModifiedVirtualTable;

        // Pin each of our listener functions
        this.receiveEventFunction = this.OnAgentReceiveEvent;
        this.filteredReceiveEventFunction = this.OnAgentFilteredReceiveEvent;
        this.showFunction = this.OnAgentShow;
        this.hideFunction = this.OnAgentHide;
        this.updateFunction = this.OnAgentUpdate;
        this.gameEventFunction = this.OnAgentGameEvent;
        this.levelChangeFunction = this.OnAgentLevelChange;
        this.classJobChangeFunction = this.OnClassJobChange;

        // Overwrite specific virtual table entries
        this.ModifiedVirtualTable->ReceiveEvent = (delegate* unmanaged<AgentInterface*, AtkValue*, AtkValue*, uint, ulong, AtkValue*>)Marshal.GetFunctionPointerForDelegate(this.receiveEventFunction);
        this.ModifiedVirtualTable->ReceiveEvent2 = (delegate* unmanaged<AgentInterface*, AtkValue*, AtkValue*, uint, ulong, AtkValue*>)Marshal.GetFunctionPointerForDelegate(this.filteredReceiveEventFunction);
        this.ModifiedVirtualTable->Show = (delegate* unmanaged<AgentInterface*, void>)Marshal.GetFunctionPointerForDelegate(this.showFunction);
        this.ModifiedVirtualTable->Hide = (delegate* unmanaged<AgentInterface*, void>)Marshal.GetFunctionPointerForDelegate(this.hideFunction);
        this.ModifiedVirtualTable->Update = (delegate* unmanaged<AgentInterface*, uint, void>)Marshal.GetFunctionPointerForDelegate(this.updateFunction);
        this.ModifiedVirtualTable->OnGameEvent = (delegate* unmanaged<AgentInterface*, AgentInterface.GameEvent, void>)Marshal.GetFunctionPointerForDelegate(this.gameEventFunction);
        this.ModifiedVirtualTable->OnLevelChange = (delegate* unmanaged<AgentInterface*, byte, ushort, void>)Marshal.GetFunctionPointerForDelegate(this.levelChangeFunction);
        this.ModifiedVirtualTable->OnClassJobChange = (delegate* unmanaged<AgentInterface*, byte, void>)Marshal.GetFunctionPointerForDelegate(this.classJobChangeFunction);
    }

    /// <summary>
    /// Gets the original virtual table address for this agent.
    /// </summary>
    internal AgentInterface.AgentInterfaceVirtualTable* OriginalVirtualTable { get; private set; }

    /// <summary>
    /// Gets the modified virtual address for this agent.
    /// </summary>
    internal AgentInterface.AgentInterfaceVirtualTable* ModifiedVirtualTable { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Ensure restoration is done atomically.
        Interlocked.Exchange(ref *(nint*)&this.agentInterface->VirtualTable, (nint)this.OriginalVirtualTable);
        IMemorySpace.Free(this.ModifiedVirtualTable, 0x8 * VirtualTableEntryCount);
    }

    private AtkValue* OnAgentReceiveEvent(AgentInterface* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        AtkValue* result = null;

        try
        {
            this.LogEvent(EnableLogging);

            this.receiveEventArgs.Agent = thisPtr;
            this.receiveEventArgs.AgentId = this.agentId;
            this.receiveEventArgs.ReturnValue = (nint)returnValue;
            this.receiveEventArgs.AtkValues = (nint)values;
            this.receiveEventArgs.ValueCount = valueCount;
            this.receiveEventArgs.EventKind = eventKind;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreReceiveEvent, this.receiveEventArgs);

            returnValue = (AtkValue*)this.receiveEventArgs.ReturnValue;
            values = (AtkValue*)this.receiveEventArgs.AtkValues;
            valueCount = this.receiveEventArgs.ValueCount;
            eventKind = this.receiveEventArgs.EventKind;

            try
            {
                result = this.OriginalVirtualTable->ReceiveEvent(thisPtr, returnValue, values, valueCount, eventKind);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Agent ReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostReceiveEvent, this.receiveEventArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentReceiveEvent.");
        }

        return result;
    }

    private AtkValue* OnAgentFilteredReceiveEvent(AgentInterface* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        AtkValue* result = null;

        try
        {
            this.LogEvent(EnableLogging);

            this.filteredReceiveEventArgs.Agent = thisPtr;
            this.filteredReceiveEventArgs.AgentId = this.agentId;
            this.filteredReceiveEventArgs.ReturnValue = (nint)returnValue;
            this.filteredReceiveEventArgs.AtkValues = (nint)values;
            this.filteredReceiveEventArgs.ValueCount = valueCount;
            this.filteredReceiveEventArgs.EventKind = eventKind;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreReceiveFilteredEvent, this.filteredReceiveEventArgs);

            returnValue = (AtkValue*)this.filteredReceiveEventArgs.ReturnValue;
            values = (AtkValue*)this.filteredReceiveEventArgs.AtkValues;
            valueCount = this.filteredReceiveEventArgs.ValueCount;
            eventKind = this.filteredReceiveEventArgs.EventKind;

            try
            {
                result = this.OriginalVirtualTable->ReceiveEvent2(thisPtr, returnValue, values, valueCount, eventKind);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Agent FilteredReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostReceiveFilteredEvent, this.filteredReceiveEventArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentFilteredReceiveEvent.");
        }

        return result;
    }

    private void OnAgentShow(AgentInterface* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.showArgs.Agent = thisPtr;
            this.showArgs.AgentId = this.agentId;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreShow, this.showArgs);

            try
            {
                this.OriginalVirtualTable->Show(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Show. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostShow, this.showArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentShow.");
        }
    }

    private void OnAgentHide(AgentInterface* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.hideArgs.Agent = thisPtr;
            this.hideArgs.AgentId = this.agentId;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreHide, this.hideArgs);

            try
            {
                this.OriginalVirtualTable->Hide(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Hide. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostHide, this.hideArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentHide.");
        }
    }

    private void OnAgentUpdate(AgentInterface* thisPtr, uint frameCount)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.updateArgs.Agent = thisPtr;
            this.updateArgs.AgentId = this.agentId;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreUpdate, this.updateArgs);

            try
            {
                this.OriginalVirtualTable->Update(thisPtr, frameCount);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Update. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostUpdate, this.updateArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentUpdate.");
        }
    }

    private void OnAgentGameEvent(AgentInterface* thisPtr, AgentInterface.GameEvent gameEvent)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.gameEventArgs.Agent = thisPtr;
            this.gameEventArgs.AgentId = this.agentId;
            this.gameEventArgs.GameEvent = (int)gameEvent;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreGameEvent, this.gameEventArgs);

            gameEvent = (AgentInterface.GameEvent)this.gameEventArgs.GameEvent;

            try
            {
                this.OriginalVirtualTable->OnGameEvent(thisPtr, gameEvent);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnGameEvent. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostGameEvent, this.gameEventArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentGameEvent.");
        }
    }

    private void OnAgentLevelChange(AgentInterface* thisPtr, byte classJobId, ushort level)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.levelChangeArgs.Agent = thisPtr;
            this.levelChangeArgs.AgentId = this.agentId;
            this.levelChangeArgs.ClassJobId = classJobId;
            this.levelChangeArgs.Level = level;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreLevelChange, this.levelChangeArgs);

            classJobId = this.levelChangeArgs.ClassJobId;
            level = this.levelChangeArgs.Level;

            try
            {
                this.OriginalVirtualTable->OnLevelChange(thisPtr, classJobId, level);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnLevelChange. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostLevelChange, this.levelChangeArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAgentLevelChange.");
        }
    }

    private void OnClassJobChange(AgentInterface* thisPtr, byte classJobId)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.classJobChangeArgs.Agent = thisPtr;
            this.classJobChangeArgs.AgentId = this.agentId;
            this.classJobChangeArgs.ClassJobId = classJobId;

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PreClassJobChange, this.classJobChangeArgs);

            classJobId = this.classJobChangeArgs.ClassJobId;

            try
            {
                this.OriginalVirtualTable->OnClassJobChange(thisPtr, classJobId);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnClassJobChange. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AgentEvent.PostClassJobChange, this.classJobChangeArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnClassJobChange.");
        }
    }

    [Conditional("DEBUG")]
    private void LogEvent(bool loggingEnabled, [CallerMemberName] string caller = "")
    {
        if (loggingEnabled)
        {
            // Manually disable the really spammy log events, you can comment this out if you need to debug them.
            if (caller is "OnAgentUpdate" || this.agentId is AgentId.PadMouseMode)
                return;

            Log.Debug($"[{caller}]: {this.agentId}");
        }
    }
}

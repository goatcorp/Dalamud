using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Gui;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Class to manage creating and cleaning up events per-plugin.
/// </summary>
internal unsafe class PluginEventController : IDisposable
{
    private static readonly ModuleLog Log = new("AddonEventManager");

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginEventController"/> class.
    /// </summary>
    public PluginEventController()
    {
        this.EventListener = new AddonEventListener(this.PluginEventListHandler);
    }

    private AddonEventListener EventListener { get; init; }

    private List<AddonEventEntry> Events { get; } = new();

    /// <summary>
    /// Adds a tracked event.
    /// </summary>
    /// <param name="atkUnitBase">The Parent addon for the event.</param>
    /// <param name="atkResNode">The Node for the event.</param>
    /// <param name="atkEventType">The Event Type.</param>
    /// <param name="handler">The delegate to call when invoking this event.</param>
    /// <returns>IAddonEventHandle used to remove the event.</returns>
    [Obsolete("Use AddEvent that uses AddonEventDelegate instead of AddonEventHandler")]
    public IAddonEventHandle AddEvent(nint atkUnitBase, nint atkResNode, AddonEventType atkEventType, IAddonEventManager.AddonEventHandler handler)
    {
        var node = (AtkResNode*)atkResNode;
        var addon = (AtkUnitBase*)atkUnitBase;
        var eventType = (AtkEventType)atkEventType;
        var eventId = this.GetNextParamKey();
        var eventGuid = Guid.NewGuid();

        var eventHandle = new AddonEventHandle
        {
            AddonName = addon->NameString,
            ParamKey = eventId,
            EventType = atkEventType,
            EventGuid = eventGuid,
        };

        var eventEntry = new AddonEventEntry
        {
            Addon = atkUnitBase,
            Handler = handler,
            Delegate = null,
            Node = atkResNode,
            EventType = atkEventType,
            ParamKey = eventId,
            Handle = eventHandle,
        };

        Log.Verbose($"Adding Event. {eventEntry.LogString}");
        this.EventListener.RegisterEvent(addon, node, eventType, eventId);
        this.Events.Add(eventEntry);

        return eventHandle;
    }

    /// <summary>
    /// Adds a tracked event.
    /// </summary>
    /// <param name="atkUnitBase">The Parent addon for the event.</param>
    /// <param name="atkResNode">The Node for the event.</param>
    /// <param name="atkEventType">The Event Type.</param>
    /// <param name="eventDelegate">The delegate to call when invoking this event.</param>
    /// <returns>IAddonEventHandle used to remove the event.</returns>
    public IAddonEventHandle AddEvent(nint atkUnitBase, nint atkResNode, AddonEventType atkEventType, IAddonEventManager.AddonEventDelegate eventDelegate)
    {
        var node = (AtkResNode*)atkResNode;
        var addon = (AtkUnitBase*)atkUnitBase;
        var eventType = (AtkEventType)atkEventType;
        var eventId = this.GetNextParamKey();
        var eventGuid = Guid.NewGuid();

        var eventHandle = new AddonEventHandle
        {
            AddonName = addon->NameString,
            ParamKey = eventId,
            EventType = atkEventType,
            EventGuid = eventGuid,
        };

        var eventEntry = new AddonEventEntry
        {
            Addon = atkUnitBase,
            Delegate = eventDelegate,
            Handler = null,
            Node = atkResNode,
            EventType = atkEventType,
            ParamKey = eventId,
            Handle = eventHandle,
        };

        Log.Verbose($"Adding Event. {eventEntry.LogString}");
        this.EventListener.RegisterEvent(addon, node, eventType, eventId);
        this.Events.Add(eventEntry);

        return eventHandle;
    }

    /// <summary>
    /// Removes a tracked event, also attempts to un-attach the event from native.
    /// </summary>
    /// <param name="handle">Unique ID of the event to remove.</param>
    public void RemoveEvent(IAddonEventHandle handle)
    {
        if (this.Events.FirstOrDefault(registeredEvent => registeredEvent.Handle == handle) is not { } targetEvent) return;

        Log.Verbose($"Removing Event. {targetEvent.LogString}");
        this.TryRemoveEventFromNative(targetEvent);
        this.Events.Remove(targetEvent);
    }

    /// <summary>
    /// Removes all events attached to the specified addon.
    /// </summary>
    /// <param name="addonName">Addon name to remove events from.</param>
    public void RemoveForAddon(string addonName)
    {
        if (this.Events.Where(entry => entry.AddonName == addonName).ToList() is { Count: not 0 } events)
        {
            Log.Verbose($"Addon: {addonName} is Finalizing, removing {events.Count} events.");

            foreach (var registeredEvent in events)
            {
                this.RemoveEvent(registeredEvent.Handle);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var registeredEvent in this.Events.ToList())
        {
            this.RemoveEvent(registeredEvent.Handle);
        }

        this.EventListener.Dispose();
    }

    private uint GetNextParamKey()
    {
        for (var i = 0u; i < uint.MaxValue; ++i)
        {
            if (this.Events.All(registeredEvent => registeredEvent.ParamKey != i)) return i;
        }

        throw new OverflowException($"uint.MaxValue number of ParamKeys used for this event controller.");
    }

    /// <summary>
    /// Attempts to remove a tracked event from native UI.
    /// This method performs several safety checks to only remove events from a still active addon.
    /// If any of these checks fail, it likely means the native UI already cleaned up the event, and we don't have to worry about them.
    /// </summary>
    /// <param name="eventEntry">Event entry to remove.</param>
    private void TryRemoveEventFromNative(AddonEventEntry eventEntry)
    {
        // Is the eventEntry addon valid?
        if (eventEntry.AddonName is AddonEventEntry.InvalidAddonName) return;

        // Is an addon with the same name active?
        var currentAddonPointer = Service<GameGui>.Get().GetAddonByName(eventEntry.AddonName);
        if (currentAddonPointer == nint.Zero) return;

        // Is our stored addon pointer the same as the active addon pointer?
        if (currentAddonPointer != eventEntry.Addon) return;

        // Make sure the addon is not unloaded
        var atkUnitBase = (AtkUnitBase*)currentAddonPointer;
        if (atkUnitBase->UldManager.LoadedState == AtkLoadState.Unloaded) return;

        // Does this addon contain the node this event is for? (by address)
        var nodeFound = false;
        foreach (var node in atkUnitBase->UldManager.Nodes)
        {
            // If this node matches our node, then we know our node is still valid.
            if ((nint)node.Value == eventEntry.Node)
            {
                nodeFound = true;
                break;
            }
        }

        // If we didn't find the node, we can't remove the event.
        if (!nodeFound) return;

        // Does the node have a registered event matching the parameters we have?
        var atkResNode = (AtkResNode*)eventEntry.Node;
        var eventType = (AtkEventType)eventEntry.EventType;
        var currentEvent = atkResNode->AtkEventManager.Event;
        var eventFound = false;
        while (currentEvent is not null)
        {
            var paramKeyMatches = currentEvent->Param == eventEntry.ParamKey;
            var eventListenerAddressMatches = (nint)currentEvent->Listener == this.EventListener.Address;
            var eventTypeMatches = currentEvent->State.EventType == eventType;

            if (paramKeyMatches && eventListenerAddressMatches && eventTypeMatches)
            {
                eventFound = true;
                break;
            }

            // Move to the next event.
            currentEvent = currentEvent->NextEvent;
        }

        // If we didn't find the event, we can't remove the event.
        if (!eventFound) return;

        // We have a valid addon, valid node, valid event, and valid key.
        this.EventListener.UnregisterEvent(atkResNode, eventType, eventEntry.ParamKey);
    }

    [Api13ToDo("Remove invoke from eventInfo.Handler, and remove nullability from eventInfo.Delegate?.Invoke")]
    private void PluginEventListHandler(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventPtr, AtkEventData* eventDataPtr)
    {
        try
        {
            if (eventPtr is null) return;
            if (this.Events.FirstOrDefault(handler => handler.ParamKey == eventParam) is not { } eventInfo) return;

            // We stored the AtkUnitBase* in EventData->Node, and EventData->Target contains the node that triggered the event.
            eventInfo.Handler?.Invoke((AddonEventType)eventType, (nint)eventPtr->Node, (nint)eventPtr->Target);

            eventInfo.Delegate?.Invoke((AddonEventType)eventType, new AddonEventData
            {
                AddonPointer = (nint)eventPtr->Node,
                NodeTargetPointer = (nint)eventPtr->Target,
                AtkEventDataPointer = (nint)eventDataPtr,
                AtkEventListener = (nint)self,
                AtkEventType = (AddonEventType)eventType,
                Param = eventParam,
                AtkEventPointer = (nint)eventPtr,
            });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Exception in PluginEventList custom event invoke.");
        }
    }
}

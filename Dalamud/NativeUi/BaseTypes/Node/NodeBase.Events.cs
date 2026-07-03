using System.Collections.Generic;

using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

using EventHandlerInfo = Dalamud.NativeUi.Classes.EventHandlerInfo;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// .
/// </summary>
internal abstract unsafe partial class NodeBase
{
    private readonly Dictionary<AtkEventType, EventHandlerInfo> eventHandlers = [];
    private CustomEventListener? nodeEventListener;

    /// <summary>
    /// Gets or sets a value indicating whether this node should show a clickable cursor when hovered.
    /// </summary>
    public bool ShowClickableCursor
    {
        get => this.DrawFlags.HasFlag(DrawFlags.ClickableCursor);
        set
        {
            if (value)
            {
                this.DrawFlags |= DrawFlags.ClickableCursor;
            }
            else
            {
                this.DrawFlags &= ~DrawFlags.ClickableCursor;
            }
        }
    }

    /// <summary>
    /// Adds a no-arg callback action for the specified event type.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="callback">No arg callback.</param>
    public void AddEvent(AtkEventType eventType, Action callback)
    {
        this.nodeEventListener ??= new CustomEventListener(this.HandleEvents);

        this.SetNodeEventFlags(eventType);

        if (this.eventHandlers.TryAdd(eventType, new EventHandlerInfo { OnActionDelegate = callback }))
        {
            Log.Verbose("[{AtkEventType}] Registered for {GetType} [{ResNode:X}]", eventType, this.GetType(), (nint)this.ResNode);
            ResNode->AtkEventManager.RegisterEvent(eventType, 0, this, this, this.nodeEventListener, false);
        }
        else
        {
            this.eventHandlers[eventType].OnActionDelegate += callback;
        }
    }

    /// <summary>
    /// Adds a ReceiveEvent styled callback for the specified event type.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="callback">Full args callback.</param>
    public void AddEvent(AtkEventType eventType, AtkEventListener.Delegates.ReceiveEvent callback)
    {
        this.nodeEventListener ??= new CustomEventListener(this.HandleEvents);

        this.SetNodeEventFlags(eventType);

        if (this.eventHandlers.TryAdd(eventType, new EventHandlerInfo { OnReceiveEventDelegate = callback }))
        {
            Log.Verbose("[{AtkEventType}] Registered for {GetType} [{ResNode:X}]", eventType, this.GetType(), (nint)this.ResNode);
            ResNode->AtkEventManager.RegisterEvent(eventType, 0, this, this, this.nodeEventListener, false);
        }
        else
        {
            this.eventHandlers[eventType].OnReceiveEventDelegate += callback;
        }
    }

    /// <summary>
    /// Removes all event handlers for the specified event type.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    public void RemoveEvent(AtkEventType eventType)
    {
        if (this.nodeEventListener is null) return;

        if (this.eventHandlers.Remove(eventType))
        {
            Log.Verbose("[{AtkEventType}] Unregistered from {GetType} [{ResNode:X}]", eventType, this.GetType(), (nint)this.ResNode);
            ResNode->AtkEventManager.UnregisterEvent(eventType, 0, this.nodeEventListener, false);
        }

        // If we have removed the last event, free the event listener
        if (this.eventHandlers.Keys.Count is 0)
        {
            this.nodeEventListener.Dispose();
            this.nodeEventListener = null;
        }
    }

    /// <summary>
    /// Removes the provided callback from the list of listeners for the specified event type.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="callback">Callback.</param>
    public void RemoveEvent(AtkEventType eventType, Action callback)
    {
        if (this.nodeEventListener is null) return;

        if (this.eventHandlers.TryGetValue(eventType, out var handler))
        {
            handler.OnActionDelegate -= callback;

            if (handler.OnReceiveEventDelegate is null && handler.OnActionDelegate is null)
            {
                this.RemoveEvent(eventType);
            }
        }
    }

    /// <summary>
    /// Removes the provided callback from the list of listeners for the specified event type.
    /// </summary>
    /// <param name="eventType">Event type.</param>
    /// <param name="callback">Callback.</param>
    public void RemoveEvent(AtkEventType eventType, AtkEventListener.Delegates.ReceiveEvent callback)
    {
        if (this.nodeEventListener is null) return;

        if (this.eventHandlers.TryGetValue(eventType, out var handler))
        {
            handler.OnReceiveEventDelegate -= callback;

            if (handler.OnReceiveEventDelegate is null && handler.OnActionDelegate is null)
            {
                this.RemoveEvent(eventType);
            }
        }
    }

    private void DisposeEvents()
    {
        if (this.nodeEventListener is not null)
        {
            ResNode->AtkEventManager.UnregisterEvent(AtkEventType.UnregisterAll, 0, this.nodeEventListener, false);
        }

        this.eventHandlers.Clear();

        this.nodeEventListener?.Dispose();
        this.nodeEventListener = null;
    }

    private void HandleEvents(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (!this.IsVisible) return;

        if (this.eventHandlers.TryGetValue(eventType, out var handler))
        {
            foreach (var noArgHandler in Delegate.EnumerateInvocationList(handler.OnActionDelegate))
            {
                try
                {
                    noArgHandler();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Exception while handing an event callback using no arg callback.");
                }
            }

            foreach (var argHandler in Delegate.EnumerateInvocationList(handler.OnReceiveEventDelegate))
            {
                try
                {
                    argHandler(thisPtr, eventType, eventParam, atkEvent, atkEventData);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Exception while handling an event callback using full args callback.");
                }
            }
        }
    }

    private void SetNodeEventFlags(AtkEventType eventType)
    {
        switch (eventType)
        {
            // Hover events need to propagate down to trigger various timelines
            case AtkEventType.MouseOver:
            case AtkEventType.MouseOut:
            case AtkEventType.MouseWheel:
                this.AddNodeFlags(NodeFlags.EmitsEvents, NodeFlags.RespondToMouse);
                break;

            // Any kind of direct interaction should be a blocking event
            // set HasCollision to prevent events from propagating
            case AtkEventType.MouseDown:
            case AtkEventType.MouseUp:
            case AtkEventType.MouseMove:
            case AtkEventType.MouseClick:
                this.AddNodeFlags(NodeFlags.EmitsEvents, NodeFlags.RespondToMouse, NodeFlags.HasCollision);
                break;

            // ButtonClick is mostly used as an event that native calls back to, when interacting with buttons
            // We do not want to re-emit, or block events in this case
            case AtkEventType.ButtonClick:
                break;
        }
    }
}

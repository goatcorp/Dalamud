using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Event listener class for managing custom events.
/// </summary>
// Custom event handler tech provided by Pohky, implemented by MidoriKami
internal unsafe class AddonEventListener : IDisposable
{
    private ReceiveEventDelegate? receiveEventDelegate;
    
    private AtkEventListener* eventListener;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonEventListener"/> class.
    /// </summary>
    /// <param name="eventHandler">The managed handler to send events to.</param>
    public AddonEventListener(ReceiveEventDelegate eventHandler)
    {
        this.receiveEventDelegate = eventHandler;

        this.eventListener = (AtkEventListener*)Marshal.AllocHGlobal(sizeof(AtkEventListener));
        this.eventListener->vtbl = (void*)Marshal.AllocHGlobal(sizeof(void*) * 3);
        this.eventListener->vfunc[0] = (delegate* unmanaged<void>)&NullSub;
        this.eventListener->vfunc[1] = (delegate* unmanaged<void>)&NullSub;
        this.eventListener->vfunc[2] = (void*)Marshal.GetFunctionPointerForDelegate(this.receiveEventDelegate);
    }

    /// <summary>
    /// Delegate for receiving custom events.
    /// </summary>
    /// <param name="self">Pointer to the event listener.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventParam">Unique Id for this event.</param>
    /// <param name="eventData">Event Data.</param>
    /// <param name="unknown">Unknown Parameter.</param>
    public delegate void ReceiveEventDelegate(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, nint unknown);
  
    /// <summary>
    /// Gets the address of this listener.
    /// </summary>
    public nint Address => (nint)this.eventListener;
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (this.eventListener is null) return;
        
        Marshal.FreeHGlobal((nint)this.eventListener->vtbl);
        Marshal.FreeHGlobal((nint)this.eventListener);

        this.eventListener = null;
        this.receiveEventDelegate = null;
    }

    /// <summary>
    /// Register an event to this event handler.
    /// </summary>
    /// <param name="addon">Addon that triggers this event.</param>
    /// <param name="node">Node to attach event to.</param>
    /// <param name="eventType">Event type to trigger this event.</param>
    /// <param name="param">Unique id for this event.</param>
    public void RegisterEvent(AtkUnitBase* addon, AtkResNode* node, AtkEventType eventType, uint param)
    {
        if (node is null) return;

        Service<Framework>.Get().RunOnFrameworkThread(() =>
        {
            node->AddEvent(eventType, param, this.eventListener, (AtkResNode*)addon, false);
        });
    }

    /// <summary>
    /// Unregister an event from this event handler.
    /// </summary>
    /// <param name="node">Node to remove the event from.</param>
    /// <param name="eventType">Event type that this event is for.</param>
    /// <param name="param">Unique id for this event.</param>
    public void UnregisterEvent(AtkResNode* node, AtkEventType eventType, uint param)
    {
        if (node is null) return;

        Service<Framework>.Get().RunOnFrameworkThread(() =>
        {
            node->RemoveEvent(eventType, param, this.eventListener, false);
        });
    }
    
    [UnmanagedCallersOnly]
    private static void NullSub()
    {
        /* do nothing */
    }
}

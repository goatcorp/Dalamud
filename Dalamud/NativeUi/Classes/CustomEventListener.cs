using System.Runtime.InteropServices;

using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Managed wrapper around a AtkEventListener.
/// This class is intended to be used to wire native events to managed event handlers.
/// </summary>
/// <remarks>
/// This version is specifically to be used for ATK/UI events.
/// </remarks>
internal unsafe class CustomEventListener : IDisposable
{
    /// <summary>
    /// Log for logging things.
    /// </summary>
    private static readonly ModuleLog Log = new("CustomEventListener");

    private readonly AtkEventListener* eventListener;

    private AtkEventListener.Delegates.ReceiveEvent? receiveEventDelegate;
    private AtkEventListener.Delegates.ReceiveEvent? receiveEventWrapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEventListener"/> class.
    /// </summary>
    /// <param name="eventHandler">The callback delegate to invoke when an event is received.</param>
    public CustomEventListener(AtkEventListener.Delegates.ReceiveEvent eventHandler)
    {
        this.receiveEventDelegate = eventHandler;

        this.receiveEventWrapper = this.ReceiveEventWrapper;

        this.eventListener = (AtkEventListener*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkEventListener), 8);
        this.eventListener->VirtualTable = (AtkEventListener.AtkEventListenerVirtualTable*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(void*) * 3, 8);
        this.eventListener->VirtualTable->Dtor = (delegate* unmanaged<AtkEventListener*, byte, AtkEventListener*>)(delegate* unmanaged<void>)&NullSub;
        this.eventListener->VirtualTable->ReceiveGlobalEvent = (delegate* unmanaged<AtkEventListener*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)(delegate* unmanaged<void>)&NullSub;
        this.eventListener->VirtualTable->ReceiveEvent = (delegate* unmanaged<AtkEventListener*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)Marshal.GetFunctionPointerForDelegate(this.receiveEventWrapper);
    }

    /// <summary>
    /// Public implicit operator to be able to use this instance as a AtkEventListener* directly.
    /// </summary>
    /// <param name="listener">Event listener to convert.</param>
    public static implicit operator AtkEventListener*(CustomEventListener listener) => listener.eventListener;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (this.eventListener is null) return;

        IMemorySpace.Free(this.eventListener->VirtualTable, (ulong)sizeof(void*) * 3);
        IMemorySpace.Free(this.eventListener);

        this.receiveEventDelegate = null;
        this.receiveEventWrapper = null;
    }

    [UnmanagedCallersOnly]
    private static void NullSub()
    {
    }

    private void ReceiveEventWrapper(AtkEventListener* thisPtr, AtkEventType eventType, int param, AtkEvent* eventObject, AtkEventData* data)
    {
        try
        {
            this.receiveEventDelegate?.Invoke(thisPtr, eventType, param, eventObject, data);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception while handling an event passed to a CustomEventListener.ReceiveEvent");
        }
    }
}

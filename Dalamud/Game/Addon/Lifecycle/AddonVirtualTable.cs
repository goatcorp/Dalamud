using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Represents a class that holds references to an addons original and modified virtual table entries.
/// </summary>
internal unsafe class AddonVirtualTable : IDisposable
{
    // This need to be at minimum the largest virtual table size of all addons
    // Copying extra entries is not problematic, and is considered safe.
    private const int VirtualTableEntryCount = 200;

    private const bool EnableLogging = true;

    private static readonly ModuleLog Log = new("LifecycleVT");

    private readonly AddonLifecycle lifecycleService;

    // Each addon gets its own set of args that are used to mutate the original call when used in pre-calls
    private readonly AddonSetupArgs setupArgs = new();
    private readonly AddonArgs finalizeArgs = new();
    private readonly AddonArgs drawArgs = new();
    private readonly AddonArgs updateArgs = new();
    private readonly AddonRefreshArgs refreshArgs = new();
    private readonly AddonRequestedUpdateArgs requestedUpdateArgs = new();
    private readonly AddonReceiveEventArgs receiveEventArgs = new();
    private readonly AddonArgs openArgs = new();
    private readonly AddonCloseArgs closeArgs = new();
    private readonly AddonShowArgs showArgs = new();
    private readonly AddonHideArgs hideArgs = new();
    private readonly AddonArgs onMoveArgs = new();
    private readonly AddonArgs onMouseOverArgs = new();
    private readonly AddonArgs onMouseOutArgs = new();
    private readonly AddonArgs focusArgs = new();

    private readonly AtkUnitBase* atkUnitBase;

    private readonly AtkUnitBase.AtkUnitBaseVirtualTable* originalVirtualTable;
    private readonly AtkUnitBase.AtkUnitBaseVirtualTable* modifiedVirtualTable;

    // Pinned Function Delegates, as these functions get assigned to an unmanaged virtual table,
    // the CLR needs to know they are in use, or it will invalidate them causing random crashing.
    private readonly AtkUnitBase.Delegates.Dtor destructorFunction;
    private readonly AtkUnitBase.Delegates.OnSetup onSetupFunction;
    private readonly AtkUnitBase.Delegates.Finalizer finalizerFunction;
    private readonly AtkUnitBase.Delegates.Draw drawFunction;
    private readonly AtkUnitBase.Delegates.Update updateFunction;
    private readonly AtkUnitBase.Delegates.OnRefresh onRefreshFunction;
    private readonly AtkUnitBase.Delegates.OnRequestedUpdate onRequestedUpdateFunction;
    private readonly AtkUnitBase.Delegates.ReceiveEvent onReceiveEventFunction;
    private readonly AtkUnitBase.Delegates.Open openFunction;
    private readonly AtkUnitBase.Delegates.Close closeFunction;
    private readonly AtkUnitBase.Delegates.Show showFunction;
    private readonly AtkUnitBase.Delegates.Hide hideFunction;
    private readonly AtkUnitBase.Delegates.OnMove onMoveFunction;
    private readonly AtkUnitBase.Delegates.OnMouseOver onMouseOverFunction;
    private readonly AtkUnitBase.Delegates.OnMouseOut onMouseOutFunction;
    private readonly AtkUnitBase.Delegates.Focus focusFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonVirtualTable"/> class.
    /// </summary>
    /// <param name="addon">AtkUnitBase* for the addon to replace the table of.</param>
    /// <param name="lifecycleService">Reference to AddonLifecycle service to callback and invoke listeners.</param>
    internal AddonVirtualTable(AtkUnitBase* addon, AddonLifecycle lifecycleService)
    {
        this.atkUnitBase = addon;
        this.lifecycleService = lifecycleService;

        // Save original virtual table
        this.originalVirtualTable = addon->VirtualTable;

        // Create copy of original table
        // Note this will copy any derived/overriden functions that this specific addon has.
        // Note: currently there are 73 virtual functions, but there's no harm in copying more for when they add new virtual functions to the game
        this.modifiedVirtualTable = (AtkUnitBase.AtkUnitBaseVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * VirtualTableEntryCount, 8);
        NativeMemory.Copy(addon->VirtualTable, this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);

        // Overwrite the addons existing virtual table with our own
        addon->VirtualTable = this.modifiedVirtualTable;

        // Pin each of our listener functions
        this.destructorFunction = this.OnAddonDestructor;
        this.onSetupFunction = this.OnAddonSetup;
        this.finalizerFunction = this.OnAddonFinalize;
        this.drawFunction = this.OnAddonDraw;
        this.updateFunction = this.OnAddonUpdate;
        this.onRefreshFunction = this.OnAddonRefresh;
        this.onRequestedUpdateFunction = this.OnRequestedUpdate;
        this.onReceiveEventFunction = this.OnAddonReceiveEvent;
        this.openFunction = this.OnAddonOpen;
        this.closeFunction = this.OnAddonClose;
        this.showFunction = this.OnAddonShow;
        this.hideFunction = this.OnAddonHide;
        this.onMoveFunction = this.OnMove;
        this.onMouseOverFunction = this.OnMouseOver;
        this.onMouseOutFunction = this.OnMouseOut;
        this.focusFunction = this.OnFocus;

        // Overwrite specific virtual table entries
        this.modifiedVirtualTable->Dtor = (delegate* unmanaged<AtkUnitBase*, byte, AtkEventListener*>)Marshal.GetFunctionPointerForDelegate(this.destructorFunction);
        this.modifiedVirtualTable->OnSetup = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, void>)Marshal.GetFunctionPointerForDelegate(this.onSetupFunction);
        this.modifiedVirtualTable->Finalizer = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.finalizerFunction);
        this.modifiedVirtualTable->Draw = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.drawFunction);
        this.modifiedVirtualTable->Update = (delegate* unmanaged<AtkUnitBase*, float, void>)Marshal.GetFunctionPointerForDelegate(this.updateFunction);
        this.modifiedVirtualTable->OnRefresh = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, bool>)Marshal.GetFunctionPointerForDelegate(this.onRefreshFunction);
        this.modifiedVirtualTable->OnRequestedUpdate = (delegate* unmanaged<AtkUnitBase*, NumberArrayData**, StringArrayData**, void>)Marshal.GetFunctionPointerForDelegate(this.onRequestedUpdateFunction);
        this.modifiedVirtualTable->ReceiveEvent = (delegate* unmanaged<AtkUnitBase*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)Marshal.GetFunctionPointerForDelegate(this.onReceiveEventFunction);
        this.modifiedVirtualTable->Open = (delegate* unmanaged<AtkUnitBase*, uint, bool>)Marshal.GetFunctionPointerForDelegate(this.openFunction);
        this.modifiedVirtualTable->Close = (delegate* unmanaged<AtkUnitBase*, bool, bool>)Marshal.GetFunctionPointerForDelegate(this.closeFunction);
        this.modifiedVirtualTable->Show = (delegate* unmanaged<AtkUnitBase*, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.showFunction);
        this.modifiedVirtualTable->Hide = (delegate* unmanaged<AtkUnitBase*, bool, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.hideFunction);
        this.modifiedVirtualTable->OnMove = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMoveFunction);
        this.modifiedVirtualTable->OnMouseOver = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMouseOverFunction);
        this.modifiedVirtualTable->OnMouseOut = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMouseOutFunction);
        this.modifiedVirtualTable->Focus = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.focusFunction);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Ensure restoration is done atomically.
        Interlocked.Exchange(ref *(nint*)&this.atkUnitBase->VirtualTable, (nint)this.originalVirtualTable);
        IMemorySpace.Free(this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);
    }

    private AtkEventListener* OnAddonDestructor(AtkUnitBase* thisPtr, byte freeFlags)
    {
        this.LogEvent(EnableLogging);

        var result = this.originalVirtualTable->Dtor(thisPtr, freeFlags);

        if ((freeFlags & 1) == 1)
        {
            IMemorySpace.Free(this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);
            AddonLifecycle.AllocatedTables.Remove(this);
        }

        return result;
    }

    private void OnAddonSetup(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        this.LogEvent(EnableLogging);

        this.setupArgs.Addon = addon;
        this.setupArgs.AtkValueCount = valueCount;
        this.setupArgs.AtkValues = (nint)values;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreSetup, this.setupArgs);

        valueCount = this.setupArgs.AtkValueCount;
        values = (AtkValue*)this.setupArgs.AtkValues;

        try
        {
            this.originalVirtualTable->OnSetup(addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonSetup. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostSetup, this.setupArgs);
    }

    private void OnAddonFinalize(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.finalizeArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFinalize, this.finalizeArgs);

        try
        {
            this.originalVirtualTable->Finalizer(thisPtr);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonFinalize. This may be a bug in the game or another plugin hooking this method.");
        }
    }

    private void OnAddonDraw(AtkUnitBase* addon)
    {
        this.LogEvent(EnableLogging);

        this.drawArgs.Addon = addon;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreDraw, this.drawArgs);

        try
        {
            this.originalVirtualTable->Draw(addon);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonDraw. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostDraw, this.drawArgs);
    }

    private void OnAddonUpdate(AtkUnitBase* addon, float delta)
    {
        this.LogEvent(EnableLogging);

        this.updateArgs.Addon = addon;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreUpdate, this.updateArgs);

        // Note: Do not pass or allow manipulation of delta.
        // It's realistically not something that should be needed.

        try
        {
            this.originalVirtualTable->Update(addon,  delta);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostUpdate, this.updateArgs);
    }

    private bool OnAddonRefresh(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.refreshArgs.Addon = addon;
        this.refreshArgs.AtkValueCount = valueCount;
        this.refreshArgs.AtkValues = (nint)values;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreRefresh, this.refreshArgs);

        valueCount = this.refreshArgs.AtkValueCount;
        values = (AtkValue*)this.refreshArgs.AtkValues;

        try
        {
            result = this.originalVirtualTable->OnRefresh(addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRefresh. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRefresh, this.refreshArgs);
        return result;
    }

    private void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        this.LogEvent(EnableLogging);

        this.requestedUpdateArgs.Addon = addon;
        this.requestedUpdateArgs.NumberArrayData = (nint)numberArrayData;
        this.requestedUpdateArgs.StringArrayData = (nint)stringArrayData;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreRequestedUpdate, this.requestedUpdateArgs);

        numberArrayData = (NumberArrayData**)this.requestedUpdateArgs.NumberArrayData;
        stringArrayData = (StringArrayData**)this.requestedUpdateArgs.StringArrayData;

        try
        {
            this.originalVirtualTable->OnRequestedUpdate(addon, numberArrayData, stringArrayData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRequestedUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRequestedUpdate, this.requestedUpdateArgs);
    }

    private void OnAddonReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.LogEvent(EnableLogging);

        this.receiveEventArgs.Addon = (nint)addon;
        this.receiveEventArgs.AtkEventType = (byte)eventType;
        this.receiveEventArgs.EventParam = eventParam;
        this.receiveEventArgs.AtkEvent = (IntPtr)atkEvent;
        this.receiveEventArgs.AtkEventData = (nint)atkEventData;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreReceiveEvent, this.receiveEventArgs);

        eventType = (AtkEventType)this.receiveEventArgs.AtkEventType;
        eventParam = this.receiveEventArgs.EventParam;
        atkEvent = (AtkEvent*)this.receiveEventArgs.AtkEvent;
        atkEventData = (AtkEventData*)this.receiveEventArgs.AtkEventData;

        try
        {
            this.originalVirtualTable->ReceiveEvent(addon, eventType, eventParam, atkEvent, atkEventData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostReceiveEvent, this.receiveEventArgs);
    }

    private bool OnAddonOpen(AtkUnitBase* thisPtr, uint depthLayer)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.openArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreOpen, this.openArgs);

        try
        {
            result = this.originalVirtualTable->Open(thisPtr, depthLayer);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonOpen. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostOpen, this.openArgs);

        return result;
    }

    private bool OnAddonClose(AtkUnitBase* thisPtr, bool fireCallback)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.closeArgs.Addon = thisPtr;
        this.closeArgs.FireCallback = fireCallback;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreClose, this.closeArgs);

        fireCallback = this.closeArgs.FireCallback;

        try
        {
            result = this.originalVirtualTable->Close(thisPtr, fireCallback);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonClose. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostClose, this.closeArgs);

        return result;
    }

    private void OnAddonShow(AtkUnitBase* thisPtr, bool silenceOpenSoundEffect, uint unsetShowHideFlags)
    {
        this.LogEvent(EnableLogging);

        this.showArgs.Addon = thisPtr;
        this.showArgs.SilenceOpenSoundEffect = silenceOpenSoundEffect;
        this.showArgs.UnsetShowHideFlags = unsetShowHideFlags;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreShow, this.showArgs);

        silenceOpenSoundEffect = this.showArgs.SilenceOpenSoundEffect;
        unsetShowHideFlags = this.showArgs.UnsetShowHideFlags;

        try
        {
            this.originalVirtualTable->Show(thisPtr, silenceOpenSoundEffect, unsetShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonShow. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostShow, this.showArgs);
    }

    private void OnAddonHide(AtkUnitBase* thisPtr, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {
        this.LogEvent(EnableLogging);

        this.hideArgs.Addon = thisPtr;
        this.hideArgs.UnknownBool = unkBool;
        this.hideArgs.CallHideCallback = callHideCallback;
        this.hideArgs.SetShowHideFlags = setShowHideFlags;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreHide, this.hideArgs);

        unkBool = this.hideArgs.UnknownBool;
        callHideCallback = this.hideArgs.CallHideCallback;
        setShowHideFlags = this.hideArgs.SetShowHideFlags;

        try
        {
            this.originalVirtualTable->Hide(thisPtr, unkBool, callHideCallback, setShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonHide. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostHide, this.hideArgs);
    }

    private void OnMove(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.onMoveArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMove, this.onMoveArgs);

        try
        {
            this.originalVirtualTable->OnMove(thisPtr);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original OnMove. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMove, this.onMoveArgs);
    }

    private void OnMouseOver(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.onMouseOverArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMouseOver, this.onMouseOverArgs);

        try
        {
            this.originalVirtualTable->OnMouseOver(thisPtr);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original OnMouseOver. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMouseOver, this.onMouseOverArgs);
    }

    private void OnMouseOut(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.onMouseOutArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMouseOut, this.onMouseOutArgs);

        try
        {
            this.originalVirtualTable->OnMouseOut(thisPtr);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original OnMouseOut. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMouseOut, this.onMouseOutArgs);
    }

    private void OnFocus(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.focusArgs.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFocus, this.focusArgs);

        try
        {
            this.originalVirtualTable->Focus(thisPtr);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original OnFocus. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostFocus, this.focusArgs);
    }

    [Conditional("DEBUG")]
    private void LogEvent(bool loggingEnabled, [CallerMemberName] string caller = "")
    {
        if (loggingEnabled)
        {
            // Manually disable the really spammy log events, you can comment this out if you need to debug them.
            if (caller is "OnAddonUpdate" or "OnAddonDraw" or "OnAddonReceiveEvent" or "OnRequestedUpdate")
                return;

            Log.Debug($"[{caller}]: {this.atkUnitBase->NameString}");
        }
    }
}

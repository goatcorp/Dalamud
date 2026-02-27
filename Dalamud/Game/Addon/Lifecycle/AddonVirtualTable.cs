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

    private const bool EnableLogging = false;

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
    private readonly AddonFocusChangedArgs focusChangedArgs = new();

    private readonly AtkUnitBase* atkUnitBase;

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
    private readonly AtkUnitBase.Delegates.OnFocusChange onFocusChangeFunction;

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
        this.OriginalVirtualTable = addon->VirtualTable;

        // Create copy of original table
        // Note this will copy any derived/overriden functions that this specific addon has.
        // Note: currently there are 73 virtual functions, but there's no harm in copying more for when they add new virtual functions to the game
        this.ModifiedVirtualTable = (AtkUnitBase.AtkUnitBaseVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * VirtualTableEntryCount, 8);
        NativeMemory.Copy(addon->VirtualTable, this.ModifiedVirtualTable, 0x8 * VirtualTableEntryCount);

        // Overwrite the addons existing virtual table with our own
        addon->VirtualTable = this.ModifiedVirtualTable;

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
        this.onMoveFunction = this.OnAddonMove;
        this.onMouseOverFunction = this.OnAddonMouseOver;
        this.onMouseOutFunction = this.OnAddonMouseOut;
        this.focusFunction = this.OnAddonFocus;
        this.onFocusChangeFunction = this.OnAddonFocusChange;

        // Overwrite specific virtual table entries
        this.ModifiedVirtualTable->Dtor = (delegate* unmanaged<AtkUnitBase*, byte, AtkEventListener*>)Marshal.GetFunctionPointerForDelegate(this.destructorFunction);
        this.ModifiedVirtualTable->OnSetup = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, void>)Marshal.GetFunctionPointerForDelegate(this.onSetupFunction);
        this.ModifiedVirtualTable->Finalizer = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.finalizerFunction);
        this.ModifiedVirtualTable->Draw = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.drawFunction);
        this.ModifiedVirtualTable->Update = (delegate* unmanaged<AtkUnitBase*, float, void>)Marshal.GetFunctionPointerForDelegate(this.updateFunction);
        this.ModifiedVirtualTable->OnRefresh = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, bool>)Marshal.GetFunctionPointerForDelegate(this.onRefreshFunction);
        this.ModifiedVirtualTable->OnRequestedUpdate = (delegate* unmanaged<AtkUnitBase*, NumberArrayData**, StringArrayData**, void>)Marshal.GetFunctionPointerForDelegate(this.onRequestedUpdateFunction);
        this.ModifiedVirtualTable->ReceiveEvent = (delegate* unmanaged<AtkUnitBase*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)Marshal.GetFunctionPointerForDelegate(this.onReceiveEventFunction);
        this.ModifiedVirtualTable->Open = (delegate* unmanaged<AtkUnitBase*, uint, bool>)Marshal.GetFunctionPointerForDelegate(this.openFunction);
        this.ModifiedVirtualTable->Close = (delegate* unmanaged<AtkUnitBase*, bool, bool>)Marshal.GetFunctionPointerForDelegate(this.closeFunction);
        this.ModifiedVirtualTable->Show = (delegate* unmanaged<AtkUnitBase*, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.showFunction);
        this.ModifiedVirtualTable->Hide = (delegate* unmanaged<AtkUnitBase*, bool, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.hideFunction);
        this.ModifiedVirtualTable->OnMove = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMoveFunction);
        this.ModifiedVirtualTable->OnMouseOver = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMouseOverFunction);
        this.ModifiedVirtualTable->OnMouseOut = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.onMouseOutFunction);
        this.ModifiedVirtualTable->Focus = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.focusFunction);
        this.ModifiedVirtualTable->OnFocusChange = (delegate* unmanaged<AtkUnitBase*, bool, void>)Marshal.GetFunctionPointerForDelegate(this.onFocusChangeFunction);
    }

    /// <summary>
    /// Gets the original virtual table address for this addon.
    /// </summary>
    internal AtkUnitBase.AtkUnitBaseVirtualTable* OriginalVirtualTable { get; private set; }

    /// <summary>
    /// Gets the modified virtual address for this addon.
    /// </summary>
    internal AtkUnitBase.AtkUnitBaseVirtualTable* ModifiedVirtualTable { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Ensure restoration is done atomically.
        Interlocked.Exchange(ref *(nint*)&this.atkUnitBase->VirtualTable, (nint)this.OriginalVirtualTable);
        IMemorySpace.Free(this.ModifiedVirtualTable, 0x8 * VirtualTableEntryCount);
    }

    private AtkEventListener* OnAddonDestructor(AtkUnitBase* thisPtr, byte freeFlags)
    {
        AtkEventListener* result = null;

        try
        {
            this.LogEvent(EnableLogging);

            try
            {
                result = this.OriginalVirtualTable->Dtor(thisPtr, freeFlags);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Dtor. This may be a bug in the game or another plugin hooking this method.");
            }

            if ((freeFlags & 1) == 1)
            {
                IMemorySpace.Free(this.ModifiedVirtualTable, 0x8 * VirtualTableEntryCount);
                AddonLifecycle.AllocatedTables.Remove(this);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonDestructor.");
        }

        return result;
    }

    private void OnAddonSetup(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        try
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
                this.OriginalVirtualTable->OnSetup(addon, valueCount, values);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnSetup. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostSetup, this.setupArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonSetup.");
        }
    }

    private void OnAddonFinalize(AtkUnitBase* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.finalizeArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFinalize, this.finalizeArgs);

            try
            {
                this.OriginalVirtualTable->Finalizer(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Finalizer. This may be a bug in the game or another plugin hooking this method.");
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonFinalize.");
        }
    }

    private void OnAddonDraw(AtkUnitBase* addon)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.drawArgs.Addon = addon;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreDraw, this.drawArgs);

            try
            {
                this.OriginalVirtualTable->Draw(addon);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Draw. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostDraw, this.drawArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonDraw.");
        }
    }

    private void OnAddonUpdate(AtkUnitBase* addon, float delta)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.updateArgs.Addon = addon;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreUpdate, this.updateArgs);

            // Note: Do not pass or allow manipulation of delta.
            // It's realistically not something that should be needed.
            // And even if someone does, they are encouraged to hook Update themselves.

            try
            {
                this.OriginalVirtualTable->Update(addon,  delta);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Update. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostUpdate, this.updateArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonUpdate.");
        }
    }

    private bool OnAddonRefresh(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        var result = false;

        try
        {
            this.LogEvent(EnableLogging);

            this.refreshArgs.Addon = addon;
            this.refreshArgs.AtkValueCount = valueCount;
            this.refreshArgs.AtkValues = (nint)values;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreRefresh, this.refreshArgs);

            valueCount = this.refreshArgs.AtkValueCount;
            values = (AtkValue*)this.refreshArgs.AtkValues;

            try
            {
                result = this.OriginalVirtualTable->OnRefresh(addon, valueCount, values);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnRefresh. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRefresh, this.refreshArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonRefresh.");
        }

        return result;
    }

    private void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        try
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
                this.OriginalVirtualTable->OnRequestedUpdate(addon, numberArrayData, stringArrayData);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnRequestedUpdate. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRequestedUpdate, this.requestedUpdateArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnRequestedUpdate.");
        }
    }

    private void OnAddonReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        try
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
                this.OriginalVirtualTable->ReceiveEvent(addon, eventType, eventParam, atkEvent, atkEventData);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon ReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostReceiveEvent, this.receiveEventArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonReceiveEvent.");
        }
    }

    private bool OnAddonOpen(AtkUnitBase* thisPtr, uint depthLayer)
    {
        var result = false;

        try
        {
            this.LogEvent(EnableLogging);

            this.openArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreOpen, this.openArgs);

            try
            {
                result = this.OriginalVirtualTable->Open(thisPtr, depthLayer);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Open. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostOpen, this.openArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonOpen.");
        }

        return result;
    }

    private bool OnAddonClose(AtkUnitBase* thisPtr, bool fireCallback)
    {
        var result = false;

        try
        {
            this.LogEvent(EnableLogging);

            this.closeArgs.Addon = thisPtr;
            this.closeArgs.FireCallback = fireCallback;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreClose, this.closeArgs);

            fireCallback = this.closeArgs.FireCallback;

            try
            {
                result = this.OriginalVirtualTable->Close(thisPtr, fireCallback);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Close. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostClose, this.closeArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonClose.");
        }

        return result;
    }

    private void OnAddonShow(AtkUnitBase* thisPtr, bool silenceOpenSoundEffect, uint unsetShowHideFlags)
    {
        try
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
                this.OriginalVirtualTable->Show(thisPtr, silenceOpenSoundEffect, unsetShowHideFlags);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Show. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostShow, this.showArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonShow.");
        }
    }

    private void OnAddonHide(AtkUnitBase* thisPtr, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {
        try
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
                this.OriginalVirtualTable->Hide(thisPtr, unkBool, callHideCallback, setShowHideFlags);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Hide. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostHide, this.hideArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonHide.");
        }
    }

    private void OnAddonMove(AtkUnitBase* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.onMoveArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMove, this.onMoveArgs);

            try
            {
                this.OriginalVirtualTable->OnMove(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnMove. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMove, this.onMoveArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonMove.");
        }
    }

    private void OnAddonMouseOver(AtkUnitBase* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.onMouseOverArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMouseOver, this.onMouseOverArgs);

            try
            {
                this.OriginalVirtualTable->OnMouseOver(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnMouseOver. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMouseOver, this.onMouseOverArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonMouseOver.");
        }
    }

    private void OnAddonMouseOut(AtkUnitBase* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.onMouseOutArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreMouseOut, this.onMouseOutArgs);

            try
            {
                this.OriginalVirtualTable->OnMouseOut(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnMouseOut. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostMouseOut, this.onMouseOutArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonMouseOut.");
        }
    }

    private void OnAddonFocus(AtkUnitBase* thisPtr)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.focusArgs.Addon = thisPtr;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFocus, this.focusArgs);

            try
            {
                this.OriginalVirtualTable->Focus(thisPtr);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon Focus. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostFocus, this.focusArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonFocus.");
        }
    }

    private void OnAddonFocusChange(AtkUnitBase* thisPtr, bool isFocused)
    {
        try
        {
            this.LogEvent(EnableLogging);

            this.focusChangedArgs.Addon = thisPtr;
            this.focusChangedArgs.ShouldFocus = isFocused;

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFocusChanged, this.focusChangedArgs);

            isFocused = this.focusChangedArgs.ShouldFocus;

            try
            {
                this.OriginalVirtualTable->OnFocusChange(thisPtr, isFocused);
            }
            catch (Exception e)
            {
                Log.Error(e, "Caught exception when calling original Addon OnFocusChanged. This may be a bug in the game or another plugin hooking this method.");
            }

            this.lifecycleService.InvokeListenersSafely(AddonEvent.PostFocusChanged, this.focusChangedArgs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception from Dalamud when attempting to process OnAddonFocusChange.");
        }
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

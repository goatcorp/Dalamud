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

    private readonly AddonSetupArgs addonSetupArg = new();
    private readonly AddonFinalizeArgs addonFinalizeArg = new();
    private readonly AddonDrawArgs addonDrawArg = new();
    private readonly AddonUpdateArgs addonUpdateArg = new();
    private readonly AddonRefreshArgs addonRefreshArg = new();
    private readonly AddonRequestedUpdateArgs addonRequestedUpdateArg = new();
    private readonly AddonReceiveEventArgs addonReceiveEventArg = new();
    private readonly AddonGenericArgs addonGenericArg = new();

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

        this.addonSetupArg.Addon = addon;
        this.addonSetupArg.AtkValueCount = valueCount;
        this.addonSetupArg.AtkValues = (nint)values;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreSetup, this.addonSetupArg);
        valueCount = this.addonSetupArg.AtkValueCount;
        values = (AtkValue*)this.addonSetupArg.AtkValues;

        try
        {
            this.originalVirtualTable->OnSetup(addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonSetup. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostSetup, this.addonSetupArg);
    }

    private void OnAddonFinalize(AtkUnitBase* thisPtr)
    {
        this.LogEvent(EnableLogging);

        this.addonFinalizeArg.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreFinalize, this.addonFinalizeArg);

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

        this.addonDrawArg.Addon = addon;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreDraw, this.addonDrawArg);

        try
        {
            this.originalVirtualTable->Draw(addon);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonDraw. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostDraw, this.addonDrawArg);
    }

    private void OnAddonUpdate(AtkUnitBase* addon, float delta)
    {
        this.LogEvent(EnableLogging);

        this.addonUpdateArg.Addon = addon;
        this.addonUpdateArg.TimeDeltaInternal = delta;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreUpdate, this.addonUpdateArg);

        try
        {
            this.originalVirtualTable->Update(addon,  delta);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostUpdate, this.addonUpdateArg);
    }

    private bool OnAddonRefresh(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.addonRefreshArg.Addon = addon;
        this.addonRefreshArg.AtkValueCount = valueCount;
        this.addonRefreshArg.AtkValues = (nint)values;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreRefresh, this.addonRefreshArg);
        valueCount = this.addonRefreshArg.AtkValueCount;
        values = (AtkValue*)this.addonRefreshArg.AtkValues;

        try
        {
            result = this.originalVirtualTable->OnRefresh(addon, valueCount, values);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRefresh. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRefresh, this.addonRefreshArg);
        return result;
    }

    private void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        this.LogEvent(EnableLogging);

        this.addonRequestedUpdateArg.Addon = addon;
        this.addonRequestedUpdateArg.NumberArrayData = (nint)numberArrayData;
        this.addonRequestedUpdateArg.StringArrayData = (nint)stringArrayData;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreRequestedUpdate, this.addonRequestedUpdateArg);
        numberArrayData = (NumberArrayData**)this.addonRequestedUpdateArg.NumberArrayData;
        stringArrayData = (StringArrayData**)this.addonRequestedUpdateArg.StringArrayData;

        try
        {
            this.originalVirtualTable->OnRequestedUpdate(addon, numberArrayData, stringArrayData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonRequestedUpdate. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostRequestedUpdate, this.addonRequestedUpdateArg);
    }

    private void OnAddonReceiveEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.LogEvent(EnableLogging);

        this.addonReceiveEventArg.Addon = (nint)addon;
        this.addonReceiveEventArg.AtkEventType = (byte)eventType;
        this.addonReceiveEventArg.EventParam = eventParam;
        this.addonReceiveEventArg.AtkEvent = (IntPtr)atkEvent;
        this.addonReceiveEventArg.Data = (nint)atkEventData;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreReceiveEvent, this.addonReceiveEventArg);
        eventType = (AtkEventType)this.addonReceiveEventArg.AtkEventType;
        eventParam = this.addonReceiveEventArg.EventParam;
        atkEvent = (AtkEvent*)this.addonReceiveEventArg.AtkEvent;
        atkEventData = (AtkEventData*)this.addonReceiveEventArg.Data;

        try
        {
            this.originalVirtualTable->ReceiveEvent(addon, eventType, eventParam, atkEvent, atkEventData);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostReceiveEvent, this.addonReceiveEventArg);
    }

    private bool OnAddonOpen(AtkUnitBase* thisPtr, uint depthLayer)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.addonGenericArg.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreOpen, this.addonGenericArg);

        try
        {
            result = this.originalVirtualTable->Open(thisPtr, depthLayer);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonOpen. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostOpen, this.addonGenericArg);

        return result;
    }

    private bool OnAddonClose(AtkUnitBase* thisPtr, bool fireCallback)
    {
        this.LogEvent(EnableLogging);

        var result = false;

        this.addonGenericArg.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreClose, this.addonGenericArg);

        try
        {
            result = this.originalVirtualTable->Close(thisPtr, fireCallback);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonClose. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostClose, this.addonGenericArg);

        return result;
    }

    private void OnAddonShow(AtkUnitBase* thisPtr, bool silenceOpenSoundEffect, uint unsetShowHideFlags)
    {
        this.LogEvent(EnableLogging);

        this.addonGenericArg.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreShow, this.addonGenericArg);

        try
        {
            this.originalVirtualTable->Show(thisPtr, silenceOpenSoundEffect, unsetShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonShow. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostShow, this.addonGenericArg);
    }

    private void OnAddonHide(AtkUnitBase* thisPtr, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {
        this.LogEvent(EnableLogging);

        this.addonGenericArg.Addon = thisPtr;
        this.lifecycleService.InvokeListenersSafely(AddonEvent.PreHide, this.addonGenericArg);

        try
        {
            this.originalVirtualTable->Hide(thisPtr, unkBool, callHideCallback, setShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonHide. This may be a bug in the game or another plugin hooking this method.");
        }

        this.lifecycleService.InvokeListenersSafely(AddonEvent.PostHide, this.addonGenericArg);
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

using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Extensions;
using Dalamud.NativeUi.Timelines;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

internal unsafe partial class NativeAddon
{
    private const int VirtualTableEntryCount = 200;
    private bool isSetup;

    private AtkUnitBase.Delegates.Dtor destructorFunction = null!;
    private AtkUnitBase.Delegates.Draw drawFunction = null!;
    private AtkUnitBase.Delegates.Finalizer finalizerFunction = null!;
    private AtkUnitBase.Delegates.Hide hideFunction = null!;
    private AtkUnitBase.Delegates.Initialize initializeFunction = null!;
    private AtkUnitBase.Delegates.OnSetup onSetupFunction = null!;
    private AtkUnitBase.Delegates.Show showFunction = null!;
    private AtkUnitBase.Delegates.Hide2 softHideFunction = null!;
    private AtkUnitBase.Delegates.Update updateFunction = null!;
    private AtkUnitBase.Delegates.OnRequestedUpdate onRequestedUpdateFunction = null!;
    private AtkUnitBase.Delegates.OnRefresh onRefreshFunction = null!;
    private AtkUnitBase.Delegates.OnScreenSizeChange onScreenSizeChangedFunction = null!;

    private AtkUnitBase.AtkUnitBaseVirtualTable* modifiedVirtualTable;
    private AtkUnitBase.AtkUnitBaseVirtualTable* originalVirtualTable;

    /// <summary>
    /// Replaces original virtual table with a modified custom virtual table.
    /// </summary>
    protected void RegisterVirtualTable()
    {
        this.originalVirtualTable = this.InternalAddon->VirtualTable;

        // Overwrite virtual table with a custom copy,
        // Note: currently there are 73 virtual functions, but there's no harm in copying more for when they add new virtual functions to the game
        this.modifiedVirtualTable = (AtkUnitBase.AtkUnitBaseVirtualTable*)IMemorySpace.GetUISpace()->AllocateZeroedArray<nint>(VirtualTableEntryCount);
        NativeMemory.Copy(this.InternalAddon->VirtualTable, this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);
        this.InternalAddon->VirtualTable = this.modifiedVirtualTable;

        this.initializeFunction = this.Initialize;
        this.onSetupFunction = this.Setup;
        this.showFunction = this.Show;
        this.updateFunction = this.Update;
        this.drawFunction = this.Draw;
        this.hideFunction = this.Hide;
        this.softHideFunction = this.Hide2;
        this.finalizerFunction = this.Finalizer;
        this.destructorFunction = this.Destructor;
        this.onRequestedUpdateFunction = this.RequestedUpdate;
        this.onRefreshFunction = this.Refresh;
        this.onScreenSizeChangedFunction = this.ScreenSizeChange;

        this.modifiedVirtualTable->Initialize = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.initializeFunction);
        this.modifiedVirtualTable->OnSetup = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, void>)Marshal.GetFunctionPointerForDelegate(this.onSetupFunction);
        this.modifiedVirtualTable->Show = (delegate* unmanaged<AtkUnitBase*, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.showFunction);
        this.modifiedVirtualTable->Update = (delegate* unmanaged<AtkUnitBase*, float, void>)Marshal.GetFunctionPointerForDelegate(this.updateFunction);
        this.modifiedVirtualTable->Draw = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.drawFunction);
        this.modifiedVirtualTable->Hide = (delegate* unmanaged<AtkUnitBase*, bool, bool, uint, void>)Marshal.GetFunctionPointerForDelegate(this.hideFunction);
        this.modifiedVirtualTable->Hide2 = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.softHideFunction);
        this.modifiedVirtualTable->Finalizer = (delegate* unmanaged<AtkUnitBase*, void>)Marshal.GetFunctionPointerForDelegate(this.finalizerFunction);
        this.modifiedVirtualTable->Dtor = (delegate* unmanaged<AtkUnitBase*, byte, AtkEventListener*>)Marshal.GetFunctionPointerForDelegate(this.destructorFunction);
        this.modifiedVirtualTable->OnRequestedUpdate = (delegate* unmanaged<AtkUnitBase*, NumberArrayData**, StringArrayData**, void>)Marshal.GetFunctionPointerForDelegate(this.onRequestedUpdateFunction);
        this.modifiedVirtualTable->OnRefresh = (delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, bool>)Marshal.GetFunctionPointerForDelegate(this.onRefreshFunction);
        this.modifiedVirtualTable->OnScreenSizeChange = (delegate* unmanaged<AtkUnitBase*, int, int, void>)Marshal.GetFunctionPointerForDelegate(this.onScreenSizeChangedFunction);
    }

    /// <summary>
    /// OnSetup Callback for an addon, this is called to attach and save references to created nodes.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    /// <param name="atkValueSpan">Atk Values for this event.</param>
    protected virtual void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
    }

    /// <summary>
    /// OnShow Callback for an addon, this is called when the window is opened.
    /// </summary>
    /// <remarks>
    /// KamiToolKit intentionally does not allow hiding addons, so this is only called when it's opened.
    /// </remarks>
    /// <param name="addon">Pointer to the addons native memory.</param>
    protected virtual void OnShow(AtkUnitBase* addon)
    {
    }

    /// <summary>
    /// OnHide Callback for an addon, this is called when the window is opened.
    /// </summary>
    /// <remarks>
    /// KamiToolKit intentionally does not allow hiding addons, so this will then trigger close and then subsequently <see cref="OnFinalize"/>.
    /// </remarks>
    /// <param name="addon">Pointer to the addons native memory.</param>
    protected virtual void OnHide(AtkUnitBase* addon)
    {
    }

    /// <summary>
    /// OnDraw Callback for an addon, this is called every frame the addon is visible.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    protected virtual void OnDraw(AtkUnitBase* addon)
    {
    }

    /// <summary>
    /// OnUpdate Callback for an addon, this is called every frame the addon exists before its opened, and after it's closed but not finalized yet.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    protected virtual void OnUpdate(AtkUnitBase* addon)
    {
    }

    /// <summary>
    /// OnFinalize Callback for the addon, this is called immediately before it is deallocated/closed fully.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    protected virtual void OnFinalize(AtkUnitBase* addon)
    {
    }

    /// <summary>
    /// OnRequestedUpdate Callback for the addon, this is only called if you subscribe to string/number array data entries.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    /// <param name="numberArrayData">Pointer to number array data.</param>
    /// <param name="stringArrayData">Pointer to string array data.</param>
    protected virtual void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
    }

    /// <summary>
    /// OnRefresh Callback for the addon, the game calls this once on open, and may trigger it under other unknown conditions.
    /// </summary>
    /// <param name="addon">Pointer to the addons native memory.</param>
    /// <param name="atkValues">Atk values for this event.</param>
    protected virtual void OnRefresh(AtkUnitBase* addon, Span<AtkValue> atkValues)
    {
    }

    private void Initialize(AtkUnitBase* thisPtr)
    {
        this.originalVirtualTable->Initialize(thisPtr);

        var widgetInfo = IMemorySpace.GetUISpace()->MallocZeroed<AtkUldWidgetInfo>();
        widgetInfo->Id = 1;
        widgetInfo->NodeCount = 0;
        widgetInfo->NodeList = null;
        widgetInfo->WidgetAlignment = new AtkWidgetAlignment
        {
            AlignmentType = AlignmentType.Center,
            X = 50.0f,
            Y = 50.0f,
        };

        thisPtr->UldManager.InitializeResourceRendererManager();
        this.InternalAddon->UldManager.ResourceFlags |= AtkUldManagerResourceFlag.Initialized;

        this.InternalAddon->UldManager.Objects = (AtkUldObjectInfo*)widgetInfo;
        this.InternalAddon->UldManager.ObjectCount = 1;
        this.InternalAddon->UldManager.ResourceFlags |= AtkUldManagerResourceFlag.ArraysAllocated;

        var rootNodeTimelineBuilder =
            new TimelineBuilder()
                .BeginFrameSet(1, 89)
                .AddLabel(1, 101, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(10, 102, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(20, 103, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(30, 104, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(40, 105, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(50, 106, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(60, 107, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(70, 108, AtkTimelineJumpBehavior.PlayOnce, 0)
                .AddLabel(80, 109, AtkTimelineJumpBehavior.PlayOnce, 0)
                .EndFrameSet();

        this.RootNode.AddTimeline(rootNodeTimelineBuilder.Build());

        this.InternalAddon->RootNode = this.RootNode;
        this.InternalAddon->UldManager.AddNodeToObjectList(this.RootNode);

        if (!this.IsOverlayAddon && this.WindowNode is not null)
        {
            this.WindowNode.AttachNode(this, NodePosition.AsFirstChild);
            this.InternalAddon->WindowNode = this.WindowNode;
            this.InternalAddon->UldManager.AddNodeToObjectList(this.WindowNode);
        }

        this.WindowNode?.WindowHeaderFocusNode.AddNodeFlags(NodeFlags.Focusable);
        this.InternalAddon->FocusNode = this.WindowNode is not null ? this.WindowNode.WindowHeaderFocusNode : this.RootNode;

        this.InternalAddon->UldManager.UpdateDrawNodeList();
        this.InternalAddon->UldManager.LoadedState = AtkLoadState.Loaded;

        this.InternalAddon->LoadState = AtkUnitBaseLoadState.LoadingUldResource;
        this.InternalAddon->WasLoadUldByNameCalled = true;
        this.InternalAddon->UpdateCollisionNodeList(false);

        // Now that we have constructed this instance, track it for auto-dispose
        CreatedAddons.Add(this);
    }

    private void Setup(AtkUnitBase* addon, uint valueCount, AtkValue* values)
    {
        if (!this.IsOverlayAddon)
        {
            this.SetInitialState();
        }
        else
        {
            ref var screenSize = ref AtkStage.Instance()->ScreenSize;

            addon->SetScale(1.0f / AtkUnitBase.GetGlobalUIScale(), true);
            addon->SetSize((ushort)screenSize.Width, (ushort)screenSize.Height);
            addon->SetPosition(0, 0);
        }

        this.originalVirtualTable->OnSetup(addon, valueCount, values);

        try
        {
            this.OnSetup(addon, new Span<AtkValue>(values, (int)valueCount));
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Setup");
        }

        // Initialize all TextId fields that were attached in OnSetup
        addon->UldManager.SetupTextRecursive();

        this.isSetup = true;
    }

    private void Show(AtkUnitBase* addon, bool silenceOpenSoundEffect, uint unsetShowHideFlags)
    {
        try
        {
            this.OnShow(addon);
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Show");
        }

        this.originalVirtualTable->Show(addon, silenceOpenSoundEffect, unsetShowHideFlags);
    }

    private void Update(AtkUnitBase* addon, float delta)
    {
        try
        {
            this.OnUpdate(addon);
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Update");
        }

        this.originalVirtualTable->Update(addon, delta);
    }

    private void Draw(AtkUnitBase* addon)
    {
        try
        {
            this.OnDraw(addon);
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Draw");
        }

        this.originalVirtualTable->Draw(addon);
    }

    private void Hide(AtkUnitBase* addon, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {
        try
        {
            this.OnHide(addon);

            var dalamudConfig = Service<DalamudConfiguration>.Get();

            dalamudConfig.AddonConfigEntries[this.InternalName] = new AddonConfig
            {
                Position = new Vector2(this.InternalAddon->X, this.InternalAddon->Y),
                Scale = this.InternalAddon->Scale / AtkUnitBase.GetGlobalUIScale(),
            };

            dalamudConfig.QueueSave();
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Hide");
        }

        this.originalVirtualTable->Hide(addon, unkBool, callHideCallback, setShowHideFlags);
        this.originalVirtualTable->Close(addon, false);
    }

    private void Hide2(AtkUnitBase* addon)
    {
        this.originalVirtualTable->Hide2(addon);
    }

    private void Finalizer(AtkUnitBase* addon)
    {
        try
        {
            this.OnFinalize(addon);
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Finalizer");
        }

        if (this.RememberClosePosition)
        {
            this.LastClosePosition = new Vector2(this.InternalAddon->X, this.InternalAddon->Y);
        }

        this.originalVirtualTable->Finalizer(addon);
        this.isSetup = false;
    }

    private AtkEventListener* Destructor(AtkUnitBase* addon, byte flags)
    {
        var result = this.originalVirtualTable->Dtor(addon, flags);

        if ((flags & 1) == 1)
        {
            this.InternalAddon = null;

            this.disposeHandle?.Dispose();
            this.disposeHandle = null;

            CreatedAddons.Remove(this);

            // Free our custom virtual table, the game doesn't know this exists and won't clear it on its own.
            // Note: Free doesn't actually have a size argument. Pending update in CS on next API break.
            IMemorySpace.Free(this.modifiedVirtualTable, 0);
        }

        return result;
    }

    private void RequestedUpdate(AtkUnitBase* thisPtr, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        // Prevent calls to OnRequestedUpdate before Setup is completed. The game will try to call this after Show but before Setup
        if (this.isSetup)
        {
            try
            {
                this.OnRequestedUpdate(thisPtr, numberArrayData, stringArrayData);
            }
            catch (Exception e)
            {
                this.Log.Error(e, "Exception in NativeAddon.RequestedUpdate");
            }
        }

        this.originalVirtualTable->OnRequestedUpdate(thisPtr, numberArrayData, stringArrayData);
    }

    private bool Refresh(AtkUnitBase* thisPtr, uint valueCount, AtkValue* values)
    {
        try
        {
            this.OnRefresh(thisPtr, new Span<AtkValue>(values, (int)valueCount));
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception in NativeAddon.Refresh");
        }

        return this.originalVirtualTable->OnRefresh(thisPtr, valueCount, values);
    }

    private void ScreenSizeChange(AtkUnitBase* thisPtr, int width, int height)
    {
        this.originalVirtualTable->OnScreenSizeChange(thisPtr, width, height);

        if (this.IsOverlayAddon || this.IgnoreGlobalScale)
        {
            thisPtr->SetScale(1.0f / AtkUnitBase.GetGlobalUIScale(), true);
        }
    }
}

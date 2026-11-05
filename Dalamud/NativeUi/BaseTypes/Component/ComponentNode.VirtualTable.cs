using System.Runtime.InteropServices;

using Dalamud.NativeUi.Extensions;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Component;

internal abstract unsafe partial class ComponentNode
{
    private const int VirtualTableEntryCount = 100;

    private AtkComponentBase.Delegates.Dtor destructorFunction = null!;
    private AtkComponentBase.Delegates.ReceiveGlobalEvent receiveGlobalEventFunction = null!;
    private AtkComponentBase.Delegates.ReceiveEvent receiveEventFunction = null!;
    private AtkComponentBase.Delegates.Initialize initializeFunction = null!;
    private AtkComponentBase.Delegates.Deinitialize deinitializeFunction = null!;
    private AtkComponentBase.Delegates.Update updateFunction = null!;
    private AtkComponentBase.Delegates.Draw drawFunction = null!;
    private AtkComponentBase.Delegates.Setup setupFunction = null!;
    private AtkComponentBase.Delegates.SetEnabledState setEnabledStateFunction = null!;
    private AtkComponentBase.Delegates.PlaySoundEffect playSoundEffectFunction = null!;
    private AtkComponentBase.Delegates.GetAtkResNode getAtkResNodeFunction = null!;
    private AtkComponentBase.Delegates.GetFocusNode getFocusNodeFunction = null!;
    private AtkComponentBase.Delegates.InitializeFromComponentData initializeFromComponentData = null!;

    private AtkComponentBase.AtkComponentBaseVirtualTable* modifiedVirtualTable;
    private AtkComponentBase.AtkComponentBaseVirtualTable* originalVirtualTable;

    /// <summary>
    /// Replaces components original virtual table with a fully managed custom virtual table.
    /// </summary>
    protected void RegisterVirtualTable()
    {
        this.originalVirtualTable = this.ComponentBase->VirtualTable;

        this.modifiedVirtualTable = (AtkComponentBase.AtkComponentBaseVirtualTable*)IMemorySpace.GetUISpace()->AllocateZeroedArray<nint>(VirtualTableEntryCount);
        NativeMemory.Copy(this.ComponentBase->VirtualTable, this.modifiedVirtualTable, 0x8 * VirtualTableEntryCount);
        this.ComponentBase->VirtualTable = this.modifiedVirtualTable;

        this.destructorFunction = this.Destructor;
        this.receiveGlobalEventFunction = this.OnReceiveGlobalEvent;
        this.receiveEventFunction = this.OnReceiveEvent;
        this.initializeFunction = this.OnInitialize;
        this.deinitializeFunction = this.OnDeinitialize;
        this.updateFunction = this.OnUpdate;
        this.drawFunction = this.OnDraw;
        this.setupFunction = this.OnSetup;
        this.setEnabledStateFunction = this.OnSetEnabledState;
        this.playSoundEffectFunction = this.OnPlaySoundEffect;
        this.getAtkResNodeFunction = this.OnGetAtkResNode;
        this.getFocusNodeFunction = this.OnGetFocusNode;
        this.initializeFromComponentData = this.OnInitializeFromComponentData;

        this.modifiedVirtualTable->Dtor = (delegate* unmanaged<AtkComponentBase*, byte, AtkEventListener*>)Marshal.GetFunctionPointerForDelegate(this.destructorFunction);
        this.modifiedVirtualTable->ReceiveGlobalEvent = (delegate* unmanaged<AtkComponentBase*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)Marshal.GetFunctionPointerForDelegate(this.receiveGlobalEventFunction);
        this.modifiedVirtualTable->ReceiveEvent = (delegate* unmanaged<AtkComponentBase*, AtkEventType, int, AtkEvent*, AtkEventData*, void>)Marshal.GetFunctionPointerForDelegate(this.receiveEventFunction);
        this.modifiedVirtualTable->Initialize = (delegate* unmanaged<AtkComponentBase*, void>)Marshal.GetFunctionPointerForDelegate(this.initializeFunction);
        this.modifiedVirtualTable->Deinitialize = (delegate* unmanaged<AtkComponentBase*, void>)Marshal.GetFunctionPointerForDelegate(this.deinitializeFunction);
        this.modifiedVirtualTable->Update = (delegate* unmanaged<AtkComponentBase*, float, void>)Marshal.GetFunctionPointerForDelegate(this.updateFunction);
        this.modifiedVirtualTable->Draw = (delegate* unmanaged<AtkComponentBase*, void>)Marshal.GetFunctionPointerForDelegate(this.drawFunction);
        this.modifiedVirtualTable->Setup = (delegate* unmanaged<AtkComponentBase*, void>)Marshal.GetFunctionPointerForDelegate(this.setupFunction);
        this.modifiedVirtualTable->SetEnabledState = (delegate* unmanaged<AtkComponentBase*, bool, void>)Marshal.GetFunctionPointerForDelegate(this.setEnabledStateFunction);
        this.modifiedVirtualTable->PlaySoundEffect = (delegate* unmanaged<AtkComponentBase*, void>)Marshal.GetFunctionPointerForDelegate(this.playSoundEffectFunction);
        this.modifiedVirtualTable->GetAtkResNode = (delegate* unmanaged<AtkComponentBase*, AtkResNode*>)Marshal.GetFunctionPointerForDelegate(this.getAtkResNodeFunction);
        this.modifiedVirtualTable->GetFocusNode = (delegate* unmanaged<AtkComponentBase*, AtkResNode*>)Marshal.GetFunctionPointerForDelegate(this.getFocusNodeFunction);
        this.modifiedVirtualTable->InitializeFromComponentData = (delegate* unmanaged<AtkComponentBase*, void*, void>)Marshal.GetFunctionPointerForDelegate(this.initializeFromComponentData);
    }

    /// <summary>
    /// Global event callback for events that the game wired up to this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventParam">Event param.</param>
    /// <param name="atkEvent">Event struct.</param>
    /// <param name="atkEventData">Event data.</param>
    protected virtual void OnReceiveGlobalEvent(AtkComponentBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.originalVirtualTable->ReceiveGlobalEvent(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    /// <summary>
    /// Event callback for events that the game wired up to this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="eventParam">Event param.</param>
    /// <param name="atkEvent">Event struct.</param>
    /// <param name="atkEventData">Event data.</param>
    protected virtual void OnReceiveEvent(AtkComponentBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        this.originalVirtualTable->ReceiveEvent(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    /// <summary>
    /// Initialize callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnInitialize(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Initialize(thisPtr);
    }

    /// <summary>
    /// Unloading callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnDeinitialize(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Deinitialize(thisPtr);
    }

    /// <summary>
    /// Per-frame update callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="delta">Time since last update.</param>
    protected virtual void OnUpdate(AtkComponentBase* thisPtr, float delta)
    {
        this.originalVirtualTable->Update(thisPtr, delta);
    }

    /// <summary>
    /// Draw callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnDraw(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Draw(thisPtr);
    }

    /// <summary>
    /// Setup callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnSetup(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->Setup(thisPtr);
    }

    /// <summary>
    /// Enable state changed callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="enabled">If this component should be set as enabled.</param>
    protected virtual void OnSetEnabledState(AtkComponentBase* thisPtr, bool enabled)
    {
        this.originalVirtualTable->SetEnabledState(thisPtr, enabled);
    }

    /// <summary>
    /// Play sound effect callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    protected virtual void OnPlaySoundEffect(AtkComponentBase* thisPtr)
    {
        this.originalVirtualTable->PlaySoundEffect(thisPtr);
    }

    /// <summary>
    /// GetAtkResNode callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <returns>Pointer to AtkResNode.</returns>
    protected virtual AtkResNode* OnGetAtkResNode(AtkComponentBase* thisPtr)
    {
        return this.originalVirtualTable->GetAtkResNode(thisPtr);
    }

    /// <summary>
    /// GetFocusNode callback for this component.
    /// </summary>
    /// <remarks>
    /// Overriden to return <see cref="FocusNode"/>.
    /// </remarks>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <returns>Pointer to the node that should be focused.</returns>
    protected virtual AtkResNode* OnGetFocusNode(AtkComponentBase* thisPtr)
    {
        return this.FocusNode;
    }

    /// <summary>
    /// Initialization from data callback for this component.
    /// </summary>
    /// <param name="thisPtr">Pointer to this AtkComponentBase.</param>
    /// <param name="data">Pointer to AtkUldComponentDataBase or derived type.</param>
    protected virtual void OnInitializeFromComponentData(AtkComponentBase* thisPtr, void* data)
    {
        this.originalVirtualTable->InitializeFromComponentData(thisPtr, data);
    }

    private AtkEventListener* Destructor(AtkComponentBase* thisPtr, byte freeFlags)
    {
        var result = this.originalVirtualTable->Dtor(thisPtr, freeFlags);

        if ((freeFlags & 1) == 1)
        {
            // Free our custom virtual table, the game doesn't know this exists and won't clear it on its own.
            // Note: Free doesn't actually have a size argument. Pending update in CS on next API break.
            IMemorySpace.Free(this.modifiedVirtualTable, 0);
            this.modifiedVirtualTable = null;
        }

        return result;
    }
}

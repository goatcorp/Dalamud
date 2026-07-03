using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Component;

/// <summary>
/// .
/// </summary>
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

        this.modifiedVirtualTable = (AtkComponentBase.AtkComponentBaseVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * VirtualTableEntryCount, 8);
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
}

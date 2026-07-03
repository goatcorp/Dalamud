using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// .
/// </summary>
internal unsafe partial class NativeAddon
{
    private const int VirtualTableEntryCount = 200;

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

    private void RegisterVirtualTable()
    {
        this.originalVirtualTable = this.InternalAddon->VirtualTable;

        // Overwrite virtual table with a custom copy,
        // Note: currently there are 73 virtual functions, but there's no harm in copying more for when they add new virtual functions to the game
        this.modifiedVirtualTable = (AtkUnitBase.AtkUnitBaseVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * VirtualTableEntryCount, 8);
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
}

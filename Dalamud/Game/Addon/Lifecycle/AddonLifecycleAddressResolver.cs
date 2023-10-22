namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// AddonLifecycleService memory address resolver.
/// </summary>
internal class AddonLifecycleAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the addon setup hook invoked by the AtkUnitManager.
    /// There are two callsites for this vFunc, we need to hook both of them to catch both normal UI and special UI cases like dialogue.
    /// This is called for a majority of all addon OnSetup's.
    /// </summary>
    public nint AddonSetup { get; private set; }
    
    /// <summary>
    /// Gets the address of the other addon setup hook invoked by the AtkUnitManager.
    /// There are two callsites for this vFunc, we need to hook both of them to catch both normal UI and special UI cases like dialogue.
    /// This seems to be called rarely for specific addons.
    /// </summary>
    public nint AddonSetup2 { get; private set; }
    
    /// <summary>
    /// Gets the address of the addon finalize hook invoked by the AtkUnitManager.
    /// </summary>
    public nint AddonFinalize { get; private set; }
    
    /// <summary>
    /// Gets the address of the addon draw hook invoked by virtual function call.
    /// </summary>
    public nint AddonDraw { get; private set; }

    /// <summary>
    /// Gets the address of the addon update hook invoked by virtual function call.
    /// </summary>
    public nint AddonUpdate { get; private set; }
    
    /// <summary>
    /// Gets the address of the addon onRequestedUpdate hook invoked by virtual function call.
    /// </summary>
    public nint AddonOnRequestedUpdate { get; private set; }
    
    /// <summary>
    /// Gets the address of AtkUnitManager_vf10 which triggers addon onRefresh.
    /// </summary>
    public nint AddonOnRefresh { get; private set; }
    
    /// <summary>
    /// Gets the address of AtkEventListener base vTable.
    /// This is used to ensure that we do not hook ReceiveEvents that resolve back to the internal handler.
    /// </summary>
    public nint AtkEventListener { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.AddonSetup = sig.ScanText("FF 90 ?? ?? ?? ?? 48 8B 93 ?? ?? ?? ?? 80 8B");
        this.AddonSetup2 = sig.ScanText("FF 90 ?? ?? ?? ?? 48 8B 03 48 8B CB 80 8B");
        this.AddonFinalize = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6");
        this.AddonDraw = sig.ScanText("FF 90 ?? ?? ?? ?? 83 EB 01 79 C1");
        this.AddonUpdate = sig.ScanText("FF 90 ?? ?? ?? ?? 40 88 AF");
        this.AddonOnRequestedUpdate = sig.ScanText("FF 90 98 01 00 00 48 8B 5C 24 30 48 83 C4 20");
        this.AddonOnRefresh = sig.ScanText("48 89 5C 24 08 57 48 83 EC 20 41 8B F8 48 8B DA");
        this.AtkEventListener = sig.GetStaticAddressFromSig("4C 8D 3D ?? ?? ?? ?? 49 8D 8E");
    }
}

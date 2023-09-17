namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// DtrBar memory address resolver.
/// </summary>
internal class DtrBarAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the AtkUnitBaseDraw method.
    /// This is the base handler for all addons.
    /// We will use this here because _DTR does not have a overloaded handler, so we must use the base handler.
    /// </summary>
    public nint AtkUnitBaseDraw { get; private set; }
    
    /// <summary>
    /// Gets the address of the DTRRequestUpdate method.
    /// </summary>
    public nint AddonRequestedUpdate { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="scanner">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(SigScanner scanner)
    {
        this.AtkUnitBaseDraw = scanner.ScanText("48 83 EC 28 F6 81 ?? ?? ?? ?? ?? 4C 8B C1");
        this.AddonRequestedUpdate = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B BA ?? ?? ?? ?? 48 8B F1 49 8B 98 ?? ?? ?? ?? 33 D2");
    }
}

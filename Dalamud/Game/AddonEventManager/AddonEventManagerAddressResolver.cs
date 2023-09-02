namespace Dalamud.Game.AddonEventManager;

/// <summary>
/// AddonEventManager memory address resolver.
/// </summary>
internal class AddonEventManagerAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the global atkevent handler
    /// </summary>
    public nint GlobalEventHandler { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="scanner">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(SigScanner scanner)
    {
        this.GlobalEventHandler = scanner.ScanText("48 89 5C 24 ?? 48 89 7C 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 50 44 0F B7 F2");
    }
}

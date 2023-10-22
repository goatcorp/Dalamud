namespace Dalamud.Game.Addon.Events;

/// <summary>
/// AddonEventManager memory address resolver.
/// </summary>
internal class AddonEventManagerAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the AtkModule UpdateCursor method.
    /// </summary>
    public nint UpdateCursor { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="scanner">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner scanner)
    {
        this.UpdateCursor = scanner.ScanText("48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 4C 8B F1 E8 ?? ?? ?? ?? 49 8B CE");
    }
}

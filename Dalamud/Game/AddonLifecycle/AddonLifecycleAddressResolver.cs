namespace Dalamud.Game.AddonLifecycle;

/// <summary>
/// AddonLifecycleService memory address resolver.
/// </summary>
internal class AddonLifecycleAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the addon setup hook invoked by the atkunitmanager.
    /// </summary>
    public nint AddonSetup { get; private set; }
    
    /// <summary>
    /// Gets the address of the addon finalize hook invoked by the atkunitmanager.
    /// </summary>
    public nint AddonFinalize { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(SigScanner sig)
    {
        this.AddonSetup = sig.ScanText("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14");
        this.AddonFinalize = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6");
    }
}

using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// AddonLifecycleService memory address resolver.
/// </summary>
[Api13ToDo("Remove this class entirely, its not used by AddonLifecycleAnymore, and use something else for HookWidget")]
internal class AddonLifecycleAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the addon finalize hook invoked by the AtkUnitManager.
    /// </summary>
    public nint AddonFinalize { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.AddonFinalize = sig.ScanText("E8 ?? ?? ?? ?? 48 83 EF 01 75 D5");
    }
}

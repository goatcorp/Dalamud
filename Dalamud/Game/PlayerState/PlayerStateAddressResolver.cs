using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.PlayerState;

/// <summary>
/// Unlock state memory address resolver.
/// </summary>
internal class PlayerStateAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of a method which is called when <see cref="RaptureAtkModule.AgentUpdateFlag"/> has <see cref="RaptureAtkModule.AgentUpdateFlags.UnlocksUpdate"/>.
    /// </summary>
    public nint PerformMateriaActionMigration { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        // RaptureHotbarModule.PerformMateriaActionMigration
        this.PerformMateriaActionMigration = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 8B 01");
    }
}

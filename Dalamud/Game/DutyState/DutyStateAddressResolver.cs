namespace Dalamud.Game.DutyState;

/// <summary>
/// Duty state memory address resolver.
/// </summary>
internal class DutyStateAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the method which is called when the client receives a content director update.
    /// </summary>
    public IntPtr ContentDirectorNetworkMessage { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.ContentDirectorNetworkMessage = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B D9 49 8B F8 41 0F B7 08");
    }
}

namespace Dalamud.Game.Network.Internal;

internal class NetworkHandlersAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets or sets the pointer to the method responsible for handling CfPop packets.
    /// </summary>
    public nint CfPopPacketHandler { get; set; }

    protected override void Setup64Bit(SigScanner scanner)
    {
        this.CfPopPacketHandler = scanner.ScanText("40 53 57 48 83 EC 78 48 8B D9 48 8D 0D");
    }
}

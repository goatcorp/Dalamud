namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;

internal class MarketBoardAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets or sets the pointer to the method responsible for handling market board history. In this case, we are
    /// sigging the packet handler method directly.
    /// </summary>
    public nint MarketBoardHistoryPacketHandler { get; set; }

    /// <summary>
    /// Gets or sets the pointer to the method responsible for processing the market board purchase packet. In this
    /// case, we are sigging the packet handler method directly.
    /// </summary>
    public nint MarketBoardPurchasePacketHandler { get; set; }

    /// <summary>
    /// Gets or sets the pointer to the method responsible for custom talk events. Necessary for marketboard tax data,
    /// as this isn't really exposed anywhere else.
    /// </summary>
    public nint CustomTalkEventResponsePacketHandler { get; set; }

    protected override void Setup64Bit(SigScanner scanner)
    {
        this.MarketBoardHistoryPacketHandler = scanner.ScanText(
            "40 53 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 85 C0 74 36 4C 8B 00 48 8B C8 41 FF 90 ?? ?? ?? ?? 48 8B C8 BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 17 48 8D 53 04");
        this.MarketBoardPurchasePacketHandler =
            scanner.ScanText("40 55 53 57 48 8B EC 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 F0 48 8B 0D");
        this.CustomTalkEventResponsePacketHandler =
            scanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 43 ?? 4C 8D 4B 17");
    }
}

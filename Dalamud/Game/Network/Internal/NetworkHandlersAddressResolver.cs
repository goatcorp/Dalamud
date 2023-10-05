namespace Dalamud.Game.Network.Internal;

/// <summary>
/// Internal address resolver for the network handlers.
/// </summary>
internal class NetworkHandlersAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets or sets the pointer to the method responsible for handling CfPop packets.
    /// </summary>
    public nint CfPopPacketHandler { get; set; }
    
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
    
    /// <summary>
    /// Gets or sets the pointer to the method responsible for the marketboard ItemRequestStart packet.
    /// </summary>
    public nint MarketBoardItemRequestStartPacketHandler { get; set; }
    
    /// <summary>
    /// Gets or sets the pointer to the InfoProxyItemSearch.AddPage method, used to load market data.
    /// </summary>
    public nint InfoProxyItemSearchAddPage { get; set; }
    
    /// <summary>
    /// Gets or sets the pointer to the method inside InfoProxyItemSearch that is responsible for building and sending
    /// a purchase request packet.
    /// </summary>
    public nint BuildMarketBoardPurchaseHandlerPacket { get; set; }

    /// <inheritdoc />
    protected override void Setup64Bit(ISigScanner scanner)
    {
        this.CfPopPacketHandler = scanner.ScanText("40 53 57 48 83 EC 78 48 8B D9 48 8D 0D");
        this.MarketBoardHistoryPacketHandler = scanner.ScanText(
            "40 53 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 85 C0 74 36 4C 8B 00 48 8B C8 41 FF 90 ?? ?? ?? ?? 48 8B C8 BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 17 48 8D 53 04");
        this.MarketBoardPurchasePacketHandler =
            scanner.ScanText("40 55 53 57 48 8B EC 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 F0 48 8B 0D");
        this.CustomTalkEventResponsePacketHandler =
            scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 49 8B D9 41 0F B6 F8 0F B7 F2 8B E9 E8 ?? ?? ?? ?? 48 8B C8 44 0F B6 CF 0F B6 44 24 ?? 44 0F B7 C6 88 44 24 ?? 8B D5 48 89 5C 24");
        this.MarketBoardItemRequestStartPacketHandler =
            scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 8B F8");
        this.InfoProxyItemSearchAddPage =
            scanner.ScanText("48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 82 ?? ?? ?? ?? 48 8B FA 48 8B D9 38 41 19 74 54");
        this.BuildMarketBoardPurchaseHandlerPacket = 
            scanner.ScanText("40 53 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B D9 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B D0 48 85 C0 0F 84 ?? ?? ?? ?? 8B 8B");
    }
}

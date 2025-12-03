namespace Dalamud.Game.Network.Internal;

/// <summary>
/// Internal address resolver for the network handlers.
/// </summary>
internal class NetworkHandlersAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets or sets the pointer to the method responsible for custom talk events. Necessary for marketboard tax data,
    /// as this isn't really exposed anywhere else.
    /// </summary>
    public nint CustomTalkEventResponsePacketHandler { get; set; }

    /// <inheritdoc />
    protected override void Setup64Bit(ISigScanner scanner)
    {
        this.CustomTalkEventResponsePacketHandler =
            scanner.ScanText(
                "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 49 8B D9 41 0F B6 F8 0F B7 F2 8B E9 E8 ?? ?? ?? ?? 44 0F B6 54 24 ?? 44 0F B6 CF 44 88 54 24 ?? 44 0F B7 C6 8B D5"); // unnamed in CS
    }
}

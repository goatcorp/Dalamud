namespace Dalamud.Game.Gui;

/// <summary>
/// The address resolver for the <see cref="ChatGui"/> class.
/// </summary>
internal sealed class ChatGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native PopulateItemLinkObject method.
    /// </summary>
    public IntPtr PopulateItemLinkObject { get; private set; }

    /// <summary>
    /// Gets the address of the native InteractableLinkClicked method.
    /// </summary>
    public IntPtr InteractableLinkClicked { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        // PopulateItemLinkObject = sig.ScanText("48 89 5C 24 08 57 48 83  EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B  CA E8 32 03 00 00 48 8B C8 E8 FA F2 B0 FF 8B C8  EB 1D 0F B6 42 14 8B 4A");

        // PopulateItemLinkObject = sig.ScanText(      "48 89 5C 24 08 57 48 83  EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B  CA E8 32 03 00 00 48 8B C8 E8 ?? ?? B0 FF 8B C8  EB 1D 0F B6 42 14 8B 4A"); 5.0
        this.PopulateItemLinkObject = sig.ScanText("48 89 5C 24 08 57 48 83 EC 20 80 7A 06 00 48 8B DA 48 8B F9 74 14 48 8B CA E8 32 03 00 00 48 8B C8 E8 ?? ?? ?? FF 8B C8 EB 1D 0F B6 42 14 8B 4A");

        this.InteractableLinkClicked = sig.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 80 BB") + 9;
    }
}

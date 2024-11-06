namespace Dalamud.Game.Gui;

/// <summary>
/// The address resolver for the <see cref="ChatGui"/> class.
/// </summary>
internal sealed class ChatGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native InteractableLinkClicked method.
    /// </summary>
    public IntPtr InteractableLinkClicked { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.InteractableLinkClicked = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 4B ?? E8 ?? ?? ?? ?? 33 D2");
    }
}

namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// An address resolver for the <see cref="FlyTextGui"/> class.
/// </summary>
internal class FlyTextGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native AddFlyText method, which occurs
    /// when the game adds fly text elements to the UI. Multiple fly text
    /// elements can be added in a single AddFlyText call.
    /// </summary>
    public IntPtr AddFlyText { get; private set; }

    /// <summary>
    /// Gets the address of the native CreateFlyText method, which occurs
    /// when the game creates a new fly text element. This method is called
    /// once per fly text element, and can be called multiple times per
    /// AddFlyText call.
    /// </summary>
    public IntPtr CreateFlyText { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.AddFlyText = sig.ScanText("E8 ?? ?? ?? ?? FF C7 41 D1 C7");
        this.CreateFlyText = sig.ScanText("E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 0F 84 ?? ?? ?? ?? 48 8B 18");
    }
}

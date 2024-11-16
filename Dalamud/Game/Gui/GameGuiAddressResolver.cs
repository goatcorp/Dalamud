namespace Dalamud.Game.Gui;

/// <summary>
/// The address resolver for the <see cref="GameGui"/> class.
/// </summary>
internal sealed class GameGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the base address of the native GuiManager class.
    /// </summary>
    public IntPtr BaseAddress { get; private set; }

    /// <summary>
    /// Gets the address of the native SetGlobalBgm method.
    /// </summary>
    public IntPtr SetGlobalBgm { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleItemHover method.
    /// </summary>
    public IntPtr HandleItemHover { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleItemOut method.
    /// </summary>
    public IntPtr HandleItemOut { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleImm method.
    /// </summary>
    public IntPtr HandleImm { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.SetGlobalBgm = sig.ScanText("E8 ?? ?? ?? ?? 8B 2F");                 // unnamed in CS
        this.HandleItemHover = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 6C 24 48 48 8B 74 24 50 4C 89 B7 08 01 00 00"); // unnamed in CS
        this.HandleItemOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 4D"); // AgentItemDetail.ReceiveEvent
        this.HandleImm = sig.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 48 83 FF 09");  // unnamed in CS
    }
}

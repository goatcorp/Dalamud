using Dalamud.Plugin.Services;

namespace Dalamud.Game.Gui;

/// <summary>
/// The address resolver for the <see cref="GameGui"/> class.
/// </summary>
internal sealed class GameGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native HandleImm method.
    /// </summary>
    public IntPtr HandleImm { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.HandleImm = sig.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 48 83 FF 09");  // unnamed in CS
    }
}

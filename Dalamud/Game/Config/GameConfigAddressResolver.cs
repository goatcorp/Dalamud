namespace Dalamud.Game.Config;

/// <summary>
/// Game config system address resolver.
/// </summary>
internal sealed class GameConfigAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the method called when any config option is changed.
    /// </summary>
    public nint ConfigChangeAddress { get; private set; }
    
    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner scanner)
    {
        this.ConfigChangeAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 3F 49 3B 3E");
    }
}

namespace Dalamud.Game.Command;

/// <summary>
/// An address resolver for the command manager.
/// </summary>
public class CommandManagerAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets pointer to a method used to test if a command is valid or not. Will return -1 if the command does not
    /// exist.
    /// </summary>
    public nint CommandErrorHandler { get; private set; }
    
    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.SetupFramework(sig);
    }
    
    private void SetupFramework(ISigScanner scanner)
    {
        this.CommandErrorHandler = 
            scanner.ScanText("E8 ?? ?? ?? ?? 83 F8 ?? 74 ?? 83 F8 ?? 0F 85 ?? ?? ?? ?? 48 8B 07");
    }
}

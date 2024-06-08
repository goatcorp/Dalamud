namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// The address resolver for the <see cref="NameplateGui"/> class.
/// </summary>
internal class NameplateGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native SetPlayerNameplateDetour method.
    /// </summary>
    public IntPtr SetPlayerNameplateDetour { get; private set; }

    /// <inheritdoc/>
    protected override void SetupInternal(ISigScanner scanner)
    {
        this.SetPlayerNameplateDetour = scanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE");
    }
}

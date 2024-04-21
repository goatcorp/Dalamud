namespace Dalamud.Game;

/// <summary>
/// The address resolver for the <see cref="Framework"/> class.
/// </summary>
internal sealed class FrameworkAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address for the function that is called once the Framework is destroyed.
    /// </summary>
    public IntPtr DestroyAddress { get; private set; }

    /// <summary>
    /// Gets the address for the function that is called once the Framework is free'd.
    /// </summary>
    public IntPtr FreeAddress { get; private set; }

    /// <summary>
    /// Gets the function that is called every tick.
    /// </summary>
    public IntPtr TickAddress { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.SetupFramework(sig);
    }

    private void SetupFramework(ISigScanner scanner)
    {
        this.DestroyAddress =
            scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 3D ?? ?? ?? ?? 48 8B D9 48 85 FF");

        this.FreeAddress =
            scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B D9 48 8B 0D ?? ?? ?? ??");

        this.TickAddress =
            scanner.ScanText("40 53 48 83 EC 20 FF 81 ?? ?? ?? ?? 48 8B D9 48 8D 4C 24 ??");
    }
}

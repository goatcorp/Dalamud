namespace Dalamud.Game.Libc;

/// <summary>
/// The address resolver for the <see cref="LibcFunction"/> class.
/// </summary>
internal sealed class LibcFunctionAddressResolver : BaseAddressResolver
{
    private delegate IntPtr StringFromCString();

    /// <summary>
    /// Gets the address of the native StdStringFromCstring method.
    /// </summary>
    public IntPtr StdStringFromCstring { get; private set; }

    /// <summary>
    /// Gets the address of the native StdStringDeallocate method.
    /// </summary>
    public IntPtr StdStringDeallocate { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.StdStringFromCstring = sig.ScanText("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 48 8D 41 22 66 C7 41 20 01 01 48 89 01 49 8B D8");
        this.StdStringDeallocate = sig.ScanText("80 79 21 00 75 12 48 8B 51 08 41 B8 33 00 00 00 48 8B 09 E9 ?? ?? ?? 00 C3");
    }
}

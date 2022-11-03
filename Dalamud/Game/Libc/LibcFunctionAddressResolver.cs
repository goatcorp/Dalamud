using System;

namespace Dalamud.Game.Libc;

/// <summary>
/// The address resolver for the <see cref="LibcFunction"/> class.
/// </summary>
public sealed class LibcFunctionAddressResolver : BaseAddressResolver
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
    protected override void Setup64Bit(SigScanner sig)
    {
        this.StdStringFromCstring = sig.ScanText("48895C2408 4889742410 57 4883EC20 488D4122 66C741200101 488901 498BD8");
        this.StdStringDeallocate = sig.ScanText("80792100 7512 488B5108 41B833000000 488B09 E9??????00 C3");
    }
}

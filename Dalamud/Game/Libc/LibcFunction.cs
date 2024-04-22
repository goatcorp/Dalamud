using System.Runtime.InteropServices;
using System.Text;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.Libc;

/// <summary>
/// This class handles creating cstrings utilizing native game methods.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ILibcFunction>]
#pragma warning restore SA1015
internal sealed class LibcFunction : IServiceType, ILibcFunction
{
    private readonly LibcFunctionAddressResolver address;
    private readonly StdStringFromCStringDelegate stdStringCtorCString;
    private readonly StdStringDeallocateDelegate stdStringDeallocate;

    [ServiceManager.ServiceConstructor]
    private LibcFunction(TargetSigScanner sigScanner)
    {
        this.address = new LibcFunctionAddressResolver();
        this.address.Setup(sigScanner);

        this.stdStringCtorCString = Marshal.GetDelegateForFunctionPointer<StdStringFromCStringDelegate>(this.address.StdStringFromCstring);
        this.stdStringDeallocate = Marshal.GetDelegateForFunctionPointer<StdStringDeallocateDelegate>(this.address.StdStringDeallocate);
    }

    // TODO: prolly callconv is not okay in x86
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr StdStringFromCStringDelegate(IntPtr pStdString, [MarshalAs(UnmanagedType.LPArray)] byte[] content, IntPtr size);

    // TODO: prolly callconv is not okay in x86
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr StdStringDeallocateDelegate(IntPtr address);

    /// <inheritdoc/>
    public OwnedStdString NewString(byte[] content)
    {
        // While 0x70 bytes in the memory should be enough in DX11 version,
        // I don't trust my analysis so we're just going to allocate almost two times more than that.
        var pString = Marshal.AllocHGlobal(256);

        // Initialize a string
        var size = new IntPtr(content.Length);
        var pReallocString = this.stdStringCtorCString(pString, content, size);

        // Log.Verbose("Prev: {Prev} Now: {Now}", pString, pReallocString);

        return new OwnedStdString(pReallocString, this.DeallocateStdString);
    }
    
    /// <inheritdoc/>
    public OwnedStdString NewString(string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return this.NewString(encoding.GetBytes(content));
    }

    private void DeallocateStdString(IntPtr address)
    {
        this.stdStringDeallocate(address);
    }
}

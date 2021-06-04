using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Game.Internal.Libc
{
    /// <summary>
    /// This class handles creating cstrings utilizing native game methods.
    /// </summary>
    public sealed class LibcFunction
    {
        private readonly LibcFunctionAddressResolver address;
        private readonly StdStringFromCStringDelegate stdStringCtorCString;
        private readonly StdStringDeallocateDelegate stdStringDeallocate;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibcFunction"/> class.
        /// </summary>
        /// <param name="scanner">The SigScanner instance.</param>
        public LibcFunction(SigScanner scanner)
        {
            this.address = new LibcFunctionAddressResolver();
            this.address.Setup(scanner);

            this.stdStringCtorCString = Marshal.GetDelegateForFunctionPointer<StdStringFromCStringDelegate>(this.address.StdStringFromCstring);
            this.stdStringDeallocate = Marshal.GetDelegateForFunctionPointer<StdStringDeallocateDelegate>(this.address.StdStringDeallocate);
        }

        // TODO: prolly callconv is not okay in x86
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr StdStringFromCStringDelegate(IntPtr pStdString, [MarshalAs(UnmanagedType.LPArray)] byte[] content, IntPtr size);

        // TODO: prolly callconv is not okay in x86
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr StdStringDeallocateDelegate(IntPtr address);

        /// <summary>
        /// Create a new string from the given bytes.
        /// </summary>
        /// <param name="content">The bytes to convert.</param>
        /// <returns>An owned std string object.</returns>
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

        /// <summary>
        /// Create a new string form the given bytes.
        /// </summary>
        /// <param name="content">The bytes to convert.</param>
        /// <param name="encoding">A non-default encoding.</param>
        /// <returns>An owned std string object.</returns>
        public OwnedStdString NewString(string content, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return this.NewString(encoding.GetBytes(content));
        }

        private void DeallocateStdString(IntPtr address)
        {
            this.stdStringDeallocate(address);
        }
    }
}

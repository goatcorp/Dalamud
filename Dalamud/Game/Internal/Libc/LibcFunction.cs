using System;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game.Internal.Libc {
    public sealed class LibcFunction {
        // TODO: prolly callconv is not okay in x86
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr StdStringFromCStringDelegate(IntPtr pStdString, [MarshalAs(UnmanagedType.LPArray)]byte[] content, IntPtr size);

        // TODO: prolly callconv is not okay in x86
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr StdStringDeallocateDelegate(IntPtr address);
        
        private LibcFunctionAddressResolver Address { get; }

        private readonly StdStringFromCStringDelegate stdStringCtorCString;
        private readonly StdStringDeallocateDelegate stdStringDeallocate;
        
        public LibcFunction(SigScanner scanner) {
            Address = new LibcFunctionAddressResolver();
            Address.Setup(scanner);

            this.stdStringCtorCString = Marshal.GetDelegateForFunctionPointer<StdStringFromCStringDelegate>(Address.StdStringFromCstring);
            this.stdStringDeallocate = Marshal.GetDelegateForFunctionPointer<StdStringDeallocateDelegate>(Address.StdStringDeallocate);
        }

        public OwnedStdString NewString(byte[] content) {
            Log.Verbose("Allocating");
            
            // While 0x70 bytes in the memory should be enough in DX11 version,
            // I don't trust my analysis so we're just going to allocate almost two times more than that.
            var pString = Marshal.AllocHGlobal(256);
            
            // Initialize a string
            var npos = new IntPtr(0xFFFFFFFF); // assumed to be -1 (0xFFFFFFFF in x86, 0xFFFFFFFF_FFFFFFFF in amd64)
            var pReallocString = this.stdStringCtorCString(pString, content, npos);
            
            //Log.Verbose("Prev: {Prev} Now: {Now}", pString, pReallocString);
            
            return new OwnedStdString(pReallocString, DeallocateStdString);
        }

        private void DeallocateStdString(IntPtr address) {
            this.stdStringDeallocate(address);
        }
    }
}

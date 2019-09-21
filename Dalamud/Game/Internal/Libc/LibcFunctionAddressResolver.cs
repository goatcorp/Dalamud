using System;
using System.Security.Policy;

namespace Dalamud.Game.Internal.Libc {
    public sealed class LibcFunctionAddressResolver : BaseAddressResolver {
        private delegate IntPtr StringFromCString();

        public IntPtr StdStringFromCstring { get; private set; }
        public IntPtr StdStringDeallocate { get; private set; }
        
        protected override void Setup64Bit(SigScanner sig) {
            StdStringFromCstring = sig.ScanText("48895C2408 4889742410 57 4883EC20 488D4122 66C741200101 488901 498BD8");
            StdStringDeallocate = sig.ScanText("80792100 7512 488B5108 41B833000000 488B09 E9??????00 C3");
        }
    }
}

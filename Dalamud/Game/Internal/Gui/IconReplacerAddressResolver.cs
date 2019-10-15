using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.Gui {
    class IconReplacerAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        protected bool IsResolved { get; set; }

        protected override void Setup64Bit(SigScanner sig) {
            this.BaseAddress = sig.ScanText("81 fa d4 08 00 00 7f 4b 74 44 8d 42 eb 3d a3 00 00 00");
        }
    }
}

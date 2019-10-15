using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.Gui {
    class IconReplaceCheckerAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        protected bool IsResolved { get; set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.BaseAddress = sig.ScanText("81 f9 d4 08 00 00 7f 33 0f 84 fa 01 00 00 83 c1 eb 81 f9 a3 00 00 00");
        }
    }
}

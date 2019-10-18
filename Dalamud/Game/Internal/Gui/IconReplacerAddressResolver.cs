using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.Gui {
    class IconReplacerAddressResolver : BaseAddressResolver {
        public IntPtr GetIcon { get; private set; }
        public IntPtr IsIconReplaceable { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            this.GetIcon = sig.ScanText("81 fa d4 08 00 00 7f 4b 74 44 8d 42 eb 3d a3 00 00 00");
            this.IsIconReplaceable = sig.ScanText("81 f9 d4 08 00 00 7f 33 0f 84 fa 01 00 00 83 c1 eb 81 f9 a3 00 00 00");
        }
    }
}

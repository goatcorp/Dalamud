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
            this.GetIcon = sig.ScanText("48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 30 8b da be dd 1c 00 00 bd d3 0d 00 00");
            //this.GetIcon = sig.ScanText("E8 ?? ?? ?? ?? F6 DB 8B C8");
            
            this.IsIconReplaceable = sig.ScanText("81 f9 2e 01 00 00 7f 39 81 f9 2d 01 00 00 0f 8d 11 02 00 00 83 c1 eb");
            //this.IsIconReplaceable = sig.ScanText("81 F9 ?? ?? ?? ?? 7F 39 81 F9 ?? ?? ?? ??");
        }
    }
}

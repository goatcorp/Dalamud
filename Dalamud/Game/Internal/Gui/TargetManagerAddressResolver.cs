using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.Gui {
    class TargetManagerAddressResolver : BaseAddressResolver {
        public IntPtr GetTarget { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            this.GetTarget = sig.ScanText("40 57 48 83 EC 40 48 8B  F9 48 8B 49 08 48 8B 01 FF 50 40 66 83 B8 CA 81  00 00 00 74 33 48 8B 4F 08 48 8B 01 FF 50 40 66  83 B8 CA 81 00 00 04 74");
        }
    }
}

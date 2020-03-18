using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Internal;

namespace Dalamud.Game.ClientState
{
    public sealed class ClientStateAddressResolver : BaseAddressResolver {
        public IntPtr ActorTable { get; private set; }
        public IntPtr LocalContentId { get; private set; }
        public IntPtr JobGaugeData { get; set; }
        
        protected override void Setup64Bit(SigScanner sig) {
            ActorTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 85 ED", 0) + 0x148;
            LocalContentId = sig.Module.BaseAddress + 0x1C2E000;
            JobGaugeData = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? FF C6 48 8D 5B 0C", 0xB9) + 0x10;
        }
    }
}

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
            ActorTable = sig.GetStaticAddressFromSig("F3 0F 11 05 ?? ?? ?? ?? EB 27", 0) + 0xC;
            LocalContentId = sig.Module.BaseAddress + 0x1C2E000;
            JobGaugeData = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 77 93", 0x220) + 0x10;
        }
    }
}

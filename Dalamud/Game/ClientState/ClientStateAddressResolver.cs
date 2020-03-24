using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Internal;

namespace Dalamud.Game.ClientState
{
    public sealed class ClientStateAddressResolver : BaseAddressResolver {
        public IntPtr ActorTable { get; private set; }
        public IntPtr LocalContentId { get; private set; }
        public IntPtr JobGaugeData { get; private set; }
        public IntPtr TerritoryType { get; private set; }
        
        protected override void Setup64Bit(SigScanner sig) {
            ActorTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 85 ED", 0) + 0x148;
            LocalContentId = sig.Module.BaseAddress + 0x1C2E000;
            JobGaugeData = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? FF C6 48 8D 5B 0C", 0xB9) + 0x10;
            TerritoryType = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 0F B6 5F 12 8B C8", 0x26);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct ASTGauge {
        [FieldOffset(4)] private CardType Card;
        [FieldOffset(5)] private unsafe fixed byte Seals[3];

        public CardType DrawnCard() {
            return Card;
        }

        public unsafe bool ContainsSeal(SealType seal) {
            if (Seals[0] == (byte)seal) return true;
            if (Seals[1] == (byte)seal) return true;
            if (Seals[2] == (byte)seal) return true;
            return false;
        }
    }
}

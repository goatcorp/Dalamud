using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct BLMGauge {
        [FieldOffset(0)] public short TimeUntilNextPolyglot;  //eno timer (ms)
        [FieldOffset(2)] public short ElementTimeRemaining;  //ui/af timer
        [FieldOffset(4)] private byte ElementStance; //ui/af
        [FieldOffset(5)] public byte NumUmbralHearts; //number of umbral hearts
        [FieldOffset(6)] public byte NumPolyglotStacks; //number of polyglot stacks
        [FieldOffset(7)] private byte EnoState; //eno active?

        public bool InUmbralIce() {
            return ElementStance > 4;
        }

        public bool InAstralFire() {
            return ElementStance > 0 && ElementStance < 4;
        }

        public bool IsEnoActive() {
            return EnoState > 0;
        }

    }

    
}

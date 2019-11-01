using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct SMNGauge {
        
        //Unfinished
        [FieldOffset(0)] public short TimerRemaining;
        [FieldOffset(2)] public SummonPet ReturnSummon;
        [FieldOffset(3)] public PetGlam ReturnSummonGlam;
        [FieldOffset(4)] public byte NumStacks;

        public bool IsPhoenixReady() {
            return (NumStacks & 0x10) > 0;
        }

        public bool IsBahamutReady() {
            return (NumStacks & 8) > 0;
        }

        public bool HasAetherflowStacks() {
            return (NumStacks & 3) > 0;
        }
    }
}

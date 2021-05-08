using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory BLM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BLMGauge
    {
        [FieldOffset(0)]
        public short TimeUntilNextPolyglot;  // eno timer (ms)

        [FieldOffset(2)]
        public short ElementTimeRemaining;  // ui/af timer

        [FieldOffset(4)]
        private byte ElementStance; // ui/af

        [FieldOffset(5)]
        public byte NumUmbralHearts; // number of umbral hearts

        [FieldOffset(6)]
        public byte NumPolyglotStacks; // number of polyglot stacks

        [FieldOffset(7)]
        private byte EnoState; // eno active?

        public bool InUmbralIce()
        {
            return this.ElementStance > 4;
        }

        public bool InAstralFire()
        {
            return this.ElementStance > 0 && this.ElementStance < 4;
        }

        public bool IsEnoActive()
        {
            return this.EnoState > 0;
        }
    }
}

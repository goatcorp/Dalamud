using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DNC job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DNCGauge
    {
        [FieldOffset(0)]
        public byte NumFeathers;

        [FieldOffset(1)]
        public byte Esprit;

        [FieldOffset(2)]
        private fixed byte StepOrder[4];

        [FieldOffset(6)]
        public byte NumCompleteSteps;

        public bool IsDancing()
        {
            return this.StepOrder[0] != 0;
        }

        public ulong NextStep()
        {
            return (ulong)(15999 + this.StepOrder[this.NumCompleteSteps] - 1);
        }
    }
}

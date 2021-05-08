using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SCH job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SCHGauge
    {
        [FieldOffset(2)]
        public byte NumAetherflowStacks;

        [FieldOffset(3)]
        public byte FairyGaugeAmount;

        [FieldOffset(4)]
        public short SeraphTimer;

        [FieldOffset(6)]
        public DismissedFairy DismissedFairy;
    }
}

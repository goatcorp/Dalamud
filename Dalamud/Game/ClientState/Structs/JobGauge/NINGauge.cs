using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory NIN job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct NINGauge
    {
        [FieldOffset(0)]
        public int HutonTimeLeft;

        [FieldOffset(4)]
        public byte Ninki;

        [Obsolete("Does not appear to be used")]
        [FieldOffset(4)]
        public byte TCJMudrasUsed;

        [Obsolete("Does not appear to be used")]
        [FieldOffset(6)]
        public byte NumHutonManualCasts;
    }
}

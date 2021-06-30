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
        /// <summary>
        /// Gets the time left on Huton in milliseconds.
        /// </summary>
        // TODO: Probably a short, confirm.
        [FieldOffset(0)]
        public int HutonTimeLeft;

        /// <summary>
        /// Gets the amount of Ninki available.
        /// </summary>
        [FieldOffset(4)]
        public byte Ninki;

        /// <summary>
        /// Obsolete.
        /// </summary>
        [Obsolete("Does not appear to be used")]
        [FieldOffset(4)]
        public byte TCJMudrasUsed;

        /// <summary>
        /// Gets the number of times Huton has been cast manually.
        /// </summary>
        [FieldOffset(5)]
        public byte NumHutonManualCasts;
    }
}

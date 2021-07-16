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
        private int hutonTimeLeft;

        [FieldOffset(4)]
        private byte ninki;

        [FieldOffset(5)]
        private byte numHutonManualCasts;

        /// <summary>
        /// Gets the time left on Huton in milliseconds.
        /// </summary>
        public int HutonTimeLeft => this.hutonTimeLeft;

        /// <summary>
        /// Gets the amount of Ninki available.
        /// </summary>
        public byte Ninki => this.ninki;

        /// <summary>
        /// Gets the number of times Huton has been cast manually.
        /// </summary>
        public byte NumHutonManualCasts => this.numHutonManualCasts;
    }
}

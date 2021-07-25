using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SAM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SAMGauge
    {
        [FieldOffset(3)]
        private byte kenki;

        [FieldOffset(4)]
        private byte meditationStacks;

        [FieldOffset(5)]
        private Sen sen;

        /// <summary>
        /// Gets the current amount of Kenki available.
        /// </summary>
        public byte Kenki => this.kenki;

        /// <summary>
        /// Gets the amount of Meditation stacks.
        /// </summary>
        public byte MeditationStacks => this.meditationStacks;

        /// <summary>
        /// Gets the active Sen.
        /// </summary>
        public Sen Sen => this.sen;

        /// <summary>
        /// Gets if the Setsu Sen is active.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasSetsu() => (this.Sen & Sen.SETSU) != 0;

        /// <summary>
        /// Gets if the Getsu Sen is active.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasGetsu() => (this.Sen & Sen.GETSU) != 0;

        /// <summary>
        /// Gets if the Ka Sen is active.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasKa() => (this.Sen & Sen.KA) != 0;
    }
}

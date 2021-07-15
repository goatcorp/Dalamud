using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SAM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SAMGauge
    {
        /// <summary>
        /// Gets the current amount of Kenki available.
        /// </summary>
        [FieldOffset(3)]
        public byte Kenki;

        /// <summary>
        /// Gets the amount of Meditation stacks.
        /// </summary>
        [FieldOffset(4)]
        public byte MeditationStacks;

        /// <summary>
        /// Gets the active Sen.
        /// </summary>
        [FieldOffset(5)]
        public Sen Sen;

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

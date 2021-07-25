using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SMN job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SMNGauge
    {
        [FieldOffset(0)]
        private short timerRemaining;

        [FieldOffset(2)]
        private SummonPet returnSummon;

        [FieldOffset(3)]
        private PetGlam returnSummonGlam;

        [FieldOffset(4)]
        private byte numStacks;

        /// <summary>
        /// Gets the time remaining for the current summon.
        /// </summary>
        public short TimerRemaining => this.timerRemaining;

        /// <summary>
        /// Gets the summon that will return after the current summon expires.
        /// </summary>
        public SummonPet ReturnSummon => this.returnSummon;

        /// <summary>
        /// Gets the summon glam for the <see cref="ReturnSummon"/>.
        /// </summary>
        public PetGlam ReturnSummonGlam => this.returnSummonGlam;

        /// <summary>
        /// Gets the current stacks.
        /// Use the summon accessors instead.
        /// </summary>
        public byte NumStacks => this.numStacks;

        /// <summary>
        /// Gets if Phoenix is ready to be summoned.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsPhoenixReady() => (this.NumStacks & 0x10) > 0;

        /// <summary>
        /// Gets if Bahamut is ready to be summoned.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsBahamutReady() => (this.NumStacks & 8) > 0;

        /// <summary>
        /// Gets if there are any Aetherflow stacks available.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasAetherflowStacks() => (this.NumStacks & 3) > 0;
    }
}

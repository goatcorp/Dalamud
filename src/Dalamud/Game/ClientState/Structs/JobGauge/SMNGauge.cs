using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SMN job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SMNGauge
    {
        /// <summary>
        /// Gets the time remaining for the current summon.
        /// </summary>
        [FieldOffset(0)]
        public short TimerRemaining;

        /// <summary>
        /// Gets the summon that will return after the current summon expires.
        /// </summary>
        [FieldOffset(2)]
        public SummonPet ReturnSummon;

        /// <summary>
        /// Gets the summon glam for the <see cref="ReturnSummon"/>.
        /// </summary>
        [FieldOffset(3)]
        public PetGlam ReturnSummonGlam;

        /// <summary>
        /// Gets the current stacks.
        /// Use the summon accessors instead.
        /// </summary>
        [FieldOffset(4)]
        public byte NumStacks;

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

using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory BLM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BLMGauge
    {
        /// <summary>
        /// Gets the time until the next Polyglot stack in milliseconds.
        /// </summary>
        [FieldOffset(0)]
        public short TimeUntilNextPolyglot;  // enochian timer

        /// <summary>
        /// Gets the time remaining for Astral Fire or Umbral Ice in milliseconds.
        /// </summary>
        [FieldOffset(2)]
        public short ElementTimeRemaining;  // umbral ice and astral fire timer

        [FieldOffset(4)]
        private byte elementStance; // umbral ice or astral fire

        /// <summary>
        /// Gets the number of Umbral Hearts remaining.
        /// </summary>
        [FieldOffset(5)]
        public byte NumUmbralHearts;

        /// <summary>
        /// Gets the number of Polyglot stacks remaining.
        /// </summary>
        [FieldOffset(6)]
        public byte NumPolyglotStacks;

        [FieldOffset(7)]
        private byte enochianState;

        /// <summary>
        /// Gets if the player is in Umbral Ice.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool InUmbralIce() => this.elementStance > 4;

        /// <summary>
        /// Gets if the player is in Astral fire.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool InAstralFire() => this.elementStance > 0 && this.elementStance < 4;

        /// <summary>
        /// Gets if Enochian is active.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsEnoActive() => this.enochianState > 0;
    }
}

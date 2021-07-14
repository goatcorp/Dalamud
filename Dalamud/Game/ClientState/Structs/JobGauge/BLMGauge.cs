using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory BLM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BLMGauge
    {
        [FieldOffset(0)]
        private short timeUntilNextPolyglot;  // enochian timer

        [FieldOffset(2)]
        private short elementTimeRemaining;  // umbral ice and astral fire timer

        [FieldOffset(4)]
        private byte elementStance; // umbral ice or astral fire

        [FieldOffset(5)]
        private byte numUmbralHearts;

        [FieldOffset(6)]
        private byte numPolyglotStacks;

        [FieldOffset(7)]
        private byte enochianState;

        /// <summary>
        /// Gets the time until the next Polyglot stack in milliseconds.
        /// </summary>
        public short TimeUntilNextPolyglot => this.timeUntilNextPolyglot;

        /// <summary>
        /// Gets the time remaining for Astral Fire or Umbral Ice in milliseconds.
        /// </summary>
        public short ElementTimeRemaining => this.elementTimeRemaining;

        /// <summary>
        /// Gets the number of Polyglot stacks remaining.
        /// </summary>
        public byte NumPolyglotStacks => this.numPolyglotStacks;

        /// <summary>
        /// Gets the number of Umbral Hearts remaining.
        /// </summary>
        public byte NumUmbralHearts => this.numUmbralHearts;

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

using System;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory BLM job gauge.
    /// </summary>
    public unsafe class BLMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.BLMGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BLMGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal BLMGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the time until the next Polyglot stack in milliseconds.
        /// </summary>
        public short TimeUntilNextPolyglot => this.Struct->TimeUntilNextPolyglot;

        /// <summary>
        /// Gets the time remaining for Astral Fire or Umbral Ice in milliseconds.
        /// </summary>
        public short ElementTimeRemaining => this.Struct->ElementTimeRemaining;

        /// <summary>
        /// Gets the number of Polyglot stacks remaining.
        /// </summary>
        public byte NumPolyglotStacks => this.Struct->NumPolyglotStacks;

        /// <summary>
        /// Gets the number of Umbral Hearts remaining.
        /// </summary>
        public byte NumUmbralHearts => this.Struct->NumUmbralHearts;

        /// <summary>
        /// Gets a value indicating whether if the player is in Umbral Ice.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool InUmbralIce => this.Struct->ElementStance > 4;

        /// <summary>
        /// Gets a value indicating whether if the player is in Astral fire.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool InAstralFire => this.Struct->ElementStance > 0 && this.Struct->ElementStance < 4;

        /// <summary>
        /// Gets a value indicating whether if Enochian is active.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsEnoActive => this.Struct->EnochianState > 0;
    }
}

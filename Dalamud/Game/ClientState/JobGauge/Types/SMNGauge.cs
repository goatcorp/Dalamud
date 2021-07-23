using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory SMN job gauge.
    /// </summary>
    public unsafe class SMNGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.SMNGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SMNGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal SMNGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the time remaining for the current summon.
        /// </summary>
        public short TimerRemaining => this.Struct->TimerRemaining;

        /// <summary>
        /// Gets the summon that will return after the current summon expires.
        /// </summary>
        public SummonPet ReturnSummon => (SummonPet)this.Struct->ReturnSummon;

        /// <summary>
        /// Gets the summon glam for the <see cref="ReturnSummon"/>.
        /// </summary>
        public PetGlam ReturnSummonGlam => (PetGlam)this.Struct->ReturnSummonGlam;

        /// <summary>
        /// Gets the current stacks.
        /// Use the summon accessors instead.
        /// </summary>
        public byte NumStacks => this.Struct->NumStacks;

        /// <summary>
        /// Gets a value indicating whether if Phoenix is ready to be summoned.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsPhoenixReady => (this.NumStacks & 0x10) > 0;

        /// <summary>
        /// Gets a value indicating whether Bahamut is ready to be summoned.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsBahamutReady => (this.NumStacks & 8) > 0;

        /// <summary>
        /// Gets a value indicating whether there are any Aetherflow stacks available.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasAetherflowStacks => (this.NumStacks & 3) > 0;
    }
}

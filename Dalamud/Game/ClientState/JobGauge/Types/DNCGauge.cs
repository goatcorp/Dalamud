using System;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory DNC job gauge.
    /// </summary>
    public unsafe class DNCGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.DNCGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DNCGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal DNCGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the number of feathers available.
        /// </summary>
        public byte NumFeathers => this.Struct->NumFeathers;

        /// <summary>
        /// Gets the amount of Espirit available.
        /// </summary>
        public byte Esprit => this.Struct->Esprit;

        /// <summary>
        /// Gets the number of steps completed for the current dance.
        /// </summary>
        public byte NumCompleteSteps => this.Struct->NumCompleteSteps;

        /// <summary>
        /// Gets the next step in the current dance.
        /// </summary>
        /// <returns>The next dance step action ID.</returns>
        public ulong NextStep => (ulong)(15999 + this.Struct->StepOrder[this.NumCompleteSteps] - 1);

        /// <summary>
        /// Gets a value indicating whether the player is dancing or not.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsDancing => this.Struct->StepOrder[0] != 0;
    }
}

using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory DRG job gauge.
    /// </summary>
    public unsafe class DRGGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.DragoonGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DRGGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal DRGGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the time remaining for Blood of the Dragon in milliseconds.
        /// </summary>
        public short BOTDTimer => this.Struct->BotdTimer;

        /// <summary>
        /// Gets the current state of Blood of the Dragon.
        /// </summary>
        public BOTDState BOTDState => (BOTDState)this.Struct->BotdState;

        /// <summary>
        /// Gets the count of eyes opened during Blood of the Dragon.
        /// </summary>
        public byte EyeCount => this.Struct->EyeCount;
    }
}

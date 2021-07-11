using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DNC job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DNCGauge
    {
        /// <summary>
        /// Gets the number of feathers available.
        /// </summary>
        [FieldOffset(0)]
        public byte NumFeathers;

        /// <summary>
        /// Gets the amount of Espirit available.
        /// </summary>
        [FieldOffset(1)]
        public byte Esprit;

        [FieldOffset(2)]
        private fixed byte stepOrder[4];

        /// <summary>
        /// Gets the number of steps completed for the current dance.
        /// </summary>
        [FieldOffset(6)]
        public byte NumCompleteSteps;

        /// <summary>
        /// Gets the next step in the current dance.
        /// </summary>
        /// <returns>The next dance step action ID.</returns>
        public ulong NextStep() => (ulong)(15999 + this.stepOrder[this.NumCompleteSteps] - 1);

        /// <summary>
        /// Gets if the player is dancing or not.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsDancing() => this.stepOrder[0] != 0;
    }
}

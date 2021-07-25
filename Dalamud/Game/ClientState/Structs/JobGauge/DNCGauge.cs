using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DNC job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DNCGauge
    {
        [FieldOffset(0)]
        private byte numFeathers;

        [FieldOffset(1)]
        private byte esprit;

        [FieldOffset(2)]
        private fixed byte stepOrder[4];

        [FieldOffset(6)]
        private byte numCompleteSteps;

        /// <summary>
        /// Gets the number of feathers available.
        /// </summary>
        public byte NumFeathers => this.numFeathers;

        /// <summary>
        /// Gets the amount of Espirit available.
        /// </summary>
        public byte Esprit => this.esprit;

        /// <summary>
        /// Gets the number of steps completed for the current dance.
        /// </summary>
        public byte NumCompleteSteps => this.numCompleteSteps;

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

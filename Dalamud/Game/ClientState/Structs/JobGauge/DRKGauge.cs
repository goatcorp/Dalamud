using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DRK job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DRKGauge
    {
        [FieldOffset(0)]
        private byte blood;

        [FieldOffset(2)]
        private ushort darksideTimeRemaining;

        [FieldOffset(4)]
        private byte darkArtsState;

        [FieldOffset(6)]
        private ushort shadowTimeRemaining;

        /// <summary>
        /// Gets the amount of blood accumulated.
        /// </summary>
        public byte Blood => this.blood;

        /// <summary>
        /// Gets the Darkside time remaining in milliseconds.
        /// </summary>
        public ushort DarksideTimeRemaining => this.darksideTimeRemaining;

        /// <summary>
        /// Gets the Shadow time remaining in milliseconds.
        /// </summary>
        public ushort ShadowTimeRemaining => this.shadowTimeRemaining;

        /// <summary>
        /// Gets if the player has Dark Arts or not.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasDarkArts() => this.darkArtsState > 0;
    }
}

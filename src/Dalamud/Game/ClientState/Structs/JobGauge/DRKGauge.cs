using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DRK job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DRKGauge
    {
        /// <summary>
        /// Gets the amount of blood accumulated.
        /// </summary>
        [FieldOffset(0)]
        public byte Blood;

        /// <summary>
        /// Gets the Darkside time remaining in milliseconds.
        /// </summary>
        [FieldOffset(2)]
        public ushort DarksideTimeRemaining;

        [FieldOffset(4)]
        private byte darkArtsState;

        /// <summary>
        /// Gets the Shadow time remaining in milliseconds.
        /// </summary>
        [FieldOffset(6)]
        public ushort ShadowTimeRemaining;

        /// <summary>
        /// Gets if the player has Dark Arts or not.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool HasDarkArts() => this.darkArtsState > 0;
    }
}

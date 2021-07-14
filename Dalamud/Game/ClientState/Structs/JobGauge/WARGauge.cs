using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory WAR job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WARGauge
    {
        [FieldOffset(0)]
        private byte beastGaugeAmount;

        /// <summary>
        /// Gets the amount of wrath in the Beast gauge.
        /// </summary>
        public byte BeastGaugeAmount => this.beastGaugeAmount;
    }
}

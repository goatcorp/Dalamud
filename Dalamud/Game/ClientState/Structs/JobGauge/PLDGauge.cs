using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory PLD job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PLDGauge
    {
        [FieldOffset(0)]
        private byte gaugeAmount;

        /// <summary>
        /// Gets the current level of the Oath gauge.
        /// </summary>
        public byte GaugeAmount => this.gaugeAmount;
    }
}

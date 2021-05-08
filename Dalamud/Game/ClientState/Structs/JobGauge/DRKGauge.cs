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
        public byte Blood;

        [FieldOffset(2)]
        public ushort DarksideTimeRemaining;

        [FieldOffset(4)]
        private byte DarkArtsState;

        [FieldOffset(6)]
        public ushort ShadowTimeRemaining;

        public bool HasDarkArts()
        {
            return this.DarkArtsState > 0;
        }
    }
}

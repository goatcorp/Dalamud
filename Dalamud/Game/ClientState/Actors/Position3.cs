using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Actors {
    [StructLayout(LayoutKind.Sequential)]
    public struct Position3 {
        public float X;
        public float Z;
        public float Y;
    }
}

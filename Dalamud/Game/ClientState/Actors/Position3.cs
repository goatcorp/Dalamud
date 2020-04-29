using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Actors {
    [StructLayout(LayoutKind.Sequential)]
    public struct Position3 {
        public float X;
        public float Z;
        public float Y;

        /// <summary>
        /// Convert this Position3 to a System.Numerics.Vector3
        /// </summary>
        /// <param name="pos">Position to convert.</param>
        public static implicit operator System.Numerics.Vector3(Position3 pos) => new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Convert this Position3 to a SharpDX.Vector3
        /// </summary>
        /// <param name="pos">Position to convert.</param>
        public static implicit operator SharpDX.Vector3(Position3 pos) => new SharpDX.Vector3(pos.X, pos.Y, pos.Z);
    }
}

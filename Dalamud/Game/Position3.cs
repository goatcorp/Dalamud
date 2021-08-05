using System.Runtime.InteropServices;

namespace Dalamud.Game
{
    /// <summary>
    /// A game native equivalent of a Vector3.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Position3
    {
        /// <summary>
        /// The X of (X,Z,Y).
        /// </summary>
        public float X;

        /// <summary>
        /// The Z of (X,Z,Y).
        /// </summary>
        public float Z;

        /// <summary>
        /// The Y of (X,Z,Y).
        /// </summary>
        public float Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Position3"/> struct.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="z">The Z position.</param>
        /// <param name="y">The Y position.</param>
        public Position3(float x, float z, float y)
        {
            this.X = x;
            this.Z = z;
            this.Y = y;
        }

        /// <summary>
        /// Convert this Position3 to a System.Numerics.Vector3.
        /// </summary>
        /// <param name="pos">Position to convert.</param>
        public static implicit operator System.Numerics.Vector3(Position3 pos) => new(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Convert this Position3 to a SharpDX.Vector3.
        /// </summary>
        /// <param name="pos">Position to convert.</param>
        public static implicit operator SharpDX.Vector3(Position3 pos) => new(pos.X, pos.Z, pos.Y);
    }
}

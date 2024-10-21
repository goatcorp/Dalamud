using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <inheritdoc cref="NineGridNodeTree"/>
internal unsafe partial class NineGridNodeTree
{
    /// <summary>
    /// A struct representing the four offsets of an <see cref="AtkNineGridNode"/>.
    /// </summary>
    internal struct NineGridOffsets
    {
        /// <summary>Top offset.</summary>
        internal int Top;

        /// <summary>Left offset.</summary>
        internal int Left;

        /// <summary>Right offset.</summary>
        internal int Right;

        /// <summary>Bottom offset.</summary>
        internal int Bottom;

        /// <summary>
        /// Initializes a new instance of the <see cref="NineGridOffsets"/> struct.
        /// </summary>
        /// <param name="top">The top offset.</param>
        /// <param name="right">The right offset.</param>
        /// <param name="bottom">The bottom offset.</param>
        /// <param name="left">The left offset.</param>
        internal NineGridOffsets(int top, int right, int bottom, int left)
        {
            this.Top = top;
            this.Right = right;
            this.Left = left;
            this.Bottom = bottom;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NineGridOffsets"/> struct.
        /// </summary>
        /// <param name="ngNode">The node using these offsets.</param>
        internal NineGridOffsets(AtkNineGridNode* ngNode)
            : this(ngNode->TopOffset, ngNode->RightOffset, ngNode->BottomOffset, ngNode->LeftOffset)
        {
        }

        private NineGridOffsets(Vector4 v)
            : this((int)v.X, (int)v.Y, (int)v.Z, (int)v.W)
        {
        }

        public static implicit operator NineGridOffsets(Vector4 v) => new(v);

        public static implicit operator Vector4(NineGridOffsets v) => new(v.Top, v.Right, v.Bottom, v.Left);

        public static NineGridOffsets operator *(float n, NineGridOffsets a) => n * (Vector4)a;

        public static NineGridOffsets operator *(NineGridOffsets a, float n) => n * a;

        /// <summary>Prints the offsets in ImGui.</summary>
        internal readonly void Print() => PrintFieldValuePairs(("Top", $"{this.Top}"), ("Bottom", $"{this.Bottom}"), ("Left", $"{this.Left}"), ("Right", $"{this.Right}"));
    }
}

using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Internal.Gui.Structs
{
    /// <summary>
    /// Native memory representation of an FFXIV UI addon.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Addon
    {
        /// <summary>
        /// The name of the addon.
        /// </summary>
        [FieldOffset(AddonOffsets.Name)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Name;

        /// <summary>
        /// Various flags that can be set on the addon.
        /// </summary>
        /// <remarks>
        /// This is a bitfield.
        /// </remarks>
        [FieldOffset(AddonOffsets.Flags)]
        public byte Flags;

        /// <summary>
        /// The X position of the addon on screen.
        /// </summary>
        [FieldOffset(AddonOffsets.X)]
        public short X;

        /// <summary>
        /// The Y position of the addon on screen.
        /// </summary>
        [FieldOffset(AddonOffsets.Y)]
        public short Y;

        /// <summary>
        /// The scale of the addon.
        /// </summary>
        [FieldOffset(AddonOffsets.Scale)]
        public float Scale;

        /// <summary>
        /// The root node of the addon's node tree.
        /// </summary>
        [FieldOffset(AddonOffsets.RootNode)]
        public unsafe AtkResNode* RootNode;
    }

    /// <summary>
    /// Memory offsets for the <see cref="Addon"/> type.
    /// </summary>
    public static class AddonOffsets
    {
        public const int Name = 0x8;
        public const int RootNode = 0xC8;
        public const int Flags = 0x182;
        public const int X = 0x1BC;
        public const int Y = 0x1BE;
        public const int Scale = 0x1AC;
    }
}

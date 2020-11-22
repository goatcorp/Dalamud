using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui.Structs {

    public class AddonOffsets {
        public const int Name = 0x8;
        public const int RootNode = 0xC8;
        public const int Flags = 0x182;
        public const int X = 0x1BC;
        public const int Y = 0x1BE;
        public const int Scale = 0x1AC;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Addon {
        [FieldOffset(AddonOffsets.Name), MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Name;

        [FieldOffset(AddonOffsets.Flags)] public byte Flags;
        [FieldOffset(AddonOffsets.X)] public short X;
        [FieldOffset(AddonOffsets.Y)] public short Y;
        [FieldOffset(AddonOffsets.Scale)] public float Scale;
        [FieldOffset(AddonOffsets.RootNode)] public unsafe AtkResNode* RootNode;

    }
}

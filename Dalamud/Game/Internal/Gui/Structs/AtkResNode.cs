using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui.Structs {

    [StructLayout(LayoutKind.Explicit, Size = 0xA8)]

    // https://github.com/aers/FFXIVClientStructs/blob/main/Component/GUI/AtkResNode.cs
    public unsafe struct AtkResNode {
        [FieldOffset(0x0)] public IntPtr AtkEventTarget;
        [FieldOffset(0x8)] public uint NodeID;
        [FieldOffset(0x20)] public AtkResNode* ParentNode;
        [FieldOffset(0x28)] public AtkResNode* PrevSiblingNode;
        [FieldOffset(0x30)] public AtkResNode* NextSiblingNode;
        [FieldOffset(0x38)] public AtkResNode* ChildNode;
        [FieldOffset(0x40)] public ushort Type;
        [FieldOffset(0x42)] public ushort ChildCount;
        [FieldOffset(0x44)] public float X;
        [FieldOffset(0x48)] public float Y;
        [FieldOffset(0x4C)] public float ScaleX;
        [FieldOffset(0x50)] public float ScaleY;
        [FieldOffset(0x54)] public float Rotation;
        [FieldOffset(0x58)] public fixed float UnkMatrix[3 * 2];
        [FieldOffset(0x70)] public uint Color;
        [FieldOffset(0x74)] public float Depth;
        [FieldOffset(0x78)] public float Depth_2;
        [FieldOffset(0x7C)] public ushort AddRed;
        [FieldOffset(0x7E)] public ushort AddGreen;
        [FieldOffset(0x80)] public ushort AddBlue;
        [FieldOffset(0x82)] public ushort AddRed_2;
        [FieldOffset(0x84)] public ushort AddGreen_2;
        [FieldOffset(0x86)] public ushort AddBlue_2;
        [FieldOffset(0x88)] public byte MultiplyRed;
        [FieldOffset(0x89)] public byte MultiplyGreen;
        [FieldOffset(0x8A)] public byte MultiplyBlue;
        [FieldOffset(0x8B)] public byte MultiplyRed_2;
        [FieldOffset(0x8C)] public byte MultiplyGreen_2;
        [FieldOffset(0x8D)] public byte MultiplyBlue_2;
        [FieldOffset(0x8E)] public byte Alpha_2;
        [FieldOffset(0x8F)] public byte UnkByte_1;
        [FieldOffset(0x90)] public ushort Width;
        [FieldOffset(0x92)] public ushort Height;
        [FieldOffset(0x94)] public float OriginX;
        [FieldOffset(0x98)] public float OriginY;
        [FieldOffset(0x9C)] public ushort Priority;
        [FieldOffset(0x9E)] public short Flags;
        [FieldOffset(0xA0)] public uint Flags_2;
    }
}

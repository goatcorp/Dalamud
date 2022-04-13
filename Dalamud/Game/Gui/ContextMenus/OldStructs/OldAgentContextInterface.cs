using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenus.OldStructs;

// TODO: This is transplanted from client structs before the rework. Need to take some time to sort all of this out soon.

[StructLayout(LayoutKind.Explicit)]
public unsafe struct OldAgentContextInterface
{
    [FieldOffset(0x0)] public AgentInterface AgentInterface;
    [FieldOffset(0x670)] public unsafe byte SelectedIndex;
    [FieldOffset(0x690)] public byte* Unk1;
    [FieldOffset(0xD08)] public byte* SubContextMenuTitle;
    [FieldOffset(0x1740)] public bool IsSubContextMenu;
}

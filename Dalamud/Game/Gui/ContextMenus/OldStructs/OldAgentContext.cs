using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenus.OldStructs;

// TODO: This is transplanted from client structs before the rework. Need to take some time to sort all of this out soon.

[StructLayout(LayoutKind.Explicit)]
public unsafe struct OldAgentContext
{
    public static OldAgentContext* Instance() => (OldAgentContext*)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Context);

    [FieldOffset(0x0)] public AgentInterface AgentInterface;
    [FieldOffset(0x0)] public OldAgentContextInterface AgentContextInterface;
    [FieldOffset(0xD18)] public unsafe OldAgentContextMenuItems* Items;
    [FieldOffset(0xE08)] public Utf8String GameObjectName;
    [FieldOffset(0xEE0)] public ulong GameObjectContentId;
    [FieldOffset(0xEF0)] public uint GameObjectId;
    [FieldOffset(0xF00)] public ushort GameObjectWorldId;
}

[StructLayout(LayoutKind.Explicit)]
public struct OldAgentContextMenuItems
{
    [FieldOffset(0x0)] public ushort AtkValueCount;
    [FieldOffset(0x8)] public AtkValue AtkValues;
    [FieldOffset(0x428)] public byte Actions;
    [FieldOffset(0x450)] public ulong UnkFunctionPointers;
    [FieldOffset(0x598)] public ulong RedButtonActions;
}

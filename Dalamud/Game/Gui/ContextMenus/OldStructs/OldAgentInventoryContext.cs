using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenus.OldStructs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct OldAgentInventoryContext
{
    public static OldAgentInventoryContext* Instance() => (OldAgentInventoryContext*) FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.InventoryContext);

    [FieldOffset(0x0)] public AgentInterface AgentInterface;
    [FieldOffset(0x0)] public OldAgentContextInterface AgentContextInterface;
    [FieldOffset(0x2C)] public uint FirstContextMenuItemAtkValueIndex;
    [FieldOffset(0x30)] public uint ContextMenuItemCount;
    [FieldOffset(0x38)] public AtkValue AtkValues;
    [FieldOffset(0x558)] public unsafe byte Actions;
    [FieldOffset(0x5A8)] public uint UnkFlags;
    [FieldOffset(0x5B0)] public uint PositionX;
    [FieldOffset(0x5B4)] public uint PositionY;
    [FieldOffset(0x5F8)] public uint InventoryItemId;
    [FieldOffset(0x5FC)] public uint InventoryItemCount;
    [FieldOffset(0x604)] public bool InventoryItemIsHighQuality;
}

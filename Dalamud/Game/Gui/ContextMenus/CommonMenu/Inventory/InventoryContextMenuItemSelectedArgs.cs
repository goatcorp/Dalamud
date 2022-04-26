using System;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu.Inventory;

/// <summary>
/// The arguments for when an inventory context menu item is selected
/// </summary>
public class InventoryContextMenuItemSelectedArgs : BaseInventoryContextMenuArgs {
    internal InventoryContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq) {
    }
}

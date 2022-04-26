using System;
using System.Collections.Generic;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu.Inventory;

/// <summary>
/// The arguments for when an inventory context menu is opened
/// </summary>
public class InventoryContextMenuOpenArgs : BaseInventoryContextMenuArgs {
    /// <summary>
    /// Context menu items in this menu.
    /// </summary>
    public List<BaseContextMenuItem> Items { get; } = new();

    internal InventoryContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq) {
    }
}

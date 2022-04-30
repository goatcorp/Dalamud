using System;
using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// The arguments for when an inventory context menu is opened.
/// </summary>
public class InventoryContextMenuOpenArgs : BaseInventoryContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryContextMenuOpenArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="itemId">itemId.</param>
    /// <param name="itemAmount">itemAmount.</param>
    /// <param name="itemHq">itemHq.</param>
    internal InventoryContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq)
        : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq)
        {
    }

    /// <summary>
    /// Gets context menu items in this menu.
    /// </summary>
    internal List<BaseContextMenuItem> Items { get; } = new();

    /// <summary>
    /// Add custom item to context menu items.
    /// </summary>
    /// <param name="name">context menu name.</param>
    /// <param name="action">context menu action.</param>
    public void AddCustomItem(SeString name, ContextMenu.InventoryContextMenuItemSelectedDelegate action)
    {
        var customItem = new InventoryContextMenuItem(ContextMenu.AddDalamudContextMenuIndicator(name), action);
        this.Items.Add(customItem);
    }
}

using System;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// The arguments for when an inventory context menu item is selected.
/// </summary>
public class InventoryContextMenuItemSelectedArgs : BaseInventoryContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryContextMenuItemSelectedArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="itemId">itemId.</param>
    /// <param name="itemAmount">itemAmount.</param>
    /// <param name="itemHq">itemHq.</param>
    internal InventoryContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq)
        : base(addon, agent, parentAddonName, itemId, itemAmount, itemHq)
        {
    }
}

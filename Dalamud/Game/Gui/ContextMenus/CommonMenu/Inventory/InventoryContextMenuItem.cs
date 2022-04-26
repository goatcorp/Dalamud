using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu.Inventory;

/// <summary>
/// A custom context menu item for inventory items.
/// </summary>
public class InventoryContextMenuItem : CustomContextMenuItem<ContextMenu.InventoryContextMenuItemSelectedDelegate> {
    /// <summary>
    /// Create a new context menu item for inventory items.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages</param>
    /// <param name="action">the action to perform on click</param>
    public InventoryContextMenuItem(SeString name, ContextMenu.InventoryContextMenuItemSelectedDelegate action) : base(name, action) {
    }
}

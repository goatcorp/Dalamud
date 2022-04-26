using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// A custom normal context menu item
/// </summary>
public class NormalContextMenuItem : CustomContextMenuItem<ContextMenu.ContextMenuItemSelectedDelegate> {
    /// <summary>
    /// Create a new custom context menu item.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages</param>
    /// <param name="action">the action to perform on click</param>
    public NormalContextMenuItem(SeString name, ContextMenu.ContextMenuItemSelectedDelegate action) : base(name, action) {
    }
}

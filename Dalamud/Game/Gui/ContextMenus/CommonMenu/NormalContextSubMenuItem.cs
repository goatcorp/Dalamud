using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// A custom context menu item that will open a submenu
/// </summary>
public class NormalContextSubMenuItem : CustomContextMenuItem<ContextMenu.ContextMenuOpenEventDelegate> {
    /// <summary>
    /// Create a new custom context menu item that will open a submenu.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages</param>
    /// <param name="action">the action to perform on click</param>
    public NormalContextSubMenuItem(SeString name, ContextMenu.ContextMenuOpenEventDelegate action) : base(name, action) {
        this.IsSubMenu = true;
    }
}

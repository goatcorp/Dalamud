namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// A base context menu item
/// </summary>
public abstract class BaseContextMenuItem {
    /// <summary>
    /// If this item should be enabled in the menu.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// If this item should have the submenu arrow in the menu.
    /// </summary>
    public bool IsSubMenu { get; set; }
}

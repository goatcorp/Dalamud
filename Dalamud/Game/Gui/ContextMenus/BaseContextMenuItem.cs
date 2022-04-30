namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// A base context menu item.
/// </summary>
public abstract class BaseContextMenuItem
{
    /// <summary>
    /// Gets a value indicating whether if this item should be enabled in the menu.
    /// </summary>
    public bool Enabled { get; protected init; } = true;

    /// <summary>
    /// Gets a value indicating whether if this item should have the submenu arrow in the menu.
    /// </summary>
    public bool IsSubMenu { get; protected init; }
}

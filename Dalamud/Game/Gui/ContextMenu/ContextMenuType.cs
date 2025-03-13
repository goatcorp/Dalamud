namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// The type of context menu.
/// Each one has a different associated <see cref="MenuTarget"/>.
/// </summary>
public enum ContextMenuType
{
    /// <summary>
    /// The default context menu.
    /// </summary>
    Default,

    /// <summary>
    /// The inventory context menu. Used when right-clicked on an item.
    /// </summary>
    Inventory,
}

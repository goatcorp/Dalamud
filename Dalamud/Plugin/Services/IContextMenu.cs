using Dalamud.Game.Gui.ContextMenu;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides methods for interacting with the game's context menu.
/// </summary>
public interface IContextMenu
{
    /// <summary>
    /// A delegate type used for the <see cref="OnMenuOpened"/> event.
    /// </summary>
    /// <param name="args">Information about the currently opening menu.</param>
    public delegate void OnMenuOpenedDelegate(MenuOpenedArgs args);

    /// <summary>
    /// Event that gets fired whenever any context menu is opened.
    /// </summary>
    /// <remarks>Use this event and then check if the triggering addon is the desired addon, then add custom context menu items to the provided args.</remarks>
    event OnMenuOpenedDelegate OnMenuOpened;

    /// <summary>
    /// Adds a menu item to a context menu.
    /// </summary>
    /// <param name="menuType">The type of context menu to add the item to.</param>
    /// <param name="item">The item to add.</param>
    /// <remarks>Used to add a context menu entry to <em>all</em> context menus.</remarks>
    void AddMenuItem(ContextMenuType menuType, MenuItem item);

    /// <summary>
    /// Removes a menu item from a context menu.
    /// </summary>
    /// <param name="menuType">The type of context menu to remove the item from.</param>
    /// <param name="item">The item to add.</param>
    /// <remarks>Used to remove a context menu entry from <em>all</em> context menus.</remarks>
    /// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if it was not found.</returns>
    bool RemoveMenuItem(ContextMenuType menuType, MenuItem item);
}

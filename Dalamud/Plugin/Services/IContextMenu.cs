using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

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
    /// Event that gets fired every time the game framework updates.
    /// </summary>
    event OnMenuOpenedDelegate OnMenuOpened;

    /// <summary>
    /// Adds a menu item to a context menu.
    /// </summary>
    /// <param name="menuType">The type of context menu to add the item to.</param>
    /// <param name="item">The item to add.</param>
    void AddMenuItem(ContextMenuType menuType, MenuItem item);

    /// <summary>
    /// Removes a menu item from a context menu.
    /// </summary>
    /// <param name="menuType">The type of context menu to remove the item from.</param>
    /// <param name="item">The item to add.</param>
    /// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if it was not found.</returns>
    bool RemoveMenuItem(ContextMenuType menuType, MenuItem item);
}

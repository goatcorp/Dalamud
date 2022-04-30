using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// A custom normal context menu item.
/// </summary>
public class GameObjectContextMenuItem : CustomContextMenuItem<ContextMenu.GameObjectContextMenuItemSelectedDelegate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameObjectContextMenuItem"/> class.
    /// Create a new custom context menu item.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages.</param>
    /// <param name="action">the action to perform on click.</param>
    public GameObjectContextMenuItem(SeString name, ContextMenu.GameObjectContextMenuItemSelectedDelegate action)
        : base(name, action)
        {
    }
}

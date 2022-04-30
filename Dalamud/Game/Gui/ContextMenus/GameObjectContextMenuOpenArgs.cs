using System;
using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// Arguments for the context menu event.
/// </summary>
public class GameObjectContextMenuOpenArgs : BaseContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameObjectContextMenuOpenArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="objectId">objectId.</param>
    /// <param name="contentIdLower">contentIdLower.</param>
    /// <param name="text">text.</param>
    /// <param name="objectWorld">objectWorld.</param>
    internal GameObjectContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld)
        : base(addon, agent, parentAddonName, objectId, contentIdLower, text, objectWorld)
        {
    }

    /// <summary>
    /// Gets context menu items in this menu.
    /// </summary>
    internal List<BaseContextMenuItem> Items { get; } = new();

    /// <summary>
    /// Add custom item to context menu items.
    /// </summary>
    /// <param name="name">context menu name.</param>
    /// <param name="action">context menu action.</param>
    public void AddCustomItem(SeString name, ContextMenu.GameObjectContextMenuItemSelectedDelegate action)
    {
        var customItem = new GameObjectContextMenuItem(ContextMenu.AddDalamudContextMenuIndicator(name), action);
        this.Items.Add(customItem);
    }
}

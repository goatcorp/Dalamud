using System;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// Arguments for the context menu item selected delegate.
/// </summary>
public class GameObjectContextMenuItemSelectedArgs : BaseContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameObjectContextMenuItemSelectedArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="objectId">objectId.</param>
    /// <param name="contentIdLower">contentIdLower.</param>
    /// <param name="text">text.</param>
    /// <param name="objectWorld">objectWorld.</param>
    internal GameObjectContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld)
        : base(addon, agent, parentAddonName, objectId, contentIdLower, text, objectWorld)
        {
    }
}

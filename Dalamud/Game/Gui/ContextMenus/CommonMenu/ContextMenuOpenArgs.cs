using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// Arguments for the context menu event.
/// </summary>
public class ContextMenuOpenArgs : BaseContextMenuArgs {
    /// <summary>
    /// Context menu items in this menu.
    /// </summary>
    public List<BaseContextMenuItem> Items { get; } = new();

    internal ContextMenuOpenArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld) : base(addon, agent, parentAddonName, objectId, contentIdLower, text, objectWorld) {
    }
}

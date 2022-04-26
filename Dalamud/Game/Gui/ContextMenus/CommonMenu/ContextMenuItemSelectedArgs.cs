using System;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// Arguments for the context menu item selected delegate.
/// </summary>
public class ContextMenuItemSelectedArgs : BaseContextMenuArgs {
    internal ContextMenuItemSelectedArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint objectId, uint contentIdLower, SeString? text, ushort objectWorld) : base(addon, agent, parentAddonName, objectId, contentIdLower, text, objectWorld) {
    }
}

using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Callback args used when a menu item is clicked.
/// </summary>
public sealed unsafe class MenuItemClickedArgs : MenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItemClickedArgs"/> class.
    /// </summary>
    /// <param name="openSubmenu">Callback for opening a submenu.</param>
    /// <param name="addon">Addon associated with the context menu.</param>
    /// <param name="agent">Agent associated with the context menu.</param>
    /// <param name="type">The type of context menu.</param>
    /// <param name="eventInterfaces">List of AtkEventInterfaces associated with the context menu.</param>
    internal MenuItemClickedArgs(Action<SeString?, IReadOnlyList<MenuItem>> openSubmenu, AtkUnitBase* addon, AgentInterface* agent, ContextMenuType type, IReadOnlySet<nint> eventInterfaces)
        : base(addon, agent, type, eventInterfaces)
    {
        this.OnOpenSubmenu = openSubmenu;
    }

    private Action<SeString?, IReadOnlyList<MenuItem>> OnOpenSubmenu { get; }

    /// <summary>
    /// Opens a submenu with the given name and items.
    /// </summary>
    /// <param name="name">The name of the submenu, displayed at the top.</param>
    /// <param name="items">The items to display in the submenu.</param>
    public void OpenSubmenu(SeString name, IReadOnlyList<MenuItem> items) =>
        this.OnOpenSubmenu(name, items);

    /// <summary>
    /// Opens a submenu with the given items.
    /// </summary>
    /// <param name="items">The items to display in the submenu.</param>
    public void OpenSubmenu(IReadOnlyList<MenuItem> items) =>
        this.OnOpenSubmenu(null, items);
}

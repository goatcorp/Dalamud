using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Callback args used when a menu item is clicked.
/// </summary>
internal sealed unsafe class MenuItemClickedArgs : MenuArgs, IMenuItemClickedArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItemClickedArgs"/> class.
    /// </summary>
    /// <param name="openSubmenu">Callback for opening a submenu.</param>
    /// <param name="addon">Addon associated with the context menu.</param>
    /// <param name="agent">Agent associated with the context menu.</param>
    /// <param name="type">The type of context menu.</param>
    /// <param name="eventInterfaces">List of AtkEventInterfaces associated with the context menu.</param>
    internal MenuItemClickedArgs(Action<SeString?, IReadOnlyList<IMenuItem>> openSubmenu, AtkUnitBase* addon, AgentInterface* agent, ContextMenuType type, IReadOnlySet<nint> eventInterfaces)
        : base(addon, agent, type, eventInterfaces)
    {
        this.OnOpenSubmenu = openSubmenu;
    }

    private Action<SeString?, IReadOnlyList<IMenuItem>> OnOpenSubmenu { get; }

    /// <inheritdoc/>
    public void OpenSubmenu(SeString name, IReadOnlyList<IMenuItem> items) =>
        this.OnOpenSubmenu(name, items);

    /// <inheritdoc/>
    public void OpenSubmenu(IReadOnlyList<IMenuItem> items) =>
        this.OnOpenSubmenu(null, items);
}

/// <summary>
/// An interface representing the callback args used when a menu item is clicked.
/// </summary>
public interface IMenuItemClickedArgs
{
    /// <summary>
    /// Opens a submenu with the given name and items.
    /// </summary>
    /// <param name="name">The name of the submenu, displayed at the top.</param>
    /// <param name="items">The items to display in the submenu.</param>
    void OpenSubmenu(SeString name, IReadOnlyList<IMenuItem> items);

    /// <summary>
    /// Opens a submenu with the given items.
    /// </summary>
    /// <param name="items">The items to display in the submenu.</param>
    void OpenSubmenu(IReadOnlyList<IMenuItem> items);

    /// <summary>
    /// Gets a list of AtkEventInterface pointers associated with the context menu.
    /// Only available with <see cref="ContextMenuType.Default"/>.
    /// Almost always an agent pointer. You can use this to find out what type of context menu it is.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the context menu is not a <see cref="ContextMenuType.Default"/>.</exception>
    IReadOnlySet<nint> EventInterfaces { get; }

    /// <summary>
    /// Gets the name of the addon that opened the context menu.
    /// </summary>
    string? AddonName { get; }

    /// <summary>
    /// Gets the memory pointer of the addon that opened the context menu.
    /// </summary>
    nint AddonPtr { get; }

    /// <summary>
    /// Gets the memory pointer of the agent that opened the context menu.
    /// </summary>
    nint AgentPtr { get; }

    /// <summary>
    /// Gets the type of the context menu.
    /// </summary>
    ContextMenuType MenuType { get; }

    /// <summary>
    /// Gets the target info of the context menu. The actual type depends on <see cref="MenuType"/>.
    /// <see cref="ContextMenuType.Default"/> signifies a <see cref="MenuTargetDefault"/>.
    /// <see cref="ContextMenuType.Inventory"/> signifies a <see cref="MenuTargetInventory"/>.
    /// </summary>
    MenuTarget Target { get; }
}

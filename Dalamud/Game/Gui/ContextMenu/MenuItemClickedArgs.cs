using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// An interface representing the callback args used when a menu item is clicked.
/// </summary>
public interface IMenuItemClickedArgs : IMenuArgs
{
    /// <summary>
    /// Opens a submenu with the given name and items.
    /// </summary>
    /// <param name="name">The name of the submenu, displayed at the top.</param>
    /// <param name="items">The items to display in the submenu.</param>
    void OpenSubmenu(ReadOnlySeString name, IReadOnlyList<IMenuItem> items);

    /// <summary>
    /// Opens a submenu with the given items.
    /// </summary>
    /// <param name="items">The items to display in the submenu.</param>
    void OpenSubmenu(IReadOnlyList<IMenuItem> items);
}

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
    internal MenuItemClickedArgs(Action<ReadOnlySeString, IReadOnlyList<IMenuItem>> openSubmenu, AtkUnitBase* addon, AgentInterface* agent, ContextMenuType type, IReadOnlySet<nint> eventInterfaces)
        : base(addon, agent, type, eventInterfaces)
    {
        this.OnOpenSubmenu = openSubmenu;
    }

    private Action<ReadOnlySeString, IReadOnlyList<IMenuItem>> OnOpenSubmenu { get; }

    /// <inheritdoc/>
    public void OpenSubmenu(ReadOnlySeString name, IReadOnlyList<IMenuItem> items) =>
        this.OnOpenSubmenu(name, items);

    /// <inheritdoc/>
    public void OpenSubmenu(IReadOnlyList<IMenuItem> items) =>
        this.OnOpenSubmenu(default, items);
}

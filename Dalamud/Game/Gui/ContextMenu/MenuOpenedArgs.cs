using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// An interface representing the callback args used when a menu item is opened.
/// </summary>
public interface IMenuOpenedArgs : IMenuArgs
{
    /// <summary>
    /// Adds a custom menu item to the context menu.
    /// </summary>
    /// <param name="item">The menu item to add.</param>
    void AddMenuItem(MenuItem item);
}

/// <summary>
/// Callback args used when a menu item is opened.
/// </summary>
internal sealed unsafe class MenuOpenedArgs : MenuArgs, IMenuOpenedArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuOpenedArgs"/> class.
    /// </summary>
    /// <param name="addMenuItem">Callback for adding a custom menu item.</param>
    /// <param name="addon">Addon associated with the context menu.</param>
    /// <param name="agent">Agent associated with the context menu.</param>
    /// <param name="type">The type of context menu.</param>
    /// <param name="eventInterfaces">List of AtkEventInterfaces associated with the context menu.</param>
    internal MenuOpenedArgs(Action<MenuItem> addMenuItem, AtkUnitBase* addon, AgentInterface* agent, ContextMenuType type, IReadOnlySet<nint> eventInterfaces)
        : base(addon, agent, type, eventInterfaces)
    {
        this.OnAddMenuItem = addMenuItem;
    }

    private Action<MenuItem> OnAddMenuItem { get; }

    /// <inheritdoc/>
    public void AddMenuItem(MenuItem item) =>
        this.OnAddMenuItem(item);
}

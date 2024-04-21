using System.Collections.Generic;

using Dalamud.Memory;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Base class for <see cref="IContextMenu"/> menu args.
/// </summary>
public abstract unsafe class MenuArgs
{
    private IReadOnlySet<nint>? eventInterfaces;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuArgs"/> class.
    /// </summary>
    /// <param name="addon">Addon associated with the context menu.</param>
    /// <param name="agent">Agent associated with the context menu.</param>
    /// <param name="type">The type of context menu.</param>
    /// <param name="eventInterfaces">List of AtkEventInterfaces associated with the context menu.</param>
    protected internal MenuArgs(AtkUnitBase* addon, AgentInterface* agent, ContextMenuType type, IReadOnlySet<nint>? eventInterfaces)
    {
        this.AddonName = addon != null ? MemoryHelper.ReadString((nint)addon->Name, 32) : null;
        this.AddonPtr = (nint)addon;
        this.AgentPtr = (nint)agent;
        this.MenuType = type;
        this.eventInterfaces = eventInterfaces;
        this.Target = type switch
        {
            ContextMenuType.Default => new MenuTargetDefault((AgentContext*)agent),
            ContextMenuType.Inventory => new MenuTargetInventory((AgentInventoryContext*)agent),
            _ => throw new ArgumentException("Invalid context menu type", nameof(type)),
        };
    }

    /// <summary>
    /// Gets the name of the addon that opened the context menu.
    /// </summary>
    public string? AddonName { get; }

    /// <summary>
    /// Gets the memory pointer of the addon that opened the context menu.
    /// </summary>
    public nint AddonPtr { get; }

    /// <summary>
    /// Gets the memory pointer of the agent that opened the context menu.
    /// </summary>
    public nint AgentPtr { get; }

    /// <summary>
    /// Gets the type of the context menu.
    /// </summary>
    public ContextMenuType MenuType { get; }

    /// <summary>
    /// Gets the target info of the context menu. The actual type depends on <see cref="MenuType"/>.
    /// <see cref="ContextMenuType.Default"/> signifies a <see cref="MenuTargetDefault"/>.
    /// <see cref="ContextMenuType.Inventory"/> signifies a <see cref="MenuTargetInventory"/>.
    /// </summary>
    public MenuTarget Target { get; }

    /// <summary>
    /// Gets a list of AtkEventInterface pointers associated with the context menu.
    /// Only available with <see cref="ContextMenuType.Default"/>.
    /// Almost always an agent pointer. You can use this to find out what type of context menu it is.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the context menu is not a <see cref="ContextMenuType.Default"/>.</exception>
    public IReadOnlySet<nint> EventInterfaces 
    {
        get
        {
            if (this.MenuType is ContextMenuType.Default)
            {
                return this.eventInterfaces ?? new HashSet<nint>();
            }
            else
            {
                throw new InvalidOperationException("Not a default context menu");
            }
        }
    }
}

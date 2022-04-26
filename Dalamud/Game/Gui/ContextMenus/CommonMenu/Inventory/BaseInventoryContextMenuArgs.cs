using System;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu.Inventory;

/// <summary>
/// The base class for inventory context menu arguments
/// </summary>
public abstract class BaseInventoryContextMenuArgs {
    /// <summary>
    /// Pointer to the context menu addon.
    /// </summary>
    public IntPtr Addon { get; }

    /// <summary>
    /// Pointer to the context menu agent.
    /// </summary>
    public IntPtr Agent { get; }

    /// <summary>
    /// The name of the addon containing this context menu, if any.
    /// </summary>
    public string? ParentAddonName { get; }

    /// <summary>
    /// The ID of the item this context menu is for.
    /// </summary>
    public uint ItemId { get; }

    /// <summary>
    /// The amount of the item this context menu is for.
    /// </summary>
    public uint ItemAmount { get; }

    /// <summary>
    /// If the item this context menu is for is high-quality.
    /// </summary>
    public bool ItemHq { get; }

    internal BaseInventoryContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq) {
        this.Addon = addon;
        this.Agent = agent;
        this.ParentAddonName = parentAddonName;
        this.ItemId = itemId;
        this.ItemAmount = itemAmount;
        this.ItemHq = itemHq;
    }
}

using System;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// The base class for inventory context menu arguments.
/// </summary>
public abstract class BaseInventoryContextMenuArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseInventoryContextMenuArgs"/> class.
    /// </summary>
    /// <param name="addon">addon.</param>
    /// <param name="agent">agent.</param>
    /// <param name="parentAddonName">parentAddonName.</param>
    /// <param name="itemId">itemId.</param>
    /// <param name="itemAmount">itemAmount.</param>
    /// <param name="itemHq">itemHq.</param>
    internal BaseInventoryContextMenuArgs(IntPtr addon, IntPtr agent, string? parentAddonName, uint itemId, uint itemAmount, bool itemHq)
    {
        this.Addon = addon;
        this.Agent = agent;
        this.ParentAddonName = parentAddonName;
        this.ItemId = itemId;
        this.ItemAmount = itemAmount;
        this.ItemHq = itemHq;
    }

    /// <summary>
    /// Gets pointer to the context menu addon.
    /// </summary>
    public IntPtr Addon { get; }

    /// <summary>
    /// Gets pointer to the context menu agent.
    /// </summary>
    public IntPtr Agent { get; }

    /// <summary>
    /// Gets the name of the addon containing this context menu, if any.
    /// </summary>
    public string? ParentAddonName { get; }

    /// <summary>
    /// Gets the ID of the item this context menu is for.
    /// </summary>
    public uint ItemId { get; }

    /// <summary>
    /// Gets the amount of the item this context menu is for.
    /// </summary>
    public uint ItemAmount { get; }

    /// <summary>
    /// Gets a value indicating whether if the item this context menu is for is high-quality.
    /// </summary>
    public bool ItemHq { get; }
}

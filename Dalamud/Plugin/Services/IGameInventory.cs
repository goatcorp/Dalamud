using System.Collections.Generic;

using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for the in-game inventory.
/// </summary>
public interface IGameInventory : IDalamudService
{
    /// <summary>
    /// Delegate function to be called when inventories have been changed.
    /// This delegate sends the entire set of changes recorded.
    /// </summary>
    /// <param name="events">The events.</param>
    delegate void InventoryChangelogDelegate(IReadOnlyCollection<InventoryEventArgs> events);

    /// <summary>
    /// Delegate function to be called for each change to inventories.
    /// This delegate sends individual events for changes.
    /// </summary>
    /// <param name="type">The event try that triggered this message.</param>
    /// <param name="data">Data for the triggered event.</param>
    delegate void InventoryChangedDelegate(GameInventoryEvent type, InventoryEventArgs data);

    /// <summary>
    /// Delegate function to be called for each change to inventories.
    /// This delegate sends individual events for changes.
    /// </summary>
    /// <typeparam name="T">The event arg type.</typeparam>
    /// <param name="data">Data for the triggered event.</param>
    delegate void InventoryChangedDelegate<in T>(T data) where T : InventoryEventArgs;

    /// <summary>
    /// Event that is fired when the inventory has been changed.<br />
    /// Note that some events, such as <see cref="ItemAdded"/>, <see cref="ItemRemoved"/>, and <see cref="ItemChanged"/>
    /// currently is subject to reinterpretation as <see cref="ItemMoved"/>, <see cref="ItemMerged"/>, and
    /// <see cref="ItemSplit"/>.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation.
    /// </summary>
    event InventoryChangelogDelegate InventoryChanged;

    /// <summary>
    /// Event that is fired when the inventory has been changed, without trying to interpret two inventory slot changes
    /// as a move event as appropriate.<br />
    /// In other words, <see cref="GameInventoryEvent.Moved"/>, <see cref="GameInventoryEvent.Merged"/>, and
    /// <see cref="GameInventoryEvent.Split"/> currently do not fire in this event.
    /// </summary>
    event InventoryChangelogDelegate InventoryChangedRaw;

    /// <summary>
    /// Event that is fired when an item is added to an inventory.<br />
    /// If this event is a part of multi-step event, then this event will not be called.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation. 
    /// </summary>
    event InventoryChangedDelegate ItemAdded;

    /// <summary>
    /// Event that is fired when an item is removed from an inventory.<br />
    /// If this event is a part of multi-step event, then this event will not be called.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation.
    /// </summary>
    event InventoryChangedDelegate ItemRemoved;

    /// <summary>
    /// Event that is fired when an items properties are changed.<br />
    /// If this event is a part of multi-step event, then this event will not be called.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation.
    /// </summary>
    event InventoryChangedDelegate ItemChanged;

    /// <summary>
    /// Event that is fired when an item is moved from one inventory into another.
    /// </summary>
    event InventoryChangedDelegate ItemMoved;

    /// <summary>
    /// Event that is fired when an item is split from one stack into two.
    /// </summary>
    event InventoryChangedDelegate ItemSplit;

    /// <summary>
    /// Event that is fired when an item is merged from two stacks into one.
    /// </summary>
    event InventoryChangedDelegate ItemMerged;

    /// <inheritdoc cref="ItemAdded"/>
    event InventoryChangedDelegate<InventoryItemAddedArgs> ItemAddedExplicit;

    /// <inheritdoc cref="ItemRemoved"/>
    event InventoryChangedDelegate<InventoryItemRemovedArgs> ItemRemovedExplicit;

    /// <inheritdoc cref="ItemChanged"/>
    event InventoryChangedDelegate<InventoryItemChangedArgs> ItemChangedExplicit;

    /// <inheritdoc cref="ItemMoved"/>
    event InventoryChangedDelegate<InventoryItemMovedArgs> ItemMovedExplicit;

    /// <inheritdoc cref="ItemSplit"/>
    event InventoryChangedDelegate<InventoryItemSplitArgs> ItemSplitExplicit;

    /// <inheritdoc cref="ItemMerged"/>
    event InventoryChangedDelegate<InventoryItemMergedArgs> ItemMergedExplicit;

    /// <summary>
    /// Gets all item slots of the specified inventory type.
    /// </summary>
    /// <param name="type">The type of inventory to get the items for.</param>
    /// <returns>A read-only span of all items in the specified inventory type.</returns>
    ReadOnlySpan<GameInventoryItem> GetInventoryItems(GameInventoryType type);
}

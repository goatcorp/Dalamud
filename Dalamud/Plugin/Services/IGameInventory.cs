using System.Collections.Generic;

using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryChangeArgsTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for the in-game inventory.
/// </summary>
public interface IGameInventory
{
    /// <summary>
    /// Delegate function to be called when inventories have been changed.
    /// This delegate sends the entire set of changes recorded.
    /// </summary>
    /// <param name="events">The events.</param>
    public delegate void InventoryChangelogDelegate(IReadOnlyCollection<InventoryEventArgs> events);

    /// <summary>
    /// Delegate function to be called for each change to inventories.
    /// This delegate sends individual events for changes.
    /// </summary>
    /// <param name="type">The event try that triggered this message.</param>
    /// <param name="data">Data for the triggered event.</param>
    public delegate void InventoryChangedDelegate(GameInventoryEvent type, InventoryEventArgs data);
    
    /// <summary>
    /// Event that is fired when the inventory has been changed.
    /// </summary>
    public event InventoryChangelogDelegate InventoryChanged;
    
    /// <summary>
    /// Event that is fired when the inventory has been changed, without trying to interpret two inventory slot changes
    /// as a move event as appropriate.<br />
    /// In other words, <see cref="GameInventoryEvent.Moved"/> does not fire in this event.
    /// </summary>
    public event InventoryChangelogDelegate InventoryChangedRaw;

    /// <summary>
    /// Event that is fired when an item is added to an inventory.<br />
    /// If an accompanying item remove event happens, then <see cref="ItemMoved"/> will be called instead.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation. 
    /// </summary>
    public event InventoryChangedDelegate ItemAdded;

    /// <summary>
    /// Event that is fired when an item is removed from an inventory.<br />
    /// If an accompanying item add event happens, then <see cref="ItemMoved"/> will be called instead.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation.
    /// </summary>
    public event InventoryChangedDelegate ItemRemoved;

    /// <summary>
    /// Event that is fired when an items properties are changed.<br />
    /// If an accompanying item change event happens, then <see cref="ItemMoved"/> will be called instead.<br />
    /// Use <see cref="InventoryChangedRaw"/> if you do not want such reinterpretation.
    /// </summary>
    public event InventoryChangedDelegate ItemChanged;

    /// <summary>
    /// Event that is fired when an item is moved from one inventory into another.
    /// </summary>
    public event InventoryChangedDelegate ItemMoved;
}

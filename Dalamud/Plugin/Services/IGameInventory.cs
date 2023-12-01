using Dalamud.Game.GameInventory;

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
    public delegate void InventoryChangelogDelegate(ReadOnlySpan<InventoryEventArgs> events);

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
    /// Event that is fired when an item is added to an inventory.
    /// </summary>
    public event InventoryChangedDelegate ItemAdded;

    /// <summary>
    /// Event that is fired when an item is removed from an inventory.
    /// </summary>
    public event InventoryChangedDelegate ItemRemoved;

    /// <summary>
    /// Event that is fired when an item is moved from one inventory into another.
    /// </summary>
    public event InventoryChangedDelegate ItemMoved;

    /// <summary>
    /// Event that is fired when an items properties are changed.
    /// </summary>
    public event InventoryChangedDelegate ItemChanged;
}

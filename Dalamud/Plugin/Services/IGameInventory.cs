using Dalamud.Game.Inventory;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for the in-game inventory.
/// </summary>
public interface IGameInventory
{
    /// <summary>
    /// Delegate function for when an item is moved from one inventory to the next.
    /// </summary>
    /// <param name="source">Which inventory the item was moved from.</param>
    /// <param name="sourceSlot">The slot this item was moved from.</param>
    /// <param name="destination">Which inventory the item was moved to.</param>
    /// <param name="destinationSlot">The slot this item was moved to.</param>
    /// <param name="item">The item moved.</param>
    public delegate void OnItemMovedDelegate(GameInventoryType source, uint sourceSlot, GameInventoryType destination, uint destinationSlot, GameInventoryItem item);

    /// <summary>
    /// Delegate function for when an item is removed from an inventory.
    /// </summary>
    /// <param name="source">Which inventory the item was removed from.</param>
    /// <param name="sourceSlot">The slot this item was removed from.</param>
    /// <param name="item">The item removed.</param>
    public delegate void OnItemRemovedDelegate(GameInventoryType source, uint sourceSlot, GameInventoryItem item);

    /// <summary>
    /// Delegate function for when an item is added to an inventory.
    /// </summary>
    /// <param name="destination">Which inventory the item was added to.</param>
    /// <param name="destinationSlot">The slot this item was added to.</param>
    /// <param name="item">The item added.</param>
    public delegate void OnItemAddedDelegate(GameInventoryType destination, uint destinationSlot, GameInventoryItem item);

    /// <summary>
    /// Delegate function for when an items properties are changed.
    /// </summary>
    /// <param name="inventory">Which inventory the item that was changed is in.</param>
    /// <param name="slot">The slot the item that was changed is in.</param>
    /// <param name="item">The item changed.</param>
    public delegate void OnItemChangedDelegate(GameInventoryType inventory, uint slot, GameInventoryItem item);

    /// <summary>
    /// Event that is fired when an item is moved from one inventory to another.
    /// </summary>
    public event OnItemMovedDelegate ItemMoved;
    
    /// <summary>
    /// Event that is fired when an item is removed from one inventory.
    /// </summary>
    /// <remarks>
    /// This event will also be fired when an item is moved from one inventory to another.
    /// </remarks>
    public event OnItemRemovedDelegate ItemRemoved;
    
    /// <summary>
    /// Event that is fired when an item is added to one inventory.
    /// </summary>
    /// <remarks>
    /// This event will also be fired when an item is moved from one inventory to another.
    /// </remarks>
    public event OnItemAddedDelegate ItemAdded;
    
    /// <summary>
    /// Event that is fired when an items properties are changed.
    /// </summary>
    public event OnItemChangedDelegate ItemChanged;
}

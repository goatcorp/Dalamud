namespace Dalamud.Game.GameInventory;

/// <summary>
/// Represents the data associated with an item being removed from an inventory.
/// </summary>
public class InventoryItemRemovedArgs : InventoryEventArgs
{
    /// <inheritdoc/>
    public override GameInventoryEvent Type => GameInventoryEvent.Removed;

    /// <summary>
    /// Gets the inventory this item was removed from.
    /// </summary>
    required public GameInventoryType Inventory { get; init; }
    
    /// <summary>
    /// Gets the slot this item was removed from.
    /// </summary>
    required public uint Slot { get; init; }
}

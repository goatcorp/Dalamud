namespace Dalamud.Game.GameInventory;

/// <summary>
/// Represents the data associated with an item being added to an inventory.
/// </summary>
public class InventoryItemAddedArgs : InventoryEventArgs
{
    /// <inheritdoc/>
    public override GameInventoryEvent Type => GameInventoryEvent.Added;
    
    /// <summary>
    /// Gets the inventory this item was added to.
    /// </summary>
    required public GameInventoryType Inventory { get; init; }
    
    /// <summary>
    /// Gets the slot this item was added to.
    /// </summary>
    required public uint Slot { get; init; }
}

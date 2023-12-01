namespace Dalamud.Game.GameInventory;

/// <summary>
/// Represents the data associated with an item being moved from one inventory and added to another.
/// </summary>
public class InventoryItemMovedArgs : InventoryEventArgs
{
    /// <inheritdoc/>
    public override GameInventoryEvent Type => GameInventoryEvent.Moved;
    
    /// <summary>
    /// Gets the inventory this item was moved from.
    /// </summary>
    required public GameInventoryType SourceInventory { get; init; }
    
    /// <summary>
    /// Gets the inventory this item was moved to.
    /// </summary>
    required public GameInventoryType TargetInventory { get; init; }
    
    /// <summary>
    /// Gets the slot this item was moved from.
    /// </summary>
    required public uint SourceSlot { get; init; }
    
    /// <summary>
    /// Gets the slot this item was moved to.
    /// </summary>
    required public uint TargetSlot { get; init; }
}

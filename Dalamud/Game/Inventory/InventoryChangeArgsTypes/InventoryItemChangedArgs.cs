namespace Dalamud.Game.GameInventory;

/// <summary>
/// Represents the data associated with an items properties being changed.
/// This also includes an items stack count changing.
/// </summary>
public class InventoryItemChangedArgs : InventoryEventArgs
{
    /// <inheritdoc/>
    public override GameInventoryEvent Type => GameInventoryEvent.Changed;
    
    /// <summary>
    /// Gets the inventory this item is in.
    /// </summary>
    required public GameInventoryType Inventory { get; init; }
    
    /// <summary>
    /// Gets the inventory slot this item is in.
    /// </summary>
    required public uint Slot { get; init; }
    
    /// <summary>
    /// Gets the state of the item from before it was changed.
    /// </summary>
    required public GameInventoryItem OldItemState { get; init; }
}

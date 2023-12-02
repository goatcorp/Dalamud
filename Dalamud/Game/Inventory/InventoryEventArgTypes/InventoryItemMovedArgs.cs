namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being moved from one inventory and added to another.
/// </summary>
public sealed class InventoryItemMovedArgs : InventoryComplexEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemMovedArgs"/> class.
    /// </summary>
    /// <param name="sourceEvent">The item at before slot.</param>
    /// <param name="targetEvent">The item at after slot.</param>
    internal InventoryItemMovedArgs(InventoryEventArgs sourceEvent, InventoryEventArgs targetEvent)
        : base(GameInventoryEvent.Moved, sourceEvent, targetEvent)
    {
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{this.Type}(item({this.Item.ItemId}) from {this.SourceInventory}#{this.SourceSlot} to {this.TargetInventory}#{this.TargetSlot})";
}

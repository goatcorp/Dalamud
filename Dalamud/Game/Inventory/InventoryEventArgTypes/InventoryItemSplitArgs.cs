namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being split from one stack into two.
/// </summary>
public sealed class InventoryItemSplitArgs : InventoryComplexEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemSplitArgs"/> class.
    /// </summary>
    /// <param name="sourceEvent">The item at before slot.</param>
    /// <param name="targetEvent">The item at after slot.</param>
    internal InventoryItemSplitArgs(InventoryEventArgs sourceEvent, InventoryEventArgs targetEvent)
        : base(GameInventoryEvent.Split, sourceEvent, targetEvent)
    {
    }

    /// <inheritdoc/>
    public override string ToString() =>
        this.SourceEvent is InventoryItemChangedArgs iica
            ? $"{this.Type}(" +
              $"item({this.Item.ItemId}), " +
              $"{this.SourceInventory}#{this.SourceSlot}({iica.OldItemState.Quantity} to {iica.Item.Quantity}), " +
              $"{this.TargetInventory}#{this.TargetSlot}(0 to {this.Item.Quantity}))"
            : base.ToString();
}

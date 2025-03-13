namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being merged from two stacks into one.
/// </summary>
public sealed class InventoryItemMergedArgs : InventoryComplexEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemMergedArgs"/> class.
    /// </summary>
    /// <param name="sourceEvent">The item at before slot.</param>
    /// <param name="targetEvent">The item at after slot.</param>
    internal InventoryItemMergedArgs(InventoryEventArgs sourceEvent, InventoryEventArgs targetEvent)
        : base(GameInventoryEvent.Merged, sourceEvent, targetEvent)
    {
    }

    /// <inheritdoc/>
    public override string ToString() =>
        this.TargetEvent is InventoryItemChangedArgs iica
            ? $"{this.Type}(" +
              $"item({this.Item.ItemId}), " +
              $"{this.SourceInventory}#{this.SourceSlot}({this.SourceEvent.Item.Quantity} to 0), " +
              $"{this.TargetInventory}#{this.TargetSlot}({iica.OldItemState.Quantity} to {iica.Item.Quantity}))"
            : base.ToString();
}

namespace Dalamud.Game.Inventory.InventoryChangeArgsTypes;

/// <summary>
/// Represents the data associated with an item being moved from one inventory and added to another.
/// </summary>
public class InventoryItemMovedArgs : InventoryEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemMovedArgs"/> class.
    /// </summary>
    /// <param name="sourceEvent">The item at before slot.</param>
    /// <param name="targetEvent">The item at after slot.</param>
    internal InventoryItemMovedArgs(InventoryEventArgs sourceEvent, InventoryEventArgs targetEvent)
        : base(GameInventoryEvent.Moved, targetEvent.Item)
    {
        this.SourceEvent = sourceEvent;
        this.TargetEvent = targetEvent;
    }

    /// <summary>
    /// Gets the inventory this item was moved from.
    /// </summary>
    public GameInventoryType SourceInventory => this.SourceEvent.Item.ContainerType;

    /// <summary>
    /// Gets the inventory this item was moved to.
    /// </summary>
    public GameInventoryType TargetInventory => this.Item.ContainerType;

    /// <summary>
    /// Gets the slot this item was moved from.
    /// </summary>
    public uint SourceSlot => this.SourceEvent.Item.InventorySlot;

    /// <summary>
    /// Gets the slot this item was moved to.
    /// </summary>
    public uint TargetSlot => this.Item.InventorySlot;

    /// <summary>
    /// Gets the associated source event.
    /// </summary>
    internal InventoryEventArgs SourceEvent { get; }

    /// <summary>
    /// Gets the associated target event.
    /// </summary>
    internal InventoryEventArgs TargetEvent { get; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"<{this.Type}> (Item #{this.Item.ItemId}) from (slot {this.SourceSlot} in {this.SourceInventory}) to (slot {this.TargetSlot} in {this.TargetInventory})";
}

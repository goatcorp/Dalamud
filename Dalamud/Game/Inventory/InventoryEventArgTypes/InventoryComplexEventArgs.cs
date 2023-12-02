namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being affected across different slots, possibly in different containers.
/// </summary>
public abstract class InventoryComplexEventArgs : InventoryEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryComplexEventArgs"/> class.
    /// </summary>
    /// <param name="type">Type of the event.</param>
    /// <param name="sourceEvent">The item at before slot.</param>
    /// <param name="targetEvent">The item at after slot.</param>
    internal InventoryComplexEventArgs(
        GameInventoryEvent type, InventoryEventArgs sourceEvent, InventoryEventArgs targetEvent)
        : base(type, targetEvent.Item)
    {
        this.SourceEvent = sourceEvent;
        this.TargetEvent = targetEvent;
    }

    /// <summary>
    /// Gets the inventory this item was at.
    /// </summary>
    public GameInventoryType SourceInventory => this.SourceEvent.Item.ContainerType;

    /// <summary>
    /// Gets the inventory this item now is.
    /// </summary>
    public GameInventoryType TargetInventory => this.Item.ContainerType;

    /// <summary>
    /// Gets the slot this item was at.
    /// </summary>
    public uint SourceSlot => this.SourceEvent.Item.InventorySlot;

    /// <summary>
    /// Gets the slot this item now is.
    /// </summary>
    public uint TargetSlot => this.Item.InventorySlot;

    /// <summary>
    /// Gets the associated source event.
    /// </summary>
    public InventoryEventArgs SourceEvent { get; }

    /// <summary>
    /// Gets the associated target event.
    /// </summary>
    public InventoryEventArgs TargetEvent { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{this.Type}({this.SourceEvent}, {this.TargetEvent})";
}

namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being added to an inventory.
/// </summary>
public sealed class InventoryItemAddedArgs : InventoryEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemAddedArgs"/> class.
    /// </summary>
    /// <param name="item">The item.</param>
    internal InventoryItemAddedArgs(in GameInventoryItem item)
        : base(GameInventoryEvent.Added, item)
    {
    }

    /// <summary>
    /// Gets the inventory this item was added to.
    /// </summary>
    public GameInventoryType Inventory => this.Item.ContainerType;

    /// <summary>
    /// Gets the slot this item was added to.
    /// </summary>
    public uint Slot => this.Item.InventorySlot;
}

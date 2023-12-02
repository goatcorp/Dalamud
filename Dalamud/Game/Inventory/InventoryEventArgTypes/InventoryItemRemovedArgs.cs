namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an item being removed from an inventory.
/// </summary>
public sealed class InventoryItemRemovedArgs : InventoryEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemRemovedArgs"/> class.
    /// </summary>
    /// <param name="item">The item.</param>
    internal InventoryItemRemovedArgs(in GameInventoryItem item)
        : base(GameInventoryEvent.Removed, item)
    {
    }

    /// <summary>
    /// Gets the inventory this item was removed from.
    /// </summary>
    public GameInventoryType Inventory => this.Item.ContainerType;

    /// <summary>
    /// Gets the slot this item was removed from.
    /// </summary>
    public uint Slot => this.Item.InventorySlot;
}

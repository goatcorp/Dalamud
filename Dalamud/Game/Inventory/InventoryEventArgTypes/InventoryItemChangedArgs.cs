namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Represents the data associated with an items properties being changed.
/// This also includes an items stack count changing.
/// </summary>
public sealed class InventoryItemChangedArgs : InventoryEventArgs
{
    private readonly GameInventoryItem oldItemState;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemChangedArgs"/> class.
    /// </summary>
    /// <param name="oldItem">The item before change.</param>
    /// <param name="newItem">The item after change.</param>
    internal InventoryItemChangedArgs(in GameInventoryItem oldItem, in GameInventoryItem newItem)
        : base(GameInventoryEvent.Changed, newItem)
    {
        this.oldItemState = oldItem;
    }

    /// <summary>
    /// Gets the inventory this item is in.
    /// </summary>
    public GameInventoryType Inventory => this.Item.ContainerType;

    /// <summary>
    /// Gets the inventory slot this item is in.
    /// </summary>
    public uint Slot => this.Item.InventorySlot;

    /// <summary>
    /// Gets the state of the item from before it was changed.
    /// <remarks><em>This is a copy of the item data.</em></remarks>
    /// </summary>
    // impl note: see InventoryEventArgs.Item.
    public ref readonly GameInventoryItem OldItemState => ref this.oldItemState;
}

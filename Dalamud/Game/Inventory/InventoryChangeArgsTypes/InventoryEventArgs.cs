namespace Dalamud.Game.GameInventory;

/// <summary>
/// Abstract base class representing inventory changed events.
/// </summary>
public abstract class InventoryEventArgs
{
    /// <summary>
    /// Gets the type of event for these args.
    /// </summary>
    public abstract GameInventoryEvent Type { get; }

    /// <summary>
    /// Gets the item associated with this event.
    /// <remarks><em>This is a copy of the item data.</em></remarks>
    /// </summary>
    required public GameInventoryItem Item { get; init; }
    
    /// <inheritdoc/>
    public override string ToString() => this.Type switch
    {
        GameInventoryEvent.Empty => $"<{this.Type}>",
        GameInventoryEvent.Added => $"<{this.Type}> ({this.Item})",
        GameInventoryEvent.Removed => $"<{this.Type}> ({this.Item})",
        GameInventoryEvent.Changed => $"<{this.Type}> ({this.Item})", 
        GameInventoryEvent.Moved when this is InventoryItemMovedArgs args => $"<{this.Type}> (Item #{this.Item.ItemId}) from (slot {args.SourceSlot} in {args.SourceInventory}) to (slot {args.TargetSlot} in {args.TargetInventory})",
        _ => $"<Type={this.Type}> {this.Item}",
    };
}

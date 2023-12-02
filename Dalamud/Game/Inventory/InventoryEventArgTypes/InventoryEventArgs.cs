namespace Dalamud.Game.Inventory.InventoryEventArgTypes;

/// <summary>
/// Abstract base class representing inventory changed events.
/// </summary>
public abstract class InventoryEventArgs
{
    private readonly GameInventoryItem item;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryEventArgs"/> class.
    /// </summary>
    /// <param name="type">Type of the event.</param>
    /// <param name="item">Item about the event.</param>
    protected InventoryEventArgs(GameInventoryEvent type, in GameInventoryItem item)
    {
        this.Type = type;
        this.item = item;
    }

    /// <summary>
    /// Gets the type of event for these args.
    /// </summary>
    public GameInventoryEvent Type { get; }

    /// <summary>
    /// Gets the item associated with this event.
    /// <remarks><em>This is a copy of the item data.</em></remarks>
    /// </summary>
    // impl note: we return a ref readonly view, to avoid making copies every time this property is accessed.
    // see: https://devblogs.microsoft.com/premier-developer/avoiding-struct-and-readonly-reference-performance-pitfalls-with-errorprone-net/
    // "Consider using ref readonly locals and ref return for library code"
    public ref readonly GameInventoryItem Item => ref this.item;

    /// <inheritdoc/>
    public override string ToString() => $"{this.Type}({this.Item})";
}

using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Game.Inventory.InventoryChangeArgsTypes;

/// <summary>
/// Abstract base class representing inventory changed events.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1206:Declaration keywords should follow order", Justification = "It literally says <access modifiers>, <static>, and then <all other keywords>. required is not an access modifier.")]
public abstract class InventoryEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryEventArgs"/> class.
    /// </summary>
    /// <param name="type">Type of the event.</param>
    /// <param name="item">Item about the event.</param>
    protected InventoryEventArgs(GameInventoryEvent type, in GameInventoryItem item)
    {
        this.Type = type;
        this.Item = item;
    }

    /// <summary>
    /// Gets the type of event for these args.
    /// </summary>
    public GameInventoryEvent Type { get; }

    /// <summary>
    /// Gets the item associated with this event.
    /// <remarks><em>This is a copy of the item data.</em></remarks>
    /// </summary>
    public GameInventoryItem Item { get; }
    
    /// <inheritdoc/>
    public override string ToString() => $"<{this.Type}> ({this.Item})";
}

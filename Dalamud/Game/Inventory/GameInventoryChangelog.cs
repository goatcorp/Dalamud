namespace Dalamud.Game.Inventory;

/// <summary>
/// Class representing an inventory item change event.
/// </summary>
internal class GameInventoryItemChangelog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameInventoryItemChangelog"/> class.
    /// </summary>
    /// <param name="state">Item state.</param>
    /// <param name="item">Item.</param>
    internal GameInventoryItemChangelog(GameInventoryChangelogState state, GameInventoryItem item)
    {
        this.State = state;
        this.Item = item;
    }
    
    /// <summary>
    /// Gets the state of this changelog event.
    /// </summary>
    internal GameInventoryChangelogState State { get; }
    
    /// <summary>
    /// Gets the item for this changelog event.
    /// </summary>
    internal GameInventoryItem Item { get; }
}

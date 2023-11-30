namespace Dalamud.Game.Inventory;

/// <summary>
/// Class representing a item's changelog state.
/// </summary>
internal enum GameInventoryChangelogState
{
    /// <summary>
    /// Item was added to an inventory.
    /// </summary>
    Added,
    
    /// <summary>
    /// Item was removed from an inventory.
    /// </summary>
    Removed,
}

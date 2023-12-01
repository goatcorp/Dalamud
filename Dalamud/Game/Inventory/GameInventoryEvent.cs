namespace Dalamud.Game.Inventory;

/// <summary>
/// Class representing a item's changelog state.
/// </summary>
public enum GameInventoryEvent
{
    /// <summary>
    /// A value indicating that there was no event.<br />
    /// You should not see this value, unless you explicitly used it yourself, or APIs using this enum say otherwise.
    /// </summary>
    Empty = 0,

    /// <summary>
    /// Item was added to an inventory.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Item was removed from an inventory.
    /// </summary>
    Removed = 2,

    /// <summary>
    /// Properties are changed for an item in an inventory.
    /// </summary>
    Changed = 3,

    /// <summary>
    /// Item has been moved, possibly across different inventories.
    /// </summary>
    Moved = 4,

    /// <summary>
    /// Item has been split into two stacks from one, possibly across different inventories.
    /// </summary>
    Split = 5,

    /// <summary>
    /// Item has been merged into one stack from two, possibly across different inventories.
    /// </summary>
    Merged = 6,
}

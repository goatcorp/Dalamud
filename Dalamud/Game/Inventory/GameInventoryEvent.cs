namespace Dalamud.Game.Inventory;

/// <summary>
/// Class representing a item's changelog state.
/// </summary>
[Flags]
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
    Added = 1 << 0,
    
    /// <summary>
    /// Item was removed from an inventory.
    /// </summary>
    Removed = 1 << 1,
    
    /// <summary>
    /// Properties are changed for an item in an inventory.
    /// </summary>
    Changed = 1 << 2,
    
    /// <summary>
    /// Item has been moved, possibly across different inventories.
    /// </summary>
    Moved = 1 << 3,
}

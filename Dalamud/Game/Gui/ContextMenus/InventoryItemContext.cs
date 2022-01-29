namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides inventory item context to a context menu.
    /// </summary>
    public sealed class InventoryItemContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryItemContext"/> class.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <param name="count">The count of the item in the stack.</param>
        /// <param name="isHighQuality">Whether the item is high quality.</param>
        internal InventoryItemContext(uint id, uint count, bool isHighQuality)
        {
            this.Id = id;
            this.Count = count;
            this.IsHighQuality = isHighQuality;
        }

        /// <summary>
        /// Gets the id of the item.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets the count of the item in the stack.
        /// </summary>
        public uint Count { get; }

        /// <summary>
        /// Gets a value indicating whether the item is high quality.
        /// </summary>
        public bool IsHighQuality { get; }
    }
}

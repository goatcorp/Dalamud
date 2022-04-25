namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides game object context to a context menu.
    /// </summary>
    public sealed class GameObjectContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameObjectContext"/> class.
        /// </summary>
        /// <param name="id">The id of the game object.</param>
        /// <param name="contentId">The lower content id of the game object.</param>
        /// <param name="name">The name of the game object.</param>
        /// <param name="worldId">The world id of the game object.</param>
        internal GameObjectContext(uint? id, ulong? contentId, string? name, ushort? worldId)
        {
            this.Id = id;
            this.ContentId = contentId;
            this.Name = name;
            this.WorldId = worldId;
        }

        /// <summary>
        /// Gets the id of the game object.
        /// </summary>
        public uint? Id { get; }

        /// <summary>
        /// Gets the content id of the game object.
        /// </summary>
        public ulong? ContentId { get; }

        /// <summary>
        /// Gets the name of the game object.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the world id of the game object.
        /// </summary>
        public ushort? WorldId { get; }
    }
}

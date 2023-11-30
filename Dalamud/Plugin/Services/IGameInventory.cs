using Dalamud.Game.Inventory;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for the in-game inventory.
/// </summary>
public interface IGameInventory
{
    /// <summary>
    /// Delegate function to be called when inventories have been changed.
    /// </summary>
    /// <param name="events">The events.</param>
    public delegate void InventoryChangeDelegate(ReadOnlySpan<GameInventoryEventArgs> events);
    
    /// <summary>
    /// Event that is fired when the inventory has been changed.
    /// </summary>
    public event InventoryChangeDelegate InventoryChanged;
    
    /// <summary>
    /// Argument for <see cref="InventoryChangeDelegate"/>.
    /// </summary>
    public readonly struct GameInventoryEventArgs
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        public readonly GameInventoryEvent Type;

        /// <summary>
        /// The content of the item in the source inventory.<br />
        /// Relevant if <see cref="Type"/> is <see cref="GameInventoryEvent.Moved"/>, <see cref="GameInventoryEvent.Changed"/>, or <see cref="GameInventoryEvent.Removed"/>.
        /// </summary>
        public readonly GameInventoryItem Source;
        
        /// <summary>
        /// The content of the item in the target inventory<br />
        /// Relevant if <see cref="Type"/> is <see cref="GameInventoryEvent.Moved"/>, <see cref="GameInventoryEvent.Changed"/>, or <see cref="GameInventoryEvent.Added"/>.
        /// </summary>
        public readonly GameInventoryItem Target;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameInventoryEventArgs"/> struct.
        /// </summary>
        /// <param name="type">The type of the event.</param>
        /// <param name="source">The source inventory item.</param>
        /// <param name="target">The target inventory item.</param>
        public GameInventoryEventArgs(GameInventoryEvent type, GameInventoryItem source, GameInventoryItem target)
        {
            this.Type = type;
            this.Source = source;
            this.Target = target;
        }

        /// <summary>
        /// Gets a value indicating whether this instance of <see cref="GameInventoryEventArgs"/> contains no information.
        /// </summary>
        public bool IsEmpty => this.Type == GameInventoryEvent.Empty;

        // TODO: are the following two aliases useful?
        
        /// <summary>
        /// Gets the type of the source inventory.<br />
        /// Relevant for <see cref="GameInventoryEvent.Moved"/> and <see cref="GameInventoryEvent.Removed"/>.
        /// </summary>
        public GameInventoryType SourceType => this.Source.ContainerType;

        /// <summary>
        /// Gets the type of the target inventory.<br />
        /// Relevant for <see cref="GameInventoryEvent.Moved"/>, <see cref="GameInventoryEvent.Changed"/>, and
        /// <see cref="GameInventoryEvent.Added"/>.
        /// </summary>
        public GameInventoryType TargetType => this.Target.ContainerType;

        /// <inheritdoc/>
        public override string ToString() => this.Type switch
        {
            GameInventoryEvent.Empty =>
                $"<{this.Type}>",
            GameInventoryEvent.Added =>
                $"<{this.Type}> ({this.Target})",
            GameInventoryEvent.Removed =>
                $"<{this.Type}> ({this.Source})",
            GameInventoryEvent.Changed or GameInventoryEvent.Moved =>
                $"<{this.Type}> ({this.Source}) to ({this.Target})",
            _ => $"<Type={this.Type}> {this.Source} => {this.Target}",
        };
    }
}

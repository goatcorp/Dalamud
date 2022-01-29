using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// An item in a context menu that with a specific game action.
    /// </summary>
    public class GameContextMenuItem : ContextMenuItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="selectedAction">The game action that will be handled when the item is selected.</param>
        internal GameContextMenuItem(SeString name, byte selectedAction)
            : base(name)
        {
            this.SelectedAction = selectedAction;
        }

        /// <summary>
        /// Gets the game action that will be handled when the item is selected.
        /// </summary>
        public byte SelectedAction { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = (hash * 23) + this.SelectedAction;
                return hash;
            }
        }
    }
}

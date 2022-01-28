using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// An item in a context menu that with a specific game action.
    /// </summary>
    public class GameContextMenuItem : ContextMenuItem
    {
        /// <summary>
        /// The game action that will be handled when the item is selected.
        /// </summary>
        public byte SelectedAction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="selectedAction">The game action that will be handled when the item is selected.</param>
        public GameContextMenuItem(SeString name, byte selectedAction)
            : base(name)
        {
            SelectedAction = selectedAction;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = hash * 23 + SelectedAction;
                return hash;
            }
        }
    }
}

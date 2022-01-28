using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// An item in a context menu that can open a sub context menu.
    /// </summary>
    public class OpenSubContextMenuItem : ContextMenuItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSubContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="opened">The action that will be called when the item is selected.</param>
        internal OpenSubContextMenuItem(SeString name, ContextMenuOpenedDelegate opened)
            : base(name)
        {
            this.Opened = opened;
            this.Indicator = ContextMenuItemIndicator.Next;
        }

        /// <summary>
        /// Gets the action that will be called when the item is selected.
        /// </summary>
        public ContextMenuOpenedDelegate Opened { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + this.Opened.GetHashCode();
                return hash;
            }
        }
    }
}

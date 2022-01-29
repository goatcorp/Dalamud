using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// An item in a context menu with a user defined action.
    /// </summary>
    public sealed class CustomContextMenuItem : ContextMenuItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomContextMenuItem"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="itemSelected">The action that will be called when the item is selected.</param>
        public CustomContextMenuItem(SeString name, CustomContextMenuItemSelectedDelegate itemSelected)
            : base(new SeString().Append(new UIForegroundPayload(539)).Append($"{SeIconChar.BoxedLetterD.ToIconString()} ").Append(new UIForegroundPayload(0)).Append(name))
        {
            this.ItemSelected = itemSelected;
        }

        /// <summary>
        /// Gets the action that will be called when the item is selected.
        /// </summary>
        public CustomContextMenuItemSelectedDelegate ItemSelected { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = (hash * 23) + this.ItemSelected.GetHashCode();
                return hash;
            }
        }
    }
}

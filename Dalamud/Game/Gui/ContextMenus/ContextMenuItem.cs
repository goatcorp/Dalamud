using System.Numerics;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// An item in a context menu.
    /// </summary>
    public abstract class ContextMenuItem
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ContextMenuItem" /> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        internal ContextMenuItem(SeString name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the item.
        /// </summary>
        public SeString Name { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the item is enabled. When enabled, an item is selectable.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the indicator of the item.
        /// </summary>
        public ContextMenuItemIndicator Indicator { get; set; } = ContextMenuItemIndicator.None;

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name.ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + new BigInteger(this.Name.Encode()).GetHashCode();
                hash = (hash * 23) + this.IsEnabled.GetHashCode();
                hash = (hash * 23) + ((int)this.Indicator).GetHashCode();
                return hash;
            }
        }
    }
}

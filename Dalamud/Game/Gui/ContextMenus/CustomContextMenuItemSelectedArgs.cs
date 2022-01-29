namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="CustomContextMenuItemSelectedDelegate"/> methods.
    /// </summary>
    public sealed class CustomContextMenuItemSelectedArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomContextMenuItemSelectedArgs"/> class.
        /// </summary>
        /// <param name="contextMenuOpenedArgs">The currently opened context menu.</param>
        /// <param name="selectedItem">The selected item within the currently opened context menu.</param>
        internal CustomContextMenuItemSelectedArgs(ContextMenuOpenedArgs contextMenuOpenedArgs, CustomContextMenuItem selectedItem)
        {
            this.ContextMenuOpenedArgs = contextMenuOpenedArgs;
            this.SelectedItem = selectedItem;
        }

        /// <summary>
        /// Gets the currently opened context menu.
        /// </summary>
        public ContextMenuOpenedArgs ContextMenuOpenedArgs { get; init; }

        /// <summary>
        /// Gets the selected item within the currently opened context menu.
        /// </summary>
        public CustomContextMenuItem SelectedItem { get; init; }
    }
}

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="CustomContextMenuItemSelectedDelegate"/> methods.
    /// </summary>
    public class CustomContextMenuItemSelectedArgs
    {
        /// <summary>
        /// The currently opened context menu.
        /// </summary>
        public ContextMenuOpenedArgs ContextMenuOpenedArgs { get; init; }

        /// <summary>
        /// The selected item within the currently opened context menu.
        /// </summary>
        public CustomContextMenuItem SelectedItem { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomContextMenuItemSelectedArgs"/> class.
        /// </summary>
        /// <param name="contextMenuOpenedArgs">The currently opened context menu.</param>
        /// <param name="selectedItem">The selected item within the currently opened context menu.</param>
        public CustomContextMenuItemSelectedArgs(ContextMenuOpenedArgs contextMenuOpenedArgs, CustomContextMenuItem selectedItem)
        {
            ContextMenuOpenedArgs = contextMenuOpenedArgs;
            SelectedItem = selectedItem;
        }
    }
}

using System;
using System.Collections.Generic;

using Dalamud.Game.Gui.ContextMenus.OldStructs;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="ContextMenuOpenedDelegate"/> methods.
    /// </summary>
    public sealed unsafe class ContextMenuOpenedArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuOpenedArgs"/> class.
        /// </summary>
        /// <param name="addon">The addon associated with the context menu.</param>
        /// <param name="agent">The agent associated with the context menu.</param>
        /// <param name="parentAddonName">The the name of the parent addon associated with the context menu.</param>
        /// <param name="items">The items in the context menu.</param>
        public ContextMenuOpenedArgs(AddonContextMenu* addon, OldAgentContextInterface* agent, string? parentAddonName, IEnumerable<ContextMenuItem> items)
        {
            this.Addon = addon;
            this.Agent = agent;
            this.ParentAddonName = parentAddonName;
            this.Items = new List<ContextMenuItem>(items);
        }

        /// <summary>
        /// Gets the addon associated with the context menu.
        /// </summary>
        public AddonContextMenu* Addon { get; }

        /// <summary>
        /// Gets the agent associated with the context menu.
        /// </summary>
        public OldAgentContextInterface* Agent { get; }

        /// <summary>
        /// Gets the name of the parent addon associated with the context menu.
        /// </summary>
        public string? ParentAddonName { get; }

        /// <summary>
        /// Gets the title of the context menu.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the game object context associated with the context menu.
        /// </summary>
        public GameObjectContext? GameObjectContext { get; init; }

        /// <summary>
        /// Gets the item context associated with the context menu.
        /// </summary>
        public InventoryItemContext? InventoryItemContext { get; init; }

        /// <summary>
        /// Gets the items in the context menu.
        /// </summary>
        internal List<ContextMenuItem> Items { get; }

        /// <summary>
        /// Append a custom context menu item to this context menu.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        /// <param name="selected">The action to be executed once selected.</param>
        public void AddCustomItem(SeString name, CustomContextMenuItemSelectedDelegate selected) =>
            this.Items.Add(new CustomContextMenuItem(name, selected));

        /// <summary>
        /// Append a custom submenu to this context menu.
        /// Note that these cannot be nested, and will be ignored if they are.
        /// </summary>
        /// <param name="name">The name of the submenu.</param>
        /// <param name="opened">The action to be executed once opened.</param>
        public void AddCustomSubMenu(SeString name, ContextMenuOpenedDelegate opened)
        {
            if (this.GameObjectContext != null)
                throw new Exception("Submenus on GameObjects are not supported yet.");

            this.Items.Add(new OpenSubContextMenuItem(name, opened));
        }
    }
}

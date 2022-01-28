using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides data for <see cref="ContextMenuOpenedDelegate"/> methods.
    /// </summary>
    public unsafe class ContextMenuOpenedArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuOpenedArgs"/> class.
        /// </summary>
        /// <param name="addon">The addon associated with the context menu.</param>
        /// <param name="agent">The agent associated with the context menu.</param>
        /// <param name="parentAddonName">The the name of the parent addon associated with the context menu.</param>
        /// <param name="items">The items in the context menu.</param>
        public ContextMenuOpenedArgs(AddonContextMenu* addon, AgentContextInterface* agent, string? parentAddonName, IEnumerable<ContextMenuItem> items)
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
        public AgentContextInterface* Agent { get; }

        /// <summary>
        /// Gets the name of the parent addon associated with the context menu.
        /// </summary>
        public string? ParentAddonName { get; }

        /// <summary>
        /// Gets the title of the context menu.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the items in the context menu.
        /// </summary>
        public List<ContextMenuItem> Items { get; }

        /// <summary>
        /// Gets the game object context associated with the context menu.
        /// </summary>
        public GameObjectContext? GameObjectContext { get; init; }

        /// <summary>
        /// Gets the item context associated with the context menu.
        /// </summary>
        public InventoryItemContext? InventoryItemContext { get; init; }
    }
}

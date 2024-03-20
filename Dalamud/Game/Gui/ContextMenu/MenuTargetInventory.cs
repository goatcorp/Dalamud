using Dalamud.Game.Inventory;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// Target information on an inventory context menu.
/// </summary>
public sealed unsafe class MenuTargetInventory : MenuTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuTargetInventory"/> class.
    /// </summary>
    /// <param name="context">The agent associated with the context menu.</param>
    internal MenuTargetInventory(AgentInventoryContext* context)
    {
        this.Context = context;
    }

    /// <summary>
    /// Gets the target item.
    /// </summary>
    public GameInventoryItem? TargetItem
    {
        get
        {
            var target = this.Context->TargetInventorySlot;
            if (target != null)
                return new(*target);
            return null;
        }
    }

    private AgentInventoryContext* Context { get; }
}

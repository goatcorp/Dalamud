using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// .
/// </summary>
internal unsafe partial class NodeBase
{
    /// <summary>
    /// Gets or sets the Text Tooltip for this node.
    /// </summary>
    /// <remarks>
    /// If tooltip is set after the node is attached, a collision update will be required.
    /// </remarks>
    public virtual ReadOnlySeString TextTooltip
    {
        get;
        set
        {
            field = value;
            if (!value.IsEmpty)
            {
                this.TryRegisterTooltipEvents();
                this.tooltipType |= AtkTooltipType.Text;
            }
            else
            {
                this.tooltipType &= ~AtkTooltipType.Text;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Action Tooltip for this node. Uses ActionId.
    /// </summary>
    /// <remarks>
    /// If tooltip is set after the node is attached, a collision update will be required.
    /// </remarks>
    public virtual uint ActionTooltip
    {
        get;
        set
        {
            field = value;
            if (value is not 0)
            {
                this.TryRegisterTooltipEvents();
                this.tooltipType |= AtkTooltipType.Action;
            }
            else
            {
                this.tooltipType &= ~AtkTooltipType.Action;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Action Tooltip for this node. Uses ItemId.
    /// </summary>
    /// <remarks>
    /// If tooltip is set after the node is attached, a collision update will be required.
    /// </remarks>
    public virtual uint ItemTooltip
    {
        get;
        set
        {
            field = value;
            if (value is not 0)
            {
                this.TryRegisterTooltipEvents();
                this.tooltipType |= AtkTooltipType.Item;
            }
            else
            {
                this.tooltipType &= ~AtkTooltipType.Item;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Action Tooltip for this node. Takes a InventoryType and a slot index to represent the item in that slot.
    /// </summary>
    /// <remarks>
    /// If tooltip is set after the node is attached, a collision update will be required.
    /// </remarks>
    public virtual InventoryItemTooltip? InventoryItemTooltip
    {
        get;
        set
        {
            field = value;
            if (value is not null)
            {
                this.TryRegisterTooltipEvents();
                this.tooltipType |= AtkTooltipType.Item;
            }
            else
            {
                this.tooltipType &= ~AtkTooltipType.Item;
            }
        }
    }

    /// <summary>
    /// Property that indicates if a tooltip is already registered.
    /// </summary>
    /// <remarks>
    /// Used by inherited nodes if they want to override the tooltip behavior.
    /// </remarks>
    protected bool TooltipRegistered { get; set; }

    /// <summary>
    /// Triggers this nodes tooltip to show, prioritizing Text -> Action -> Item tooltips in that order, only one tooltip will show.
    /// </summary>
    public virtual void ShowTooltip()
    {
        if (ParentAddon is null) return; // Shouldn't be possible
        if (this.tooltipType is AtkTooltipType.None) return;

        using var stringBuilder = new RentedSeStringBuilder();
        using var stringBuffer = new RentedAtkValues(1);
        if (!this.TextTooltip.IsEmpty)
        {
            stringBuffer[0].SetManagedString(stringBuilder.Builder.Append(this.TextTooltip).GetViewAsSpan());
        }

        var tooltipArgs = new AtkTooltipManager.AtkTooltipArgs();

        if (this.tooltipType.HasFlag(AtkTooltipType.Text))
        {
            tooltipArgs.TextArgs.AtkArrayType = 0;
            tooltipArgs.TextArgs.Text = stringBuffer[0].String;
        }

        if (this.tooltipType.HasFlag(AtkTooltipType.Action))
        {
            tooltipArgs.ActionArgs.Flags = 1;
            tooltipArgs.ActionArgs.Kind = DetailKind.Action;
            tooltipArgs.ActionArgs.Id = (int)this.ActionTooltip;
        }

        if (this.tooltipType.HasFlag(AtkTooltipType.Item) && this.InventoryItemTooltip is { } inventoryTooltip)
        {
            tooltipArgs.ItemArgs.Kind = DetailKind.InventoryItem;
            tooltipArgs.ItemArgs.InventoryType = inventoryTooltip.Inventory;
            tooltipArgs.ItemArgs.Slot = inventoryTooltip.Slot;
            tooltipArgs.ItemArgs.BuyQuantity = -1;
            tooltipArgs.ItemArgs.Flag1 = 0;
        }
        else if (this.tooltipType.HasFlag(AtkTooltipType.Item) && this.InventoryItemTooltip is null)
        {
            tooltipArgs.ItemArgs.Kind = DetailKind.Item;
            tooltipArgs.ItemArgs.ItemId = (int)this.ItemTooltip;
            tooltipArgs.ItemArgs.BuyQuantity = -1;
            tooltipArgs.ItemArgs.Flag1 = 0;
        }

        AtkStage.Instance()->TooltipManager.ShowTooltip(this.tooltipType, ParentAddon->Id, this, &tooltipArgs);
    }

    /// <summary>
    /// Shows the specified text as a tooltip for this node.
    /// </summary>
    public void ShowTextTooltip(ReadOnlySeString tooltip)
    {
        if (tooltip.IsEmpty) return;

        AtkStage.Instance()->TooltipManager.ShowTooltip(ParentAddon->Id, null, tooltip);
    }

    /// <summary>
    /// Hides any tooltip active for the addon this node is attached to.
    /// </summary>
    /// <remarks>
    /// You could potentially close a tooltip the game itself is showing you via this method, exercise caution.
    /// </remarks>
    public void HideTooltip()
    {
        if (ParentAddon is null) return;

        AtkStage.Instance()->TooltipManager.HideTooltip(ParentAddon->Id);
    }

    private void TryRegisterTooltipEvents()
    {
        if (this.tooltipEventsRegistered) return;

        AddEvent(AtkEventType.MouseOver, this.ShowTooltip);
        AddEvent(AtkEventType.MouseOut, this.HideTooltip);
        OnVisibilityToggled += this.ToggleCollisionFlag;
        this.ToggleCollisionFlag(IsVisible);

        this.tooltipEventsRegistered = true;
    }

    private void UnregisterTooltipEvents()
    {
        if (this.tooltipEventsRegistered)
        {
            RemoveEvent(AtkEventType.MouseOver, this.ShowTooltip);
            RemoveEvent(AtkEventType.MouseOut, this.HideTooltip);
            OnVisibilityToggled -= this.ToggleCollisionFlag;
            this.tooltipEventsRegistered = false;
        }
    }

    private void ToggleCollisionFlag(bool isVisible)
    {
        if (this is ComponentNode.ComponentNode) return;

        if (isVisible)
        {
            AddNodeFlags(NodeFlags.HasCollision);
        }
        else
        {
            RemoveNodeFlags(NodeFlags.HasCollision);
        }
    }

    private AtkTooltipType tooltipType = AtkTooltipType.None;
    private bool tooltipEventsRegistered;
}

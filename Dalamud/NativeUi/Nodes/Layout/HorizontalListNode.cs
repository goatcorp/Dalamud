using System.Linq;

using Dalamud.NativeUi.Enums;

namespace Dalamud.NativeUi.Nodes.Layout;

/// <summary>
/// A <see cref="LayoutListNode"/> that represents a horizontally growing list of nodes.
/// Can be anchored on left side or right side.
/// </summary>
internal class HorizontalListNode : LayoutListNode
{
    /// <summary>
    /// Gets or sets the alignment used when calculating layout.
    /// </summary>
    /// <remarks>
    /// Setting triggers layout recalculation.
    /// </remarks>
    public HorizontalListAnchor Alignment
    {
        get;
        set
        {
            field = value;
            this.RecalculateLayout();
        }
    }

    /// <inheritdoc/>
    public override float Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            this.RecalculateLayout();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this node adjusts contained nodes heights to match this nodes height.
    /// </summary>
    public bool FitHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node resizes the horizontal list node to fit the height of all contents.
    /// </summary>
    public bool FitToContentHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node resizes the horizontal list node to fit the width of all contents.
    /// </summary>
    public bool FitToContentWidth { get; set; }

    /// <summary>
    /// Gets the amount of space remaining in this node.
    /// </summary>
    public float AreaRemaining
        => this.Width - this.NodeList.Sum(node => node.Width + this.ItemSpacing) - this.ItemSpacing;

    /// <summary>
    /// Gets or sets the up nav index.
    /// </summary>
    public int NavUp { get; set; }

    /// <summary>
    /// Gets or sets the down nav index.
    /// </summary>
    public int NavDown { get; set; }

    /// <inheritdoc />
    protected override void OnRecalculateLayout()
    {
        if (this.FitToContentWidth)
        {
            base.Width = this.NodeList.Sum(node => node.IsVisible ? node.Width + this.ItemSpacing : 0.0f) + this.FirstItemSpacing - this.ItemSpacing;
        }

        var startX = this.Alignment switch {
            HorizontalListAnchor.Left => 0.0f + this.FirstItemSpacing,
            HorizontalListAnchor.Right => this.Width - this.FirstItemSpacing,
            _ => 0.0f,
        };

        foreach (var node in this.NodeList)
        {
            if (!node.IsVisible) continue;

            if (this.Alignment is HorizontalListAnchor.Right)
            {
                startX -= node.Width + this.ItemSpacing;
            }

            node.X = startX;
            this.AdjustNode(node);

            if (this.Alignment is HorizontalListAnchor.Left)
            {
                startX += node.Width + this.ItemSpacing;
            }

            if (this.FitHeight)
            {
                node.Height = this.Height;
            }
        }

        if (this.FitToContentHeight)
        {
            this.Height = this.NodeList.Max(node => node.Height);
        }
    }
}

using System.Linq;

using Dalamud.NativeUi.Enums;

namespace Dalamud.NativeUi.Nodes.Layout;

/// <summary>
/// A <see cref="LayoutListNode"/> that represents a vertical list of elements.
/// </summary>
internal class VerticalListNode : LayoutListNode
{
    /// <summary>
    /// Gets or sets displays items starting from either the bottom or the top of the list.
    /// </summary>
    public VerticalListAnchor Anchor
    {
        get;
        set
        {
            field = value;
            this.RecalculateLayout();
        }
    }

    /// <summary>
    /// Gets or sets displays items either left aligned or right aligned.
    /// </summary>
    public VerticalListAlignment Alignment
    {
        get;
        set
        {
            field = value;
            this.RecalculateLayout();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether resizes this layout node to fit the height of the contained nodes.
    /// </summary>
    public bool FitContents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether resizes nodes that are inserted to be the same width as the content area.
    /// </summary>
    public bool FitWidth { get; set; }

    /// <inheritdoc />
    protected override void OnRecalculateLayout()
    {
        var startY = this.Anchor switch {
            VerticalListAnchor.Top => 0.0f + this.FirstItemSpacing,
            VerticalListAnchor.Bottom => this.Height,
            _ => 0.0f,
        };

        foreach (var node in this.NodeList)
        {
            if (!node.IsVisible) continue;

            if (this.Anchor is VerticalListAnchor.Bottom)
            {
                startY -= node.Height + this.ItemSpacing;
            }

            node.Y = startY;

            if (this.FitWidth)
            {
                node.Width = this.Width;
            }
            else
            {
                switch (this.Alignment)
                {
                    case VerticalListAlignment.Right:
                        node.X = this.Width - node.Width;
                        break;

                    case VerticalListAlignment.Left:
                        node.X = 0.0f;
                        break;
                }
            }

            this.AdjustNode(node);

            if (this.Anchor is VerticalListAnchor.Top)
            {
                startY += node.Height + this.ItemSpacing;
            }
        }

        if (this.FitContents)
        {
            this.Height = (this.NodeList.Sum(node => node.IsVisible ? node.Height + this.ItemSpacing : 0.0f) + this.FirstItemSpacing) - this.ItemSpacing;
        }
    }
}

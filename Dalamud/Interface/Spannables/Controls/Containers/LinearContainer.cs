using System.Collections.Generic;

using Dalamud.Interface.Spannables.Controls.EventHandlers;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A container that lays out controls in a single line.</summary>
public class LinearContainer : ContainerControl
{
    private readonly List<ChildLayout> childLayouts = new();
    
    /// <summary>Direction of laying out the controls.</summary>
    public enum LinearDirection
    {
        /// <summary>Lay out controls, left to right.</summary>
        LeftToRight,

        /// <summary>Lay out controls, right to left.</summary>
        RightToLeft,

        /// <summary>Lay out controls, top to bottom.</summary>
        TopToBottom,

        /// <summary>Lay out controls, bottom to top.</summary>
        BottomToTop,
    }

    /// <summary>Gets or sets the direction of laying out the child controls.</summary>
    public LinearDirection Direction { get; set; } = LinearDirection.LeftToRight;

    /// <summary>Gets or sets the content bias, which decides where the child controls are, in case their sizes do not
    /// fill the whole control.</summary>
    /// <remarks><c>0</c> will make them stick to start, and <c>1</c> will make them stick to end, specified from
    /// <see cref="Direction"/>.</remarks>
    public float ContentBias { get; set; }

    /// <summary>Gets or sets a weight value that <see cref="ChildLayout.Weight"/> will treat this property as a
    /// denominator. <c>0</c> if unset, and sum of all <see cref="ChildLayout.Weight"/> should be used in place.
    /// </summary>
    public float TotalWeight { get; set; } = 1f;

    /// <summary>Gets the child layout that may be modified.</summary>
    /// <param name="index">Index of the child.</param>
    /// <returns>The child layout that may be modified.</returns>
    public ChildLayout GetChildLayout(int index) => this.childLayouts[index];

    /// <inheritdoc/>
    protected override void OnChildAdd(ControlChildArgs args)
    {
        this.childLayouts.Insert(args.Index, new());
        base.OnChildAdd(args);
    }

    /// <inheritdoc/>
    protected override void OnChildRemove(ControlChildArgs args)
    {
        this.childLayouts.RemoveAt(args.Index);
        base.OnChildRemove(args);
    }

    /// <summary>Declares a child layout.</summary>
    public class ChildLayout
    {
        /// <summary>Gets or sets the child alignment.</summary>
        /// <remarks><c>0</c> will make them stick to start, and <c>1</c> will make them stick to end, specified from
        /// <see cref="Direction"/>.</remarks>
        public float Alignment { get; set; }

        /// <summary>Gets or sets the optional weight.</summary>
        /// <remarks>A value of <c>0</c> or less indicates that this child has no weight parameter.</remarks>
        public float Weight { get; set; }
    }
}

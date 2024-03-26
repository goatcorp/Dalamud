using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A container that lays out controls in a single line.</summary>
public class LinearContainer : ContainerControl
{
    private readonly List<ChildLayout> childLayouts = new();
    private readonly List<Vector2> childOffsets = new();
    private LinearDirection direction = LinearDirection.LeftToRight;
    private float contentBias;
    private float totalWeight = 1f;

    /// <summary>Occurs when the property <see cref="Direction"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, LinearDirection>? DirectionChange;

    /// <summary>Occurs when the property <see cref="ContentBias"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, float>? ContentBiasChange;

    /// <summary>Occurs when the property <see cref="TotalWeight"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, float>? TotalWeightChange;

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
    public LinearDirection Direction
    {
        get => this.direction;
        set => this.HandlePropertyChange(
            nameof(this.Direction),
            ref this.direction,
            value,
            this.OnDirectionChange);
    }

    /// <summary>Gets or sets the content bias, which decides where the child controls are, in case their sizes do not
    /// fill the whole control.</summary>
    /// <remarks><c>0</c> will make them stick to start, and <c>1</c> will make them stick to end, specified from
    /// <see cref="Direction"/>.</remarks>
    public float ContentBias
    {
        get => this.contentBias;
        set => this.HandlePropertyChange(
            nameof(this.ContentBias),
            ref this.contentBias,
            value,
            this.OnContentBiasChange);
    }

    /// <summary>Gets or sets a weight value that <see cref="ChildLayout.Weight"/> will treat this property as a
    /// denominator. If no value is specified (<c>0</c> or less), then the sum of all <see cref="ChildLayout.Weight"/>
    /// will be used in place.</summary>
    public float TotalWeight
    {
        get => this.totalWeight;
        set => this.HandlePropertyChange(
            nameof(this.TotalWeight),
            ref this.totalWeight,
            value,
            this.OnTotalWeightChange);
    }

    /// <summary>Gets the child layout.</summary>
    /// <param name="index">Index of the child.</param>
    /// <returns>The child layout.</returns>
    public ChildLayout GetChildLayout(int index) => this.childLayouts[index];

    /// <summary>Sets the child layout.</summary>
    /// <param name="index">Index of the child.</param>
    /// <param name="layout">The new child layout.</param>
    public void SetChildLayout(int index, ChildLayout layout) =>
        this.childLayouts[index] = layout ?? throw new NullReferenceException();

    /// <summary>Sets the child layout.</summary>
    /// <param name="index">Index of the child.</param>
    /// <param name="layout">The new child layout.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    public LinearContainer WithChildLayout(int index, ChildLayout layout)
    {
        this.SetChildLayout(index, layout);
        return this;
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureChildren(
        SpannableMeasureArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        Debug.Assert(
            children.Length == this.childLayouts.Count,
            $"{nameof(children)} and {nameof(this.childLayouts)} got out of synchronization.");
        Debug.Assert(
            renderPasses.Length == this.childLayouts.Count,
            $"{nameof(renderPasses)} and {nameof(this.childLayouts)} got out of synchronization.");

        var weightSum = this.totalWeight;
        if (weightSum <= 0)
        {
            weightSum = 0;
            foreach (var cl in this.childLayouts)
                weightSum += cl.Weight;
            if (weightSum <= 0)
                weightSum = 1f;
        }

        var isHorizontal = this.direction is LinearDirection.LeftToRight or LinearDirection.RightToLeft;
        var isVertical = this.direction is LinearDirection.TopToBottom or LinearDirection.BottomToTop;
        var childOffset = Vector2.Zero;
        var contentBox = RectVector4.InvertedExtrema;
        var childSizeSum = Vector2.Zero;

        var requiredMinSizeForWeight = Vector2.Zero;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            var layout = this.childLayouts[i];
            var innerId = this.InnerIdAvailableSlot + i;

            var useHorizontalWeight = isHorizontal && layout.Weight > 0f && args.MaxSize.X < float.PositiveInfinity;
            var useVerticalWeight = isVertical && layout.Weight > 0f && args.MaxSize.Y < float.PositiveInfinity;

            Vector2 maxChildSize;
            if (useHorizontalWeight)
                maxChildSize = new(args.MaxSize.X * (layout.Weight / weightSum), float.MaxValue);
            else if (useVerticalWeight)
                maxChildSize = new(float.MaxValue, args.MaxSize.Y * (layout.Weight / weightSum));
            else if (isHorizontal)
                maxChildSize = args.MaxSize with { X = float.PositiveInfinity };
            else if (isVertical)
                maxChildSize = args.MaxSize with { Y = float.PositiveInfinity };
            else
                maxChildSize = new(float.PositiveInfinity);

            var minChildSize = new Vector2(
                useHorizontalWeight ? maxChildSize.X : 0,
                useVerticalWeight ? maxChildSize.Y : 0);

            args.NotifyChild(child, pass, innerId, minChildSize, maxChildSize);
            childSizeSum += pass.Boundary.RightBottom;

            if (isHorizontal && layout.Weight > 0 && weightSum > 0)
            {
                requiredMinSizeForWeight.X = Math.Max(
                    requiredMinSizeForWeight.X,
                    (pass.Boundary.Right * weightSum) / layout.Weight);
            }

            switch (this.direction)
            {
                case LinearDirection.RightToLeft:
                    childOffset.X -= pass.Boundary.Right;
                    break;
                case LinearDirection.BottomToTop:
                    childOffset.Y -= pass.Boundary.Bottom;
                    break;
            }

            this.childOffsets[i] = childOffset;
            contentBox = RectVector4.Union(contentBox, RectVector4.Translate(pass.Boundary, childOffset));

            switch (this.direction)
            {
                case LinearDirection.LeftToRight:
                    childOffset.X += pass.Boundary.Right;
                    break;
                case LinearDirection.TopToBottom:
                    childOffset.Y += pass.Boundary.Bottom;
                    break;
            }
        }

        if (!contentBox.IsValid)
            contentBox = base.MeasureChildren(args, children, renderPasses);

        var rb = contentBox.Size;
        if (args.SuggestedSize.X < float.PositiveInfinity)
            rb.X = Math.Max(rb.X, args.SuggestedSize.X);
        if (args.SuggestedSize.Y < float.PositiveInfinity)
            rb.Y = Math.Max(rb.Y, args.SuggestedSize.Y);
        if (args.MinSize.X < float.PositiveInfinity && rb.X < args.MinSize.X)
            rb.X = args.MinSize.X;
        if (args.MinSize.Y < float.PositiveInfinity && rb.Y < args.MinSize.Y)
            rb.Y = args.MinSize.Y;
        rb = Vector2.Max(rb, requiredMinSizeForWeight);

        return new(Vector2.Zero, Vector2.Min(rb, args.MaxSize));
    }

    /// <inheritdoc/>
    protected override void CommitMeasurementChildren(
        ControlCommitMeasurementEventArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        var childSizeSum = Vector2.Zero;
        var childSizeMax = Vector2.Zero;
        foreach (var x in renderPasses)
        {
            childSizeSum += x.Boundary.RightBottom;
            childSizeMax = Vector2.Max(childSizeMax, x.Boundary.RightBottom);
        }

        var myBoxSize = this.direction switch
        {
            LinearDirection.LeftToRight or LinearDirection.RightToLeft => new(childSizeSum.X, childSizeMax.Y),
            LinearDirection.TopToBottom or LinearDirection.BottomToTop => new(childSizeMax.X, childSizeSum.Y),
            _ => Vector2.Zero,
        };
        var myFullBoxSize = Vector2.Max(myBoxSize, this.MeasuredContentBox.Size);

        var baseOffset = this.direction switch
        {
            LinearDirection.LeftToRight or LinearDirection.TopToBottom => this.MeasuredContentBox.LeftTop,
            LinearDirection.RightToLeft => new(
                Math.Max(this.MeasuredContentBox.Right, childSizeSum.X),
                this.MeasuredContentBox.Top),
            LinearDirection.BottomToTop => new(
                this.MeasuredContentBox.Left,
                Math.Max(this.MeasuredContentBox.Bottom, childSizeSum.Y)),
            _ => Vector2.Zero,
        };

        var bias = this.direction switch
        {
            LinearDirection.LeftToRight => new(this.contentBias, 0),
            LinearDirection.RightToLeft => new(-this.contentBias, 0),
            LinearDirection.TopToBottom => new(0, this.contentBias),
            LinearDirection.BottomToTop => new(0, -this.contentBias),
            _ => Vector2.Zero,
        };

        baseOffset += Vector2.Max(Vector2.Zero, myFullBoxSize - myBoxSize) * bias;
        baseOffset -= this.Scroll;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            var layout = this.childLayouts[i];
            var offset = this.childOffsets[i];
            switch (this.direction)
            {
                case LinearDirection.LeftToRight or LinearDirection.RightToLeft:
                    offset.Y += (myFullBoxSize.Y - pass.Boundary.Height) * layout.Alignment;
                    break;
                case LinearDirection.TopToBottom or LinearDirection.BottomToTop:
                    offset.X += (myFullBoxSize.X - pass.Boundary.Width) * layout.Alignment;
                    break;
            }

            var childFinalLocalOffset = baseOffset + offset;
            args.SpannableArgs.NotifyChild(child, pass, childFinalLocalOffset.Round(), Matrix4x4.Identity);
        }
    }

    /// <inheritdoc/>
    protected override void OnChildAdd(ControlChildEventArgs args)
    {
        this.childLayouts.Insert(args.Index, new());
        this.childOffsets.Insert(args.Index, default);
        base.OnChildAdd(args);
    }

    /// <inheritdoc/>
    protected override void OnChildRemove(ControlChildEventArgs args)
    {
        this.childLayouts.RemoveAt(args.Index);
        this.childOffsets.RemoveAt(args.Index);
        base.OnChildRemove(args);
    }

    /// <summary>Raises the <see cref="DirectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnDirectionChange(PropertyChangeEventArgs<ControlSpannable, LinearDirection> args) =>
        this.DirectionChange?.Invoke(args);

    /// <summary>Raises the <see cref="ContentBiasChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnContentBiasChange(PropertyChangeEventArgs<ControlSpannable, float> args) =>
        this.ContentBiasChange?.Invoke(args);

    /// <summary>Raises the <see cref="TotalWeightChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTotalWeightChange(PropertyChangeEventArgs<ControlSpannable, float> args) =>
        this.TotalWeightChange?.Invoke(args);

    /// <summary>Declares a child layout.</summary>
    public record ChildLayout
    {
        /// <summary>Gets the child alignment.</summary>
        /// <remarks><c>0</c> will make them stick to start, and <c>1</c> will make them stick to end, specified from
        /// <see cref="Direction"/>.</remarks>
        public float Alignment { get; init; }

        /// <summary>Gets the optional weight.</summary>
        /// <remarks>
        /// <para>A value of <c>0</c> or less indicates that this child has no weight parameter.</para>
        /// <para>This does nothing if the <see cref="LinearContainer"/> is not given finite bounds.</para>
        /// </remarks>
        public float Weight { get; init; }
    }
}

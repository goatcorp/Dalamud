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
        ReadOnlySpan<ISpannableRenderPass> renderPasses,
        in RectVector4 box)
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

        var childOffset = Vector2.Zero;
        var selfBoundary = RectVector4.InvertedExtrema;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            var layout = this.childLayouts[i];
            var innerId = this.InnerIdAvailableSlot + i;

            var childBox = this.direction switch
            {
                LinearDirection.LeftToRight or LinearDirection.RightToLeft
                    when layout.Weight > 0f && box.Right < float.PositiveInfinity =>
                    box with { Left = 0, Right = box.Width * (layout.Weight / weightSum) },
                LinearDirection.TopToBottom or LinearDirection.BottomToTop
                    when layout.Weight > 0f && box.Bottom < float.PositiveInfinity =>
                    box with { Top = 0, Bottom = box.Height * (layout.Weight / weightSum) },
                LinearDirection.LeftToRight or LinearDirection.RightToLeft =>
                    box with { Left = 0, Right = float.PositiveInfinity },
                LinearDirection.TopToBottom or LinearDirection.BottomToTop =>
                    box with { Top = 0, Bottom = float.PositiveInfinity },
                _ => box,
            };

            if (childBox.Right < float.PositiveInfinity)
                childBox.Right = MathF.Round(childBox.Right - childBox.Left);
            childBox.Left = 0;
            if (childBox.Bottom < float.PositiveInfinity)
                childBox.Bottom = MathF.Round(childBox.Bottom - childBox.Top);
            childBox.Top = 0;
            args.NotifyChild(child, pass, innerId, childBox.RightBottom);

            var occupyWidth = MathF.Round(pass.Boundary.Right);
            var occupyHeight = MathF.Round(pass.Boundary.Bottom);
            switch (this.direction)
            {
                case LinearDirection.RightToLeft:
                    childOffset.X -= occupyWidth;
                    break;
                case LinearDirection.BottomToTop:
                    childOffset.Y -= occupyHeight;
                    break;
            }

            this.childOffsets[i] = childOffset;
            selfBoundary = RectVector4.Union(selfBoundary, RectVector4.Translate(pass.Boundary, childOffset));

            switch (this.direction)
            {
                case LinearDirection.LeftToRight:
                    childOffset.X += occupyWidth;
                    break;
                case LinearDirection.TopToBottom:
                    childOffset.Y += occupyHeight;
                    break;
            }
        }

        selfBoundary =
            selfBoundary.IsValid
                ? RectVector4.Translate(selfBoundary, box.LeftTop)
                : base.MeasureChildren(args, children, renderPasses, box);

        var maxWidth = box.Width < float.PositiveInfinity ? box.Width : selfBoundary.Width;
        var maxHeight = box.Height < float.PositiveInfinity ? box.Height : selfBoundary.Height;

        if (selfBoundary.Width < maxWidth)
        {
            var bias = this.direction switch {
                LinearDirection.LeftToRight => this.contentBias,
                LinearDirection.RightToLeft => 1 - this.contentBias,
                LinearDirection.TopToBottom => 0,
                LinearDirection.BottomToTop => 0,
                _ => 0,
            };
            selfBoundary = selfBoundary with
            {
                Left = (maxWidth - selfBoundary.Width) * bias,
                Right = ((maxWidth - selfBoundary.Width) * bias) + selfBoundary.Width,
            };
        }
        else
        {
            selfBoundary = selfBoundary with
            {
                Left = 0,
                Right = selfBoundary.Width,
            };
        }

        if (selfBoundary.Height < maxHeight)
        {
            var bias = this.direction switch {
                LinearDirection.LeftToRight => 0,
                LinearDirection.RightToLeft => 0,
                LinearDirection.TopToBottom => this.contentBias,
                LinearDirection.BottomToTop => 1 - this.contentBias,
                _ => 0,
            };
            selfBoundary = selfBoundary with
            {
                Top = (maxHeight - selfBoundary.Height) * bias,
                Bottom = ((maxHeight - selfBoundary.Height) * bias) + selfBoundary.Height,
            };
        }
        else
        {
            selfBoundary = selfBoundary with
            {
                Top = 0,
                Bottom = selfBoundary.Height,
            };
        }

        selfBoundary = RectVector4.Translate(selfBoundary, box.LeftTop);
        return new(
            MathF.Round(selfBoundary.Left),
            MathF.Round(selfBoundary.Top),
            MathF.Round(selfBoundary.Right),
            MathF.Round(selfBoundary.Bottom));
    }

    /// <inheritdoc/>
    protected override void CommitMeasurementChildren(
        ControlCommitMeasurementEventArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        var baseOffset = this.direction switch
        {
            LinearDirection.LeftToRight => this.MeasuredContentBox.LeftTop,
            LinearDirection.RightToLeft => this.MeasuredContentBox.RightTop,
            LinearDirection.TopToBottom => this.MeasuredContentBox.LeftTop,
            LinearDirection.BottomToTop => this.MeasuredContentBox.LeftBottom,
            _ => Vector2.Zero,
        };
        baseOffset += this.Scroll;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            var layout = this.childLayouts[i];
            var offset = this.childOffsets[i];
            switch (this.direction)
            {
                case LinearDirection.LeftToRight or LinearDirection.RightToLeft:
                    offset.Y += (this.MeasuredContentBox.Height - pass.Boundary.Height) * layout.Alignment;
                    break;
                case LinearDirection.TopToBottom or LinearDirection.BottomToTop:
                    offset.X += (this.MeasuredContentBox.Width - pass.Boundary.Width) * layout.Alignment;
                    break;
            }

            var childFinalLocalOffset = baseOffset + offset;
            childFinalLocalOffset = new(MathF.Round(childFinalLocalOffset.X), MathF.Round(childFinalLocalOffset.Y));
            args.SpannableArgs.NotifyChild(child, pass, childFinalLocalOffset, Matrix4x4.Identity);
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

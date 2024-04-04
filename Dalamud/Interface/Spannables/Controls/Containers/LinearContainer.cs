using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

#pragma warning disable SA1010

/// <summary>A container that lays out controls in a single line.</summary>
public class LinearContainer : ContainerControl
{
    private readonly List<ChildLayout> childLayouts = [];
    private readonly List<Vector2> childOffsets = [];
    private LinearDirection direction = LinearDirection.LeftToRight;
    private float contentBias;
    private float totalWeight = 1f;

    /// <summary>Initializes a new instance of the <see cref="LinearContainer"/> class.</summary>
    public LinearContainer()
    {
        this.UseDefaultScrollHandling = true;
        this.Children.CollectionChanged += this.ChildrenOnCollectionChanged;
    }

    /// <summary>Occurs when the property <see cref="Direction"/> is changing.</summary>
    public event PropertyChangeEventHandler<LinearDirection>? DirectionChange;

    /// <summary>Occurs when the property <see cref="ContentBias"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ContentBiasChange;

    /// <summary>Occurs when the property <see cref="TotalWeight"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? TotalWeightChange;

    /// <summary>Gets or sets the direction of laying out the child controls.</summary>
    public LinearDirection Direction
    {
        get => this.direction;
        set => this.HandlePropertyChange(
            nameof(this.Direction),
            ref this.direction,
            value,
            this.direction == value,
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
            this.contentBias - value == 0f,
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
            this.totalWeight - value == 0f,
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
    protected override RectVector4 MeasureChildren(Vector2 suggestedSize)
    {
        Debug.Assert(
            this.Children.Count == this.childLayouts.Count,
            $"{nameof(this.Children)} and {nameof(this.childLayouts)} got out of synchronization.");

        var weightSum = this.totalWeight;
        if (weightSum <= 0)
        {
            weightSum = 0;
            foreach (var cl in this.childLayouts)
                weightSum += cl.Weight;
            if (weightSum <= 0)
                weightSum = 1f;
        }

        var contentBox = RectVector4.InvertedExtrema;
        var requiredMinSizeForWeight = Vector2.Zero;
        for (var j = 0; j < 2; j++)
        {
            // Handle MatchParent inside WrapContent containers, by doing a two-pass, and using the measured
            // dimension for the second pass.
            // TODO: Propagate MatchParent inside WrapContent containers information back to measuring parent,
            // so that the second pass can be skipped when unnecessary.
            if (j == 1)
            {
                if (this.direction.IsVertical())
                    suggestedSize.X = contentBox.Right;
                else
                    suggestedSize.Y = contentBox.Bottom;

                contentBox = RectVector4.InvertedExtrema;
                requiredMinSizeForWeight = Vector2.Zero;
            }

            var childOffset = Vector2.Zero;
            var childSizeSum = Vector2.Zero;

            for (var i = 0; i < this.Children.Count; i++)
            {
                var cm = this.Children[i];
                var layout = this.childLayouts[i];

                var useHorizontalWeight =
                    this.direction.IsHorizontal() && layout.Weight > 0f && suggestedSize.X < float.PositiveInfinity;
                var useVerticalWeight =
                    this.direction.IsVertical() && layout.Weight > 0f && suggestedSize.Y < float.PositiveInfinity;

                Vector2 maxChildSize;
                if (useHorizontalWeight)
                    maxChildSize = new(suggestedSize.X * (layout.Weight / weightSum), float.PositiveInfinity);
                else if (useVerticalWeight)
                    maxChildSize = new(float.PositiveInfinity, suggestedSize.Y * (layout.Weight / weightSum));
                else if (this.direction.IsHorizontal())
                    maxChildSize = suggestedSize with { X = float.PositiveInfinity };
                else if (this.direction.IsVertical())
                    maxChildSize = suggestedSize with { Y = float.PositiveInfinity };
                else
                    maxChildSize = new(float.PositiveInfinity);

                cm.RenderPassMeasure(maxChildSize);

                var b = Vector2.Max(cm.Boundary.RightBottom, cm.Boundary.Size);
                childSizeSum += b;

                if (this.direction.IsHorizontal() && layout.Weight > 0 && weightSum > 0)
                {
                    requiredMinSizeForWeight.X = Math.Max(
                        requiredMinSizeForWeight.X,
                        (b.X * weightSum) / layout.Weight);
                }

                switch (this.direction)
                {
                    case LinearDirection.RightToLeft:
                        childOffset.X -= b.X;
                        break;
                    case LinearDirection.BottomToTop:
                        childOffset.Y -= b.Y;
                        break;
                }

                this.childOffsets[i] = childOffset;
                contentBox = RectVector4.Union(contentBox, RectVector4.Translate(cm.Boundary, childOffset));

                switch (this.direction)
                {
                    case LinearDirection.LeftToRight:
                        childOffset.X += b.X;
                        break;
                    case LinearDirection.TopToBottom:
                        childOffset.Y += b.Y;
                        break;
                }
            }
        }

        if (!contentBox.IsValid)
            contentBox = base.MeasureChildren(suggestedSize);

        var rb = contentBox.Size;
        if (suggestedSize.X < float.PositiveInfinity)
            rb.X = Math.Max(rb.X, suggestedSize.X);
        if (suggestedSize.Y < float.PositiveInfinity)
            rb.Y = Math.Max(rb.Y, suggestedSize.Y);
        rb = Vector2.Max(rb, requiredMinSizeForWeight);

        return new(Vector2.Zero, rb);
    }

    /// <inheritdoc/>
    protected override void PlaceChildren(SpannableEventArgs args)
    {
        var childSizeSum = Vector2.Zero;
        var childSizeMax = Vector2.Zero;
        foreach (var x in this.Children)
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

        for (var i = 0; i < this.Children.Count; i++)
        {
            var cm = this.Children[i];
            var layout = this.childLayouts[i];
            var offset = this.childOffsets[i];
            switch (this.direction)
            {
                case LinearDirection.LeftToRight or LinearDirection.RightToLeft:
                    offset.Y += (myFullBoxSize.Y - cm.Boundary.Height) * layout.Alignment;
                    break;
                case LinearDirection.TopToBottom or LinearDirection.BottomToTop:
                    offset.X += (myFullBoxSize.X - cm.Boundary.Width) * layout.Alignment;
                    break;
            }

            var childFinalLocalOffset = baseOffset + offset;
            cm.RenderPassPlace(
                Matrix4x4.CreateTranslation(new(childFinalLocalOffset.Round(1f / this.EffectiveRenderScale), 0)),
                this.FullTransformation);
        }
    }

    /// <summary>Raises the <see cref="DirectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDirectionChange(PropertyChangeEventArgs<LinearDirection> args) =>
        this.DirectionChange?.Invoke(args);

    /// <summary>Raises the <see cref="ContentBiasChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnContentBiasChange(PropertyChangeEventArgs<float> args) =>
        this.ContentBiasChange?.Invoke(args);

    /// <summary>Raises the <see cref="TotalWeightChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTotalWeightChange(PropertyChangeEventArgs<float> args) =>
        this.TotalWeightChange?.Invoke(args);

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } items:
                this.childLayouts.InsertRange(
                    e.NewStartingIndex,
                    Enumerable.Range(0, items.Count).Select(_ => new ChildLayout()));
                this.childOffsets.InsertRange(
                    e.NewStartingIndex,
                    Enumerable.Range(0, items.Count).Select(_ => Vector2.Zero));
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { } items:
                this.childLayouts.RemoveRange(e.OldStartingIndex, items.Count);
                this.childOffsets.RemoveRange(e.OldStartingIndex, items.Count);
                break;

            case NotifyCollectionChangedAction.Replace:
                break;

            case NotifyCollectionChangedAction.Move:
                this.childLayouts.InsertRange(
                    e.NewStartingIndex,
                    this.childLayouts.Slice(e.OldStartingIndex, e.OldItems!.Count));
                this.childOffsets.InsertRange(
                    e.NewStartingIndex,
                    this.childOffsets.Slice(e.OldStartingIndex, e.OldItems!.Count));
                break;

            case NotifyCollectionChangedAction.Reset:
                this.childLayouts.Clear();
                this.childOffsets.Clear();
                this.childLayouts.EnsureCapacity(this.Children.Count);
                this.childOffsets.EnsureCapacity(this.Children.Count);
                for (var i = 0; i < this.Children.Count; i++)
                {
                    this.childLayouts.Add(new());
                    this.childOffsets.Add(default);
                }

                break;
        }
    }

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

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Numerics;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls.RecyclerViews;

#pragma warning disable SA1010
#pragma warning disable SA1101

/// <summary>A layout manager that lays out data in a single direction.</summary>
public class LinearLayoutManager : RecyclerViewControl.BaseLayoutManager
{
    private readonly List<VisibleItem> visibleItems = [];

    private long lastHandleInteractionTick = long.MaxValue;

    private Vector2 accumulatedScrollDelta;
    private Vector2 smoothScrollAmount;
    private float nonLimDimScroll;
    private bool needDispatchScrollEvent;

    private bool wasBeginningVisible = true;
    private bool wasEndVisible = true;

    /// <summary>Gets or sets the gravity for the main direction.</summary>
    /// <value><c>0</c> will put the visible items at the beginning. <c>1</c> will put the visible items at the end.
    /// Values outside the range of [0, 1] will be clamped.</value>
    /// <remarks>Does nothing if visible items fill the area of the parent <see cref="RecyclerViewControl"/>.
    /// </remarks>
    public float MainDirectionGravity { get; set; }

    /// <summary>Gets or sets the gravity for the off direction.</summary>
    /// <value><c>0</c> will put the visible items at the beginning. <c>1</c> will put the visible items at the end.
    /// Values outside the range of [0, 1] will be clamped.</value>
    public float OffDirectionGravity { get; set; }

    /// <summary>Gets or sets the offset ratio of the anchored item.</summary>
    /// <value>0 to pin the first visible item at its position when items before or after change, and put the first
    /// item to be displayed ever since the collection has become non-empty at the top;
    /// 1 to pin the last visible item at its position, and the first item to be displayed at the bottom.</value>
    public float AnchorOffsetRatio { get; set; }

    /// <summary>Gets or sets a value indicating whether to stick to terminus if already at the beginning or end of the
    /// scroll range. In case both beginning and end are visible, if <see cref="AnchorOffsetRatio"/> is less than
    /// 0.5, then it will stick to the beginning; otherwise, it will stick to the end.</summary>
    public bool StickToTerminus { get; set; } = false;

    /// <summary>Gets or sets the cell decoration.</summary>
    /// <remarks>Same cell decoration spannable will be used multiple times; use instances of <see cref="ISpannable"/>s
    /// that can exist as children of multiple parents. This rules out all <see cref="ControlSpannable"/>s.</remarks>
    public ISpannable? CellDecoration { get; set; }

    /// <summary>Gets the index of the anchored item.</summary>
    /// <value><c>-1</c> if no item is anchored, because no item is visible.</value>
    /// <remarks>When item is added or removed above or the anchored item, the scroll position of the recycler view
    /// will be adjusted to keep the anchored item at the same position.</remarks>
    public int AnchoredItem { get; private set; }

    /// <summary>Gets the scrolled offset within the anchored item.</summary>
    public float AnchoredItemScrollOffset { get; private set; }

    /// <summary>Gets the scrolled offset ratio within the anchored item.</summary>
    public float AnchoredItemScrollOffsetRatio { get; private set; }

    /// <inheritdoc cref="ControlSpannable.IsAnyAnimationRunning"/>
    public override bool IsAnyAnimationRunning
    {
        get
        {
            if (this.Parent is null)
                return false;

            if (base.IsAnyAnimationRunning || this.Parent.AutoScrollPerSecond != Vector2.Zero)
                return true;

            foreach (var v in this.visibleItems)
            {
                if (v.Animation?.IsRunning is true
                    || v.PreviousAnimation?.IsRunning is true
                    || v.SizeEasing?.IsRunning is true)
                    return true;
            }

            return false;
        }
    }

    private Span<VisibleItem> VisibleItems => CollectionsMarshal.AsSpan(this.visibleItems);

    public void ScrollBy(float delta) => throw new NotImplementedException();

    public void SmoothScrollBy(float delta) => throw new NotImplementedException();

    public void ScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);

    public void ScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();

    public void SmoothScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);

    public void SmoothScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void ScrollBy(Vector2 delta)
    {
        this.accumulatedScrollDelta += delta;

        this.RequestMeasure();
    }

    /// <inheritdoc/>
    public override void SmoothScrollBy(Vector2 delta)
    {
        if (this.ScrollEasing.IsRunning)
        {
            var prev = Math.Clamp((float)this.ScrollEasing.Value, 0f, 1f);
            this.ScrollEasing.Update();
            var now = Math.Clamp((float)this.ScrollEasing.Value, 0f, 1f);
            var prevAnimationDelta = now - prev;
            this.accumulatedScrollDelta += this.smoothScrollAmount * prevAnimationDelta;

            this.smoothScrollAmount = delta + (this.smoothScrollAmount * (1 - now));
            this.ScrollEasing.Restart();
            this.ScrollEasing.Update();
        }
        else
        {
            this.smoothScrollAmount = delta;
            this.ScrollEasing.Restart();
            this.ScrollEasing.Update();
        }

        this.RequestMeasure();
    }

    /// <inheritdoc/>
    public override int FindItemIndexFromSpannable(ISpannable? spannable)
    {
        if (spannable is null)
            return -1;
        foreach (ref var v in this.VisibleItems)
        {
            if (ReferenceEquals(v.Spannable, spannable))
                return v.Index;
        }

        return -1;
    }

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindMeasurementFromItemIndex(int index)
    {
        var vi = this.VisibleItems.BinarySearch(new VisibleItem(index));
        if (vi < 0)
            return null;
        return this.visibleItems[vi].Measurement;
    }

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
    {
        foreach (var vi in this.visibleItems)
        {
            if (vi.Measurement is not { } m)
                continue;
            if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                return m;
        }

        return null;
    }

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindClosestChildMeasurementAt(Vector2 screenOffset)
    {
        var minDist = float.PositiveInfinity;
        ISpannableMeasurement? minItem = null;
        foreach (var vi in this.visibleItems)
        {
            if (vi.Measurement is not { } m)
                continue;

            var localOffset = m.PointToClient(screenOffset);
            var dist = m.Boundary.DistanceSquared(localOffset);
            if (dist <= minDist)
            {
                minDist = dist;
                minItem = m;
            }
        }

        return minItem;
    }

    /// <inheritdoc/>
    public override IEnumerable<(int Index, ISpannableMeasurement Measurement)> EnumerateItemSpannableMeasurements()
    {
        foreach (var vi in this.visibleItems)
        {
            if (vi.Measurement is not null)
                yield return (vi.Index, vi.Measurement);
        }
    }

    /// <inheritdoc/>
    protected override void BeforeParentDetach()
    {
        foreach (ref var x in this.VisibleItems)
            this.ReturnVisibleItem(ref x);
        this.visibleItems.Clear();
        this.lastHandleInteractionTick = long.MaxValue;
        this.nonLimDimScroll = 0;
        this.smoothScrollAmount = Vector2.Zero;
        this.accumulatedScrollDelta = Vector2.Zero;
        base.BeforeParentDetach();
    }

    /// <inheritdoc/>
    protected override void HandleInteractionChildren()
    {
        if (this.Parent is null)
            return;

        foreach (var vi in this.visibleItems)
            vi.Measurement?.HandleInteraction();

        if (this.needDispatchScrollEvent)
        {
            this.RequestNotifyScroll();
            this.needDispatchScrollEvent = false;
        }

        var now = Environment.TickCount64;
        if (this.lastHandleInteractionTick != long.MaxValue)
        {
            var secondsPastLastTick = (now - this.lastHandleInteractionTick) / 1000f;

            // Prevent integration inaccuracies from making it not scroll at all.
            if (secondsPastLastTick >= 0.01f)
            {
                this.accumulatedScrollDelta += this.Parent.AutoScrollPerSecond * secondsPastLastTick;
                this.lastHandleInteractionTick = now;
            }
        }
        else
        {
            this.lastHandleInteractionTick = now;
        }
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureChildren(Vector2 suggestedSize)
    {
        // A copy of previous anchor position, to test whether scroll happened.
        var prevAnchor = (this.AnchoredItem, this.AnchoredItemScrollOffsetRatio, this.nonLimDimScroll);

        // If the collection is not bound, then stop.
        if (this.Parent is null || this.Collection is not { Count: var itemCount })
        {
            this.FirstVisibleItem = this.LastVisibleItem = this.AnchoredItem = -1;
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.wasBeginningVisible = this.wasEndVisible = true;
            this.nonLimDimScroll = 0;
            this.accumulatedScrollDelta = this.smoothScrollAmount = Vector2.Zero;
            return new(Vector2.Zero, suggestedSize);
        }

        // Update animations, and process items with finished remove animations.
        for (var i = this.visibleItems.Count - 1; i >= 0; i--)
        {
            ref var item = ref this.VisibleItems[i];
            item.UpdateAnimation();

            if (item.PreviousAnimation?.IsRunning is true)
                continue;

            if (item.Removed)
            {
                this.ReturnVisibleItem(ref item);
                this.visibleItems.RemoveAt(i);
            }
            else
            {
                this.ReturnPreviousVisibleItem(ref item);
            }
        }

        // If there is nothing being displayed and nothing to display, then stop.
        if (this.visibleItems.Count == 0 && itemCount == 0)
        {
            this.FirstVisibleItem = this.LastVisibleItem = this.AnchoredItem = -1;
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.wasBeginningVisible = this.wasEndVisible = true;
            this.nonLimDimScroll = 0;
            this.accumulatedScrollDelta = this.smoothScrollAmount = Vector2.Zero;
            return new(Vector2.Zero, suggestedSize);
        }

        if (this.AnchoredItem == -1)
            this.AnchoredItem = (int)MathF.Round(this.AnchorOffsetRatio * itemCount);
        this.AnchoredItem = itemCount <= 0 ? -1 : Math.Clamp(this.AnchoredItem, 0, itemCount - 1);

        var limDim = suggestedSize.Y; // VERTICAL
        var nonLimDim = suggestedSize.X;

        if (this.ScrollEasing.IsRunning)
        {
            var prev = Math.Clamp((float)this.ScrollEasing.Value, 0f, 1f);
            this.ScrollEasing.Update();
            var delta = Math.Clamp((float)this.ScrollEasing.Value, 0f, 1f) - prev;
            this.accumulatedScrollDelta += this.smoothScrollAmount * delta;
            if (this.ScrollEasing.IsDone)
                this.ScrollEasing.Stop();
        }

        var scrollOffsetDelta = 0f;
        if (this.accumulatedScrollDelta != Vector2.Zero)
        {
            float scrollScale;
            if (this.Parent.Renderer.TryGetFontData(
                    this.Parent.EffectiveRenderScale,
                    this.Parent.TextStyle,
                    out var fontData))
                scrollScale = fontData.ScaledFontSize;
            else
                scrollScale = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx * this.Parent.Scale;

            int nlines;
            unsafe
            {
                if (!SystemParametersInfoW(SPI.SPI_GETWHEELSCROLLLINES, 0, &nlines, 0))
                    nlines = 3;
            }

            // VERTICAL
            scrollOffsetDelta += this.accumulatedScrollDelta.Y * scrollScale * nlines;
            this.nonLimDimScroll += this.accumulatedScrollDelta.X * scrollScale * nlines;

            this.accumulatedScrollDelta = Vector2.Zero;
        }
        else if (this.StickToTerminus)
        {
            if (this.wasBeginningVisible && (!this.wasEndVisible || this.AnchorOffsetRatio < 0.5f))
            {
                // Stick to beginning
                this.AnchoredItem = 0;
                this.AnchorOffsetRatio = 0;
            }
            else if (this.wasEndVisible && (!this.wasBeginningVisible || this.AnchorOffsetRatio >= 0.5f))
            {
                // Stick to the end
                this.AnchoredItem = itemCount - 1;
                this.AnchorOffsetRatio = 0;
            }
        }

        var viAnchorIndex = -1;
        if (this.AnchoredItem != -1)
        {
            // Resolve the anchored item.
            viAnchorIndex = this.VisibleItems.BinarySearch(new VisibleItem(this.AnchoredItem));
            if (viAnchorIndex < 0)
            {
                viAnchorIndex = ~viAnchorIndex;
                this.visibleItems.Insert(viAnchorIndex, new(this.AnchoredItem));
            }

            ref var viAnchor = ref this.VisibleItems[viAnchorIndex];
            this.MeasureVisibleItem(ref viAnchor, nonLimDim, limDim);
            viAnchor.Offset = (limDim >= float.PositiveInfinity ? 0f : limDim) * this.AnchorOffsetRatio;
            viAnchor.Offset -= this.GetVisibleItemExpandingDimensionSize(viAnchor) *
                               this.AnchoredItemScrollOffsetRatio;

            viAnchor.Offset -= scrollOffsetDelta;
            viAnchor.Offset = MathF.Round(viAnchor.Offset * this.Parent.EffectiveRenderScale) /
                              this.Parent.EffectiveRenderScale;

            this.MeasureAroundAnchor(ref viAnchorIndex, itemCount, limDim, nonLimDim);
        }
        else
        {
            this.accumulatedScrollDelta = Vector2.Zero;
            this.smoothScrollAmount = Vector2.Zero;
            this.ScrollEasing.Reset();
        }

        if (this.visibleItems.Count == 0)
        {
            this.wasBeginningVisible = this.wasEndVisible = true;
            return new(Vector2.Zero, suggestedSize);
        }

        var isEverythingVisible =
            this.visibleItems[0].Index == 0
            && this.visibleItems[0].Offset >= 0
            && this.visibleItems[^1].Index == itemCount - 1
            && this.visibleItems[^1].Offset + this.GetVisibleItemExpandingDimensionSize(this.visibleItems[^1]) <=
            limDim;

        // Deal with vertical adjustments.
        {
            var visibleEndOffset = this.VisibleItems[^1].Offset +
                                   this.GetVisibleItemExpandingDimensionSize(this.VisibleItems[^1]);
            var visibleFirstOffset = this.VisibleItems[0].Offset;
            var visibleSize = visibleEndOffset - visibleFirstOffset;

            if (limDim >= float.PositiveInfinity)
            {
                // Deal with WrapContent limiting dimension.

                limDim = visibleSize;
                foreach (ref var v in this.VisibleItems)
                    v.Offset -= visibleFirstOffset;
                isEverythingVisible = true;

                this.wasBeginningVisible = this.wasEndVisible = true;
            }
            else if (visibleSize < limDim && this.visibleItems[0].Index == 0 &&
                     this.visibleItems[^1].Index == itemCount - 1)
            {
                // Apply gravity.

                var delta = ((limDim - visibleSize) * this.MainDirectionGravity) - visibleFirstOffset;
                foreach (ref var v in this.VisibleItems)
                    v.Offset += delta;

                this.wasBeginningVisible = this.wasEndVisible = true;
                isEverythingVisible = true;
            }
            else if (!isEverythingVisible)
            {
                var isBeginningVisible = false;
                var isEndVisible = false;
                var extendAgain = false;
                if (this.visibleItems[0].Index == 0 && this.visibleItems[0].Offset >= 0)
                {
                    // Prevent scrolling up too much.
                    var delta = this.visibleItems[0].Offset;
                    if (delta > 0)
                    {
                        foreach (ref var v in this.VisibleItems)
                            v.Offset -= delta;
                        extendAgain = true;
                    }

                    isBeginningVisible = true;
                }

                var lastItemBottom = this.visibleItems[^1].Offset +
                                     this.GetVisibleItemExpandingDimensionSize(this.visibleItems[^1]);
                if (this.visibleItems[^1].Index == itemCount - 1
                    && lastItemBottom - limDim <= this.Parent.EffectiveRenderScale)
                {
                    // Prevent scrolling down too much.
                    var delta = limDim - lastItemBottom;
                    if (delta > 0)
                    {
                        foreach (ref var v in this.VisibleItems)
                            v.Offset += delta;
                        extendAgain = true;
                    }

                    isEndVisible = true;
                }

                this.wasBeginningVisible = isBeginningVisible;
                this.wasEndVisible = isEndVisible;

                if (viAnchorIndex != -1 && extendAgain)
                {
                    this.MeasureAroundAnchor(
                        ref viAnchorIndex,
                        itemCount,
                        limDim,
                        nonLimDim);
                }
            }

            if (isEverythingVisible)
            {
                this.AnchoredItem = Math.Clamp(
                    (int)MathF.Round(itemCount * this.AnchorOffsetRatio),
                    0,
                    itemCount - 1);
                this.AnchoredItemScrollOffset = this.AnchoredItemScrollOffsetRatio = 0f;
            }
        }

        // Deal with WrapContent non-limiting dimension.
        if (nonLimDim >= float.PositiveInfinity)
        {
            nonLimDim = 0;
            foreach (ref var v in this.VisibleItems)
                nonLimDim = Math.Max(nonLimDim, this.GetVisibleItemNonExpandingDimensionSize(v));

            foreach (ref var v in this.VisibleItems)
                this.MeasureVisibleItem(ref v, nonLimDim, limDim);
        }

        // Round the offsets so that things do not look blurry due to being not pixel perfect.
        foreach (ref var item in this.VisibleItems)
        {
            item.Offset = MathF.Round(item.Offset * this.Parent.EffectiveRenderScale) /
                          this.Parent.EffectiveRenderScale;
        }

        // Figure out the anchor again.
        var anchorOffset = limDim * this.AnchorOffsetRatio;
        var anchoredItemDist = float.PositiveInfinity;
        this.AnchoredItem = -1;
        this.AnchoredItemScrollOffset = 0f;
        this.AnchoredItemScrollOffsetRatio = 0f;

        var nonLimDimScrollRangeMax = 0f;
        foreach (ref var vi in this.VisibleItems)
        {
            var limDimSize = this.GetVisibleItemExpandingDimensionSize(vi);
            if (limDimSize >= float.PositiveInfinity)
                continue;

            var nonLimDimSize = this.GetVisibleItemNonExpandingDimensionSize(vi);
            nonLimDimScrollRangeMax = Math.Max(nonLimDimScrollRangeMax, nonLimDimSize);

            var dist = 0f;
            if (anchorOffset < vi.Offset)
                dist = vi.Offset - anchorOffset;
            else if (anchorOffset > vi.Offset + limDimSize)
                dist = anchorOffset - (vi.Offset + limDimSize);
            if (this.AnchoredItem == -1 || dist < anchoredItemDist)
            {
                this.AnchoredItem = vi.Index;
                anchoredItemDist = dist;
                this.AnchoredItemScrollOffset = anchorOffset - vi.Offset;
                this.AnchoredItemScrollOffsetRatio = this.AnchoredItemScrollOffset / limDimSize;
            }
        }

        if (this.AnchoredItemScrollOffset is float.NaN)
            this.AnchoredItemScrollOffset = 0f;
        if (this.AnchoredItemScrollOffsetRatio is float.NaN)
            this.AnchoredItemScrollOffsetRatio = 0f;
        this.AnchoredItemScrollOffsetRatio = Math.Clamp(this.AnchoredItemScrollOffsetRatio, 0f, 1f);

        this.nonLimDimScroll = Math.Clamp(this.nonLimDimScroll, 0, Math.Max(0, nonLimDimScrollRangeMax - nonLimDim));

        if (prevAnchor != (this.AnchoredItem, this.AnchoredItemScrollOffsetRatio, this.nonLimDimScroll))
            this.needDispatchScrollEvent = true;

        this.CanScroll = this.visibleItems.Count > 0 && !isEverythingVisible;

        return new(Vector2.Zero, new(nonLimDim, limDim)); // VERTICAL
    }

    /// <inheritdoc/>
    protected override void UpdateTransformationChildren()
    {
        if (this.Parent is null)
            return;

        foreach (ref var vi in this.VisibleItems)
        {
            if (vi.CellDecorationMeasurement is not null)
            {
                // VERTICAL
                var translation = new Vector2(0, vi.Offset);

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);

                vi.CellDecorationMeasurement.UpdateTransformation(
                    Matrix4x4.CreateTranslation(new(translation, 0)),
                    this.Parent.FullTransformation);
            }

            if (vi.Measurement is not null)
            {
                var mtx = Matrix4x4.Identity;
                if (vi.Animation?.IsRunning is true)
                    mtx = vi.Animation.AnimatedTransformation;

                // VERTICAL
                var translation = new Vector2(
                    (this.Parent.MeasuredContentBox.Width - vi.Measurement.Boundary.Right) * this.OffDirectionGravity,
                    vi.Offset);
                translation.X -= this.nonLimDimScroll;

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);
                mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateTranslation(new(translation, 0)));
                vi.Measurement.UpdateTransformation(mtx, this.Parent.FullTransformation);
            }

            if (vi.PreviousMeasurement is not null)
            {
                var mtx = Matrix4x4.Identity;
                if (vi.PreviousAnimation?.IsRunning is true)
                    mtx = vi.PreviousAnimation.AnimatedTransformation;

                // VERTICAL
                var translation = new Vector2(
                    (this.Parent.MeasuredContentBox.Width - vi.PreviousMeasurement.Boundary.Right) *
                    this.OffDirectionGravity,
                    vi.Offset);
                translation.X -= this.nonLimDimScroll;

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);
                mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateTranslation(new(translation, 0)));
                vi.PreviousMeasurement.UpdateTransformation(mtx, this.Parent.FullTransformation);
            }
        }
    }

    /// <inheritdoc/>
    protected override void DrawChildren(ControlDrawEventArgs args)
    {
        foreach (var vi in this.visibleItems)
        {
            vi.CellDecorationMeasurement?.Draw(args.DrawListPtr);
            if (vi.PreviousAnimation?.AnimatedOpacity is { } pao and < 1f)
            {
                using var s = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, Vector2.One, pao);
                vi.PreviousMeasurement.Draw(args.DrawListPtr);
            }
            else
            {
                vi.PreviousMeasurement?.Draw(args.DrawListPtr);
            }

            if (vi.Animation?.AnimatedOpacity is { } aao and < 1f)
            {
                using var s = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, Vector2.One, aao);
                vi.Measurement.Draw(args.DrawListPtr);
            }
            else
            {
                vi.Measurement?.Draw(args.DrawListPtr);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionReset()
    {
        if (this.Collection is not { } col)
        {
            this.ClearVisibleItems();
            return;
        }

        foreach (ref var v in this.VisibleItems)
        {
            this.CurrentToPreviousVisibleItem(ref v);
            v.PreviousAnimation = new()
            {
                BeforeOpacity = 1f,
                AfterOpacity = 0f,
                OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
            };
            v.PreviousAnimation.Start();
            v.Removed = true;
        }

        if (col.Count == 0)
            return;

        var rangeFrom = Math.Max(0, this.AnchoredItem - 100);
        var rangeTo = Math.Max(col.Count - 1, this.AnchoredItem + 100);
        this.visibleItems.EnsureCapacity((rangeTo - rangeFrom) + 1);
        for (var i = rangeFrom; i <= rangeTo; i++)
        {
            this.visibleItems.Add(
                new(i)
                {
                    Animation = new()
                    {
                        BeforeOpacity = 0f,
                        AfterOpacity = 1f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    },
                });
            this.VisibleItems[^1].Animation!.Start();
        }

        this.VisibleItems.Sort();
    }

    /// <inheritdoc/>
    protected override void OnCollectionInsert(int startIndex, int count)
    {
        if (this.visibleItems.Count == 0)
        {
            if (this.Collection is not { } col)
                return;

            this.ClearVisibleItems();
            var index =
                this.AnchoredItem == -1
                    ? (int)Math.Clamp(count * this.AnchorOffsetRatio, 0, count - 1)
                    : Math.Clamp(this.AnchoredItem, 0, col.Count - 1);
            this.FirstVisibleItem = this.LastVisibleItem = this.AnchoredItem = index;
            this.AnchoredItemScrollOffsetRatio = this.AnchorOffsetRatio;
        }

        foreach (ref var v in this.VisibleItems)
        {
            if (v.Index >= startIndex && !v.Removed)
                v.Index++;
        }

        this.visibleItems.EnsureCapacity(this.visibleItems.Count + count);
        for (var i = 0; i < count; i++)
        {
            var index = startIndex + i;
            if (Math.Abs(this.AnchoredItem - index) > 100)
                continue;

            this.visibleItems.Add(
                new(index)
                {
                    Animation = new()
                    {
                        BeforeOpacity = 0f,
                        AfterOpacity = 1f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    },
                });
            this.VisibleItems[^1].Animation!.Start();
        }

        this.VisibleItems.Sort();

        if (this.AnchoredItem >= startIndex)
            this.AnchoredItem += count;
    }

    /// <inheritdoc/>
    protected override void OnCollectionRemove(int startIndex, int count)
    {
        var oldEndIndex = startIndex + count;
        foreach (ref var v in this.VisibleItems)
        {
            if (v.Index < startIndex || v.Removed)
                continue;

            if (v.Index < oldEndIndex)
            {
                this.CurrentToPreviousVisibleItem(ref v);
                v.PreviousAnimation = new()
                {
                    BeforeOpacity = 1f,
                    AfterOpacity = 0f,
                    OpacityEasing = new InCubic(TimeSpan.FromMilliseconds(200)),
                };
                v.PreviousAnimation.Start();
                v.Removed = true;
            }
            else
            {
                v.Index -= count;
            }
        }

        this.VisibleItems.Sort();

        if (this.AnchoredItem >= startIndex)
            this.AnchoredItem -= count;
    }

    /// <inheritdoc/>
    protected override void OnCollectionReplace(int startIndex, int count)
    {
        var replacementEndIndex = startIndex + count;
        foreach (ref var v in this.VisibleItems)
        {
            if (v.Index < startIndex || v.Index >= replacementEndIndex || v.Removed)
                continue;

            this.CurrentToPreviousVisibleItem(ref v);
            v.PreviousAnimation = new()
            {
                BeforeOpacity = 1f,
                AfterOpacity = 0f,
                OpacityEasing = new InCubic(TimeSpan.FromMilliseconds(200)),
            };
            v.PreviousAnimation.Start();

            v.Animation = new()
            {
                BeforeOpacity = 0f,
                AfterOpacity = 1f,
                OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
            };
            v.Animation.Start();

            v.SizeEasing = new InOutCubic(TimeSpan.FromMilliseconds(200));
            v.SizeEasing.Start();
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionMove(int oldStartIndex, int newStartIndex, int count)
    {
        var delta = newStartIndex - oldStartIndex;
        foreach (ref var v in this.VisibleItems)
        {
            if (v.Index < oldStartIndex || v.Index >= oldStartIndex + count || v.Removed)
                continue;
            v.Index += delta;
            // TODO: animate
        }

        if (oldStartIndex <= this.AnchoredItem && this.AnchoredItem < oldStartIndex + count)
            this.AnchoredItem += newStartIndex - oldStartIndex;
        this.VisibleItems.Sort();
    }

    private void MeasureAroundAnchor(
        ref int viAnchorIndex,
        int itemCount,
        float limDim,
        float nonLimDim)
    {
        // Resolve the item before the anchored item.
        var offset = this.VisibleItems[viAnchorIndex].Offset;
        var viMinItemIndex = viAnchorIndex - 1;
        for (this.FirstVisibleItem = this.AnchoredItem - 1;
             this.FirstVisibleItem >= 0;
             this.FirstVisibleItem--, viMinItemIndex--)
        {
            if (viMinItemIndex < 0 || this.visibleItems[viMinItemIndex].Index != this.FirstVisibleItem)
            {
                viAnchorIndex++;
                this.visibleItems.Insert(++viMinItemIndex, new(this.FirstVisibleItem));
            }

            ref var viItem = ref this.VisibleItems[viMinItemIndex];
            this.MeasureVisibleItem(ref viItem, nonLimDim, limDim);
            var viSize = this.GetVisibleItemExpandingDimensionSize(viItem);
            offset -= viSize;
            viItem.Offset = offset;
            if (viItem.Offset + viSize < 0 && limDim < float.PositiveInfinity)
                break;
            if (viItem.Removed)
                this.FirstVisibleItem++;
        }

        this.FirstVisibleItem++;

        // Resolve the item after the anchored item.
        offset = this.VisibleItems[viAnchorIndex].Offset +
                 this.GetVisibleItemExpandingDimensionSize(this.VisibleItems[viAnchorIndex]);
        var viMaxItemIndex = viAnchorIndex + 1;
        for (this.LastVisibleItem = this.AnchoredItem + 1;
             this.LastVisibleItem < itemCount;
             this.LastVisibleItem++, viMaxItemIndex++)
        {
            if (viMaxItemIndex >= this.visibleItems.Count ||
                this.visibleItems[viMaxItemIndex].Index != this.LastVisibleItem)
                this.visibleItems.Insert(viMaxItemIndex, new(this.LastVisibleItem));
            ref var viItem = ref this.VisibleItems[viMaxItemIndex];
            this.MeasureVisibleItem(ref viItem, nonLimDim, limDim);
            var viSize = this.GetVisibleItemExpandingDimensionSize(viItem);
            viItem.Offset = offset;
            offset += viSize;
            if (offset > limDim)
                break;
            if (viItem.Removed)
                this.LastVisibleItem--;
        }

        this.LastVisibleItem--;

        viMinItemIndex = Math.Max(viMinItemIndex, 0);
        viMaxItemIndex = Math.Min(viMaxItemIndex, this.visibleItems.Count - 1);

        // Should not happen, but just in case it gets desynchronized.
        // This probably means change notification is not being given or handled properly.
        while (viMaxItemIndex >= 0 && this.visibleItems[viMaxItemIndex].Index >= itemCount)
            viMaxItemIndex--;

        for (var i = this.visibleItems.Count - 1; i > viMaxItemIndex; i--)
        {
            this.ReturnVisibleItem(ref this.VisibleItems[i]);
            this.visibleItems.RemoveAt(i);
        }

        for (var i = viMinItemIndex - 1; i >= 0; i--)
        {
            this.ReturnVisibleItem(ref this.VisibleItems[i]);
            this.visibleItems.RemoveAt(i);
            if (i <= viAnchorIndex)
                viAnchorIndex--;
        }
    }

    private void MeasureVisibleItem(ref VisibleItem vi, float nonLimDim, float limDim)
    {
        if (this.Parent is null)
            return;

        if (vi.SpannableType == RecyclerViewControl.InvalidSpannableType)
        {
            if (vi.Removed)
                return;
            vi.SpannableType = this.ResolveSpannableType(vi.Index);
            vi.Spannable = this.TakePlaceholder(vi.SpannableType, out vi.SpannableSlot, out vi.SpannableInnerId);
        }

        if (!ReferenceEquals(this.CellDecoration, vi.CellDecorationSpannable))
        {
            vi.CellDecorationMeasurement?.ReturnMeasurementToSpannable();
            vi.CellDecorationMeasurement = null;

            vi.CellDecorationSpannable = this.CellDecoration;
            vi.CellDecorationMeasurement = this.CellDecoration?.RentMeasurement(this.Parent.Renderer);
            if (this.CellDecoration is not null)
                this.RentSlotAndInnerId(this.CellDecoration, out vi.CellDecorationSlot, out vi.CellDecorationInnerId);
        }

        if (vi.Measurement is null && vi.Spannable is not null)
        {
            vi.Measurement = vi.Spannable.RentMeasurement(this.Parent.Renderer);
            this.PopulateSpannable(vi.Index, vi.SpannableType, vi.Spannable, vi.Measurement);
        }

        if (vi.Measurement is { } m)
        {
            // VERTICAL
            m.Options.Size = new(nonLimDim, float.PositiveInfinity);
            m.Options.VisibleSize = new(nonLimDim, limDim);

            m.RenderScale = this.Parent.RenderScale;
            m.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.SpannableInnerId);
            m.Measure();
        }
        else
        {
            m = null;
        }

        if (vi.PreviousMeasurement is { } pm)
        {
            // VERTICAL
            pm.Options.Size = new(nonLimDim, float.PositiveInfinity);
            pm.Options.VisibleSize = new(nonLimDim, limDim);

            pm.RenderScale = this.Parent.RenderScale;
            pm.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.SpannableInnerId);
            pm.Measure();
        }
        else
        {
            pm = null;
        }

        if (vi.CellDecorationMeasurement is not null && (m ?? pm) is { } any)
        {
            // VERTICAL
            vi.CellDecorationMeasurement.Options.Size = new(nonLimDim, any.Boundary.Height);
            vi.CellDecorationMeasurement.Options.VisibleSize = new(nonLimDim, any.Boundary.Height);

            vi.CellDecorationMeasurement.RenderScale = this.Parent.RenderScale;
            vi.CellDecorationMeasurement.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.CellDecorationInnerId);
            vi.CellDecorationMeasurement.Measure();
        }
    }

    private void ClearVisibleItems()
    {
        foreach (ref var v in this.VisibleItems)
            this.ReturnVisibleItem(ref v);

        this.visibleItems.Clear();
        this.FirstVisibleItem = this.LastVisibleItem = this.AnchoredItem = -1;
    }

    private void ReturnVisibleItem(ref VisibleItem v)
    {
        this.ReturnCurrentVisibleItem(ref v);
        this.ReturnPreviousVisibleItem(ref v);
        v.CellDecorationMeasurement = null;
        v.CellDecorationSpannable = null;
        this.ReturnSlotAndInnerId(v.CellDecorationSlot, v.CellDecorationInnerId);
        v.CellDecorationSlot = v.CellDecorationInnerId = -1;
    }

    private float GetVisibleItemExpandingDimensionSize(in VisibleItem v)
    {
        var h = v.Measurement?.Boundary.Bottom ?? 0;
        if (v.PreviousMeasurement?.Boundary.Bottom is { } ph && v.SizeEasing is { } se)
            return float.Lerp(ph, h, (float)se.Value);
        return h;
    }

    private float GetVisibleItemNonExpandingDimensionSize(in VisibleItem v)
    {
        var h = v.Measurement?.Boundary.Right ?? 0;
        if (v.PreviousMeasurement?.Boundary.Right is { } ph && v.SizeEasing is { } se)
            return float.Lerp(ph, h, (float)se.Value);
        return h;
    }

    private void ReturnCurrentVisibleItem(ref VisibleItem v)
    {
        if (v.Spannable is not null && v.Measurement is not null)
            this.ClearSpannable(v.SpannableType, v.Spannable, v.Measurement);
        v.Spannable?.ReturnMeasurement(v.Measurement);
        this.ReturnPlaceholder(v.SpannableType, v.Spannable, v.SpannableSlot, v.SpannableInnerId);
        v.Spannable = null;
        v.Measurement = null;
        v.Animation = null;
        v.SpannableSlot = v.SpannableInnerId = -1;
    }

    private void ReturnPreviousVisibleItem(ref VisibleItem v)
    {
        if (v.PreviousSpannable is not null && v.PreviousMeasurement is not null)
            this.ClearSpannable(v.PreviousSpannableType, v.PreviousSpannable, v.PreviousMeasurement);
        v.PreviousSpannable?.ReturnMeasurement(v.PreviousMeasurement);
        this.ReturnPlaceholder(
            v.PreviousSpannableType,
            v.PreviousSpannable,
            v.PreviousSpannableSlot,
            v.PreviousSpannableInnerId);
        v.PreviousSpannable = null;
        v.PreviousMeasurement = null;
        v.PreviousAnimation = null;
        v.PreviousSpannableSlot = v.PreviousSpannableInnerId = -1;
    }

    private void CurrentToPreviousVisibleItem(ref VisibleItem v)
    {
        this.ReturnPreviousVisibleItem(ref v);

        v.PreviousMeasurement = v.Measurement;
        v.PreviousSpannable = v.Spannable;
        v.PreviousSpannableType = v.SpannableType;
        v.PreviousAnimation = null;
        v.PreviousSpannableSlot = v.SpannableSlot;
        v.PreviousSpannableInnerId = v.SpannableInnerId;

        v.Measurement = null;
        v.Spannable = null;
        v.SpannableType = RecyclerViewControl.InvalidSpannableType;
        v.Animation = null;
        v.SpannableSlot = v.SpannableInnerId = -1;
    }

    [DebuggerDisplay("#{Index} ({Spannable}): {Offset} / {Measurement}")]
    private struct VisibleItem : IComparable<VisibleItem>
    {
        public int Index;
        public float Offset;
        public bool Removed;

        public Easing? SizeEasing;

        public SpannableAnimator? PreviousAnimation;
        public int PreviousSpannableType;
        public ISpannable? PreviousSpannable;
        public ISpannableMeasurement? PreviousMeasurement;
        public int PreviousSpannableSlot;
        public int PreviousSpannableInnerId;

        public SpannableAnimator? Animation;
        public int SpannableType;
        public ISpannable? Spannable;
        public ISpannableMeasurement? Measurement;
        public int SpannableSlot;
        public int SpannableInnerId;

        public ISpannable? CellDecorationSpannable;
        public ISpannableMeasurement? CellDecorationMeasurement;
        public int CellDecorationSlot;
        public int CellDecorationInnerId;

        public VisibleItem(int index)
        {
            this.Index = index;
            this.PreviousSpannableType = this.SpannableType = RecyclerViewControl.InvalidSpannableType;
        }

        public void UpdateAnimation()
        {
            if (this.SizeEasing is not null)
            {
                this.SizeEasing.Update();
                if (this.SizeEasing.IsDone)
                    this.SizeEasing = null;
            }

            if (this.PreviousAnimation is not null)
            {
                if (this.PreviousMeasurement is not null)
                {
                    this.PreviousAnimation.Update(this.PreviousMeasurement);
                    if (this.PreviousAnimation.IsDone)
                        this.PreviousAnimation = null;
                }
                else
                {
                    this.PreviousAnimation = null;
                }
            }

            if (this.Animation is not null)
            {
                if (this.Measurement is not null)
                {
                    this.Animation.Update(this.Measurement);
                    if (this.Animation.IsDone)
                        this.Animation = null;
                }
                else
                {
                    this.Animation = null;
                }
            }
        }

        public readonly int CompareTo(VisibleItem other)
        {
            var r = this.Index.CompareTo(other.Index);
            if (r != 0)
                return r;
            return (this.Removed ? 0 : 1).CompareTo(this.Removed ? 0 : 1);
        }
    }
}

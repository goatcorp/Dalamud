using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.RecyclerViews;

#pragma warning disable SA1010
#pragma warning disable SA1101

/// <summary>A layout manager that lays out data in a single direction.</summary>
public class LinearLayoutManager : RecyclerViewControl.BaseLayoutManager
{
    private readonly List<VisibleEntry> visibleEntries = [];

    private LinearDirection direction = LinearDirection.LeftToRight;
    private Vector2 gravity;
    private float anchorOffsetRatio;
    private bool stickToTerminal;

    private long lastHandleInteractionTick = long.MaxValue;

    private Vector2 accumulatedScrollDelta;
    private Vector2 smoothScrollAmount;
    private float nonLimDimScroll;
    private bool needDispatchScrollEvent;
    private bool useOffDirectionScroll;

    private float scrollRequestedPosition;

    private bool wasBeginningVisible = true;
    private bool wasEndVisible = true;

    private bool useResetAnimationOnVisibleEntryPopulationOnce;

    /// <summary>Occurs when the property <see cref="Direction"/> is changing.</summary>
    public event PropertyChangeEventHandler<LinearDirection>? DirectionChange;

    /// <summary>Occurs when the property <see cref="Gravity"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? GravityChange;

    /// <summary>Occurs when the property <see cref="AnchorOffsetRatio"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? AnchorOffsetRatioChange;

    /// <summary>Occurs when the property <see cref="StickToTerminal"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? StickToTerminalChange;

    /// <summary>Occurs when the property <see cref="UseOffDirectionScroll"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? UseOffDirectionScrollChange;

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

    /// <summary>Gets or sets the gravity for the main direction.</summary>
    /// <value><c>0</c> will put the visible items at the beginning. <c>1</c> will put the visible items at the end.
    /// Values outside the range of [0, 1] will be clamped.</value>
    /// <remarks>Does nothing if there is no gap between the parent border and a visible item.</remarks>
    public Vector2 Gravity
    {
        get => this.gravity;
        set => this.HandlePropertyChange(
            nameof(this.Gravity),
            ref this.gravity,
            value,
            this.gravity == value,
            this.OnGravityChange);
    }

    /// <summary>Gets or sets the offset ratio of the anchored item.</summary>
    /// <value>0 to pin the first visible item at its position when items before or after change, and put the first
    /// item to be displayed ever since the collection has become non-empty at the beginning;
    /// 1 to pin the last visible item at its position, and the first item to be displayed at the end.</value>
    public float AnchorOffsetRatio
    {
        get => this.anchorOffsetRatio;
        set => this.HandlePropertyChange(
            nameof(this.AnchorOffsetRatio),
            ref this.anchorOffsetRatio,
            value,
            this.anchorOffsetRatio - value == 0f,
            this.OnAnchorOffsetRatioChange);
    }

    /// <summary>Gets or sets a value indicating whether to stick to terminal if already at the beginning or end of the
    /// scroll range. In case both beginning and end are visible, if <see cref="AnchorOffsetRatio"/> is less than
    /// 0.5, then it will stick to the beginning; otherwise, it will stick to the end.</summary>
    public bool StickToTerminal
    {
        get => this.stickToTerminal;
        set => this.HandlePropertyChange(
            nameof(this.StickToTerminal),
            ref this.stickToTerminal,
            value,
            this.stickToTerminal == value,
            this.OnStickToTerminalChange);
    }

    /// <summary>Gets or sets a value indicating whether to enable scrolling in the direction that is perpendicular to
    /// <see cref="Direction"/>.</summary>
    public bool UseOffDirectionScroll
    {
        get => this.useOffDirectionScroll;
        set => this.HandlePropertyChange(
            nameof(this.UseOffDirectionScroll),
            ref this.useOffDirectionScroll,
            value,
            this.useOffDirectionScroll == value,
            this.OnUseOffDirectionScrollChange);
    }

    /// <summary>Gets the index of the anchored item.</summary>
    /// <value><c>-1</c> if no item is anchored, because no item is visible.</value>
    /// <remarks>When item is added or removed above or the anchored item, the scroll position of the recycler view
    /// will be adjusted to keep the anchored item at the same position.</remarks>
    public int AnchoredItem { get; private set; } = -1;

    /// <summary>Gets the scrolled offset within the anchored item.</summary>
    public float AnchoredItemScrollOffset { get; private set; }

    /// <summary>Gets the scrolled offset ratio within the anchored item.</summary>
    public float AnchoredItemScrollOffsetRatio { get; private set; }

    /// <summary>Gets the scrolled offset within the first visible item.</summary>
    public float FirstVisibleItemScrollOffset { get; private set; }

    /// <summary>Gets the scrolled offset ratio within the first visible item.</summary>
    public float FirstVisibleItemScrollOffsetRatio { get; private set; }

    /// <summary>Gets the scrolled offset within the last visible item.</summary>
    public float LastVisibleItemScrollOffset { get; private set; }

    /// <summary>Gets the scrolled offset ratio within the last visible item.</summary>
    public float LastVisibleItemScrollOffsetRatio { get; private set; }

    /// <inheritdoc cref="ControlSpannable.IsAnyAnimationRunning"/>
    public override bool IsAnyAnimationRunning
    {
        get
        {
            if (this.Parent is null)
                return false;

            if (base.IsAnyAnimationRunning || this.Parent.AutoScrollPerSecond != Vector2.Zero)
                return true;

            foreach (var v in this.visibleEntries)
            {
                if (v.Animation?.IsRunning is true
                    || v.PreviousAnimation?.IsRunning is true
                    || v.SizeEasing?.IsRunning is true)
                    return true;
            }

            return false;
        }
    }

    private Span<VisibleEntry> VisibleEntries => CollectionsMarshal.AsSpan(this.visibleEntries);

    /// <summary>Gets a value indicating whether the lower index in the visible items, which always means the earlier
    /// items in the the given collection, are on the side with lesser coordinate values (left/top side.)</summary>
    private bool IsLowerIndexBeginningSide
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.direction.IsDirectionConsistentWithIndex();
    }

    /// <summary>Gets the gravity in the main direction specified from <see cref="Direction"/>,
    /// but using <c>0</c> as left or top, and <c>1</c> as right or bottom regardless of
    /// <see cref="IsLowerIndexBeginningSide"/>.</summary>
    private float MainDirectionScreenCoordinatesGravity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.direction.ConvertGravity(this.gravity.GetMainDirection(this.direction));
    }

    /// <summary>Gets the gravity in the off direction specified from <see cref="Direction"/>.</summary>
    private float OffDirectionScreenCoordinatesGravity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.direction.IsVertical() ? this.Gravity.X : this.Gravity.Y;
    }

    public void ScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);

    public void ScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();

    public void SmoothScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);

    public void SmoothScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void ScrollBy(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        this.accumulatedScrollDelta += delta;

        this.Parent?.RequestMeasure();
    }

    /// <inheritdoc/>
    public override void SmoothScrollBy(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

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

        this.Parent?.RequestMeasure();
    }

    /// <inheritdoc/>
    public override int FindItemIndexFromSpannable(Spannable? spannable)
    {
        if (spannable is null)
            return -1;
        foreach (ref var v in this.VisibleEntries)
        {
            if (ReferenceEquals(v.Spannable, spannable))
                return v.Index;
        }

        return -1;
    }

    /// <inheritdoc/>
    public override Spannable? FindMeasurementFromItemIndex(int index)
    {
        var vi = this.VisibleEntries.BinarySearch(new VisibleEntry(index));
        if (vi < 0)
            return null;
        return this.visibleEntries[vi].Spannable;
    }

    /// <inheritdoc/>
    public override Spannable? FindChildAtPos(Vector2 screenOffset)
    {
        foreach (var vi in this.visibleEntries)
        {
            if (vi.Spannable is not { } m)
                continue;
            if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                return m;
        }

        return null;
    }

    /// <inheritdoc/>
    public override Spannable? FindClosestChildMeasurementAt(Vector2 screenOffset)
    {
        var minDist = float.PositiveInfinity;
        Spannable? minItem = null;
        foreach (var vi in this.visibleEntries)
        {
            if (vi.Spannable is not { } m)
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
    public override IEnumerable<(int Index, Spannable Spannable)> EnumerateItemSpannableMeasurements()
    {
        foreach (var vi in this.visibleEntries)
        {
            if (vi.Spannable is not null)
                yield return (vi.Index, vi.Spannable);
        }
    }

    /// <inheritdoc/>
    public override void PreDispatchEvents()
    {
        base.PreDispatchEvents();

        if (this.Parent is null)
            return;

        foreach (var vi in this.visibleEntries)
            vi.Spannable?.RenderPassPreDispatchEvents();

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
                this.ScrollByLines(this.Parent.AutoScrollPerSecond * secondsPastLastTick);
                this.lastHandleInteractionTick = now;
            }
        }
        else
        {
            this.lastHandleInteractionTick = now;
        }
    }

    /// <inheritdoc/>
    protected override void BeforeParentDetach()
    {
        foreach (ref var x in this.VisibleEntries)
            this.ReturnVisibleEntry(ref x);
        this.visibleEntries.Clear();
        this.Parent!.VerticalScrollBar.Scroll -= this.VerticalScrollBarOnScroll;
        this.Parent!.HorizontalScrollBar.Scroll -= this.HorizontalScrollBarOnScroll;
        base.BeforeParentDetach();
    }

    /// <inheritdoc/>
    protected override void AfterParentAttach()
    {
        base.AfterParentAttach();
        this.lastHandleInteractionTick = long.MaxValue;
        this.nonLimDimScroll = 0;
        this.smoothScrollAmount = Vector2.Zero;
        this.accumulatedScrollDelta = Vector2.Zero;
        this.AnchoredItem = -1;
        this.scrollRequestedPosition = float.NaN;
        this.Parent!.VerticalScrollBar.Scroll += VerticalScrollBarOnScroll;
        this.Parent!.HorizontalScrollBar.Scroll += this.HorizontalScrollBarOnScroll;
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureChildren(Vector2 suggestedSize)
    {
        // A copy of previous anchor position, to test whether scroll happened.
        var prevAnchor = (this.AnchoredItem, this.AnchoredItemScrollOffsetRatio, this.nonLimDimScroll);

        // If the collection is not bound, then stop.
        if (this.Parent is null || this.Collection is not { Count: var itemCount })
        {
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.ClearVisibleEntries();
            return new(Vector2.Zero, suggestedSize);
        }

        // Update animations, reset measurement state, and process items with finished remove animations.
        for (var i = this.visibleEntries.Count - 1; i >= 0; i--)
        {
            ref var item = ref this.VisibleEntries[i];
            item.Measured = false;
            if (item.PreviousAnimation?.IsDone is not true)
                continue;

            if (item.Removed)
            {
                this.ReturnVisibleEntry(ref item);
                this.visibleEntries.RemoveAt(i);
            }
            else
            {
                this.ReturnPreviousVisibleEntry(ref item);
            }
        }

        // If there is nothing being displayed and nothing to display, then stop.
        if (this.visibleEntries.Count == 0 && itemCount == 0)
        {
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.ClearVisibleEntries();
            return new(Vector2.Zero, suggestedSize);
        }

        this.GetDimensions(suggestedSize, out var expandingDimension, out var nonExpandingDimension);

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
            if (this.direction.IsVertical())
            {
                scrollOffsetDelta += this.accumulatedScrollDelta.Y;
                this.nonLimDimScroll += this.accumulatedScrollDelta.X;
            }
            else
            {
                scrollOffsetDelta += this.accumulatedScrollDelta.X;
                this.nonLimDimScroll += this.accumulatedScrollDelta.Y;
            }

            this.accumulatedScrollDelta = Vector2.Zero;
        }
        else if (this.StickToTerminal)
        {
            var a = (this.AnchorOffsetRatio < 0.5f) == this.IsLowerIndexBeginningSide;
            if (this.wasBeginningVisible && (!this.wasEndVisible || !a))
            {
                // Stick to beginning
                this.AnchoredItem = 0;
                this.AnchoredItemScrollOffsetRatio = this.direction.ConvertGravity(0);
            }
            else if (this.wasEndVisible && (!this.wasBeginningVisible || a))
            {
                // Stick to the end
                this.AnchoredItem = itemCount - 1;
                this.AnchoredItemScrollOffsetRatio = this.direction.ConvertGravity(1);
            }
        }

        if (!this.useOffDirectionScroll)
            this.nonLimDimScroll = 0;

        if (itemCount > 0)
        {
            if (this.AnchoredItem == -1)
                this.AnchoredItem = (int)MathF.Round(this.AnchorOffsetRatio * itemCount);
            this.AnchoredItem = Math.Clamp(this.AnchoredItem, 0, itemCount - 1);
        }
        else
        {
            this.AnchoredItem = -1;
        }

        // Temporarily use scroll request information as anchor, if specified.
        if (this.scrollRequestedPosition is not float.NaN)
        {
            // idk why but this seems to work for now
            const int numVisibleEntriesDelta = 3;

            var itemCountRange = (itemCount - this.visibleEntries.Count) + numVisibleEntriesDelta;
            var itemIndexOffset =
                Math.Max(
                    (int)MathF.Ceiling(this.visibleEntries.Count * this.AnchorOffsetRatio) - numVisibleEntriesDelta,
                    0);
            this.AnchoredItem = Math.Clamp(
                (int)(itemIndexOffset + (this.scrollRequestedPosition * itemCountRange)),
                0,
                itemCount - 1);
            this.AnchoredItemScrollOffsetRatio = this.direction.ConvertGravity(this.anchorOffsetRatio);
            this.scrollRequestedPosition = float.NaN;
        }

        var veAnchorIndex = -1;
        if (this.AnchoredItem != -1)
        {
            // Resolve the anchored item.
            veAnchorIndex = this.VisibleEntries.BinarySearch(new VisibleEntry(this.AnchoredItem));
            if (veAnchorIndex < 0)
            {
                veAnchorIndex = ~veAnchorIndex;
                this.visibleEntries.Insert(veAnchorIndex, new(this.AnchoredItem));
                if (this.useResetAnimationOnVisibleEntryPopulationOnce)
                {
                    this.ResolveChangeAnimations(NotifyCollectionChangedAction.Reset, false, true, out _, out var na);
                    this.VisibleEntries[veAnchorIndex].Animation = na;
                }
            }

            ref var veAnchor = ref this.VisibleEntries[veAnchorIndex];
            this.MeasureVisibleEntry(ref veAnchor, nonExpandingDimension, expandingDimension);

            // Navigate to the offset that the anchor specifies.
            if (expandingDimension >= float.PositiveInfinity)
                veAnchor.Offset = 0f;
            else
                veAnchor.Offset = expandingDimension * this.direction.ConvertGravity(this.AnchorOffsetRatio);

            // Navigate to the offset that the value that specifies the ratio inside anchor specified.
            veAnchor.Offset -= this.GetExpandingDimensionLerped(veAnchor) * this.AnchoredItemScrollOffsetRatio;

            // Apply latest extra scrolling.
            veAnchor.Offset -= scrollOffsetDelta;

            // Round so that no subpixel offset arises.
            veAnchor.Offset =
                MathF.Round(veAnchor.Offset * this.Parent.EffectiveRenderScale) / this.Parent.EffectiveRenderScale;

            this.MeasureAroundAnchor(ref veAnchorIndex, itemCount, expandingDimension, nonExpandingDimension);
        }
        else
        {
            this.accumulatedScrollDelta = Vector2.Zero;
            this.smoothScrollAmount = Vector2.Zero;
            this.ScrollEasing.Reset();

            // Deal with the case where the collection got cleared yet there are removal animations remaining.
            var updated = false;
            foreach (var v in this.VisibleEntries)
                updated |= v.UpdateAnimation();
            if (updated)
                this.Parent?.RequestMeasure();
        }

        // Find the first and last visible entry indices.
        var lesserOffsetVisibleEntryIndex = 0;
        var greaterOffsetVisibleEntryIndex = this.visibleEntries.Count - 1;
        if (true)
        {
            var ves = this.VisibleEntries;
            while (lesserOffsetVisibleEntryIndex < this.visibleEntries.Count
                   && lesserOffsetVisibleEntryIndex < greaterOffsetVisibleEntryIndex
                   && ves[lesserOffsetVisibleEntryIndex].OutsideViewport)
                lesserOffsetVisibleEntryIndex++;

            while (greaterOffsetVisibleEntryIndex >= 0
                   && lesserOffsetVisibleEntryIndex < greaterOffsetVisibleEntryIndex
                   && ves[greaterOffsetVisibleEntryIndex].OutsideViewport)
                greaterOffsetVisibleEntryIndex--;
        }

        // If the above MeasureAroundAnchor effectively cleared visible items, then stop.
        if (lesserOffsetVisibleEntryIndex > greaterOffsetVisibleEntryIndex)
        {
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.ClearVisibleEntries();
            return new(Vector2.Zero, suggestedSize);
        }

        // Swap the lesser and greater, as the offsets are descending whereas visible entry indices are ascending.
        if (!this.IsLowerIndexBeginningSide)
        {
            (lesserOffsetVisibleEntryIndex, greaterOffsetVisibleEntryIndex) =
                (greaterOffsetVisibleEntryIndex, lesserOffsetVisibleEntryIndex);
        }

        // Deal with main direction adjustments.
        bool isEverythingFullyVisible;
        if (true)
        {
            ref var lesserOffsetVisibleEntry = ref this.VisibleEntries[lesserOffsetVisibleEntryIndex];
            ref var greaterOffsetVisibleEntry = ref this.VisibleEntries[greaterOffsetVisibleEntryIndex];

            var lesserOffsetTerminalItemIndex = this.IsLowerIndexBeginningSide ? 0 : itemCount - 1;
            var greaterOffsetTerminalItemIndex = this.IsLowerIndexBeginningSide ? itemCount - 1 : 0;

            var lesserOffsetVisibleEntryTop = lesserOffsetVisibleEntry.Offset;
            var greaterOffsetVisibleEntryBottom =
                greaterOffsetVisibleEntry.Offset + this.GetExpandingDimensionLerped(greaterOffsetVisibleEntry);

            var isEveryItemAssignedVisibleEntry =
                this.visibleEntries[
                    Math.Min(lesserOffsetVisibleEntryIndex, greaterOffsetVisibleEntryIndex)].Index == 0
                && this.visibleEntries[
                    Math.Max(lesserOffsetVisibleEntryIndex, greaterOffsetVisibleEntryIndex)].Index == itemCount - 1;
            isEverythingFullyVisible =
                isEveryItemAssignedVisibleEntry
                && lesserOffsetVisibleEntry.Offset >= 0
                && greaterOffsetVisibleEntryBottom <= expandingDimension;

            var visibleSize = greaterOffsetVisibleEntryBottom - lesserOffsetVisibleEntryTop;

            if (expandingDimension >= float.PositiveInfinity)
            {
                // Deal with WrapContent limiting dimension.

                expandingDimension = visibleSize;
                foreach (ref var v in this.VisibleEntries)
                    v.Offset -= lesserOffsetVisibleEntryTop;
                isEverythingFullyVisible = true;

                this.wasBeginningVisible = this.wasEndVisible = true;
            }
            else if (!isEverythingFullyVisible)
            {
                var isBeginningVisible = false;
                var isEndVisible = false;
                var extendAgain = false;

                if (lesserOffsetVisibleEntry.Index == lesserOffsetTerminalItemIndex &&
                    lesserOffsetVisibleEntry.Offset >= 0)
                {
                    // Prevent scrolling up too much.
                    var delta = lesserOffsetVisibleEntry.Offset;
                    if (delta > 0)
                    {
                        foreach (ref var v in this.VisibleEntries)
                            v.Offset -= delta;
                        extendAgain = true;
                    }

                    if (this.IsLowerIndexBeginningSide)
                        isBeginningVisible = true;
                    else
                        isEndVisible = true;
                }

                if (greaterOffsetVisibleEntry.Index == greaterOffsetTerminalItemIndex
                    && greaterOffsetVisibleEntryBottom - expandingDimension <= this.Parent.EffectiveRenderScale)
                {
                    // Prevent scrolling down too much.
                    var delta = expandingDimension - greaterOffsetVisibleEntryBottom;
                    if (delta > 0)
                    {
                        foreach (ref var v in this.VisibleEntries)
                            v.Offset += delta;
                        extendAgain = true;
                    }

                    if (this.IsLowerIndexBeginningSide)
                        isEndVisible = true;
                    else
                        isBeginningVisible = true;
                }

                this.wasBeginningVisible = isBeginningVisible;
                this.wasEndVisible = isEndVisible;

                if (veAnchorIndex != -1 && extendAgain)
                    this.MeasureAroundAnchor(ref veAnchorIndex, itemCount, expandingDimension, nonExpandingDimension);
            }

            if (isEverythingFullyVisible)
            {
                this.AnchoredItem = Math.Clamp(
                    (int)MathF.Round(itemCount * this.AnchorOffsetRatio),
                    0,
                    itemCount - 1);
                this.AnchoredItemScrollOffset = this.AnchoredItemScrollOffsetRatio = 0f;
            }
        }

        // Remove entries that are outside viewport.
        // We do this here since we call MeasureAroundAnchor up to twice, which may temporarily consider a visible item
        // invisible due to scrolling adjustments.
        if (true)
        {
            var i = 0;
            var ves = this.VisibleEntries;
            for (; i < ves.Length; i++)
            {
                if (!ves[i].OutsideViewport)
                    break;
                this.ReturnVisibleEntry(ref ves[i]);
            }

            this.visibleEntries.RemoveRange(0, i);

            ves = this.VisibleEntries;
            for (i = ves.Length; i > 0; i--)
            {
                if (!ves[i - 1].OutsideViewport)
                    break;
                this.ReturnVisibleEntry(ref ves[i - 1]);
            }

            this.visibleEntries.RemoveRange(i, ves.Length - i);

            lesserOffsetVisibleEntryIndex = this.IsLowerIndexBeginningSide ? 0 : this.visibleEntries.Count - 1;
            greaterOffsetVisibleEntryIndex = this.IsLowerIndexBeginningSide ? this.visibleEntries.Count - 1 : 0;
        }

        // If the list of visible entries is empty after the above operation, stop.
        if (this.visibleEntries.Count == 0)
        {
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = 0;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = 0;
            this.ClearVisibleEntries();
            return new(Vector2.Zero, suggestedSize);
        }

        this.VisibleItemCount = this.visibleEntries.Count;

        // If everything is contained in the viewport, apply gravity.
        if (true)
        {
            ref var lesserOffsetVisibleEntry = ref this.VisibleEntries[lesserOffsetVisibleEntryIndex];
            ref var greaterOffsetVisibleEntry = ref this.VisibleEntries[greaterOffsetVisibleEntryIndex];

            var isEveryItemAssignedVisibleEntry =
                this.visibleEntries[0].Index == 0 && this.visibleEntries[^1].Index == itemCount - 1;

            var lesserOffsetVisibleEntryTop = lesserOffsetVisibleEntry.Offset;
            var greaterOffsetVisibleEntryBottom =
                greaterOffsetVisibleEntry.Offset + this.GetExpandingDimensionLerped(greaterOffsetVisibleEntry);

            var visibleSize = greaterOffsetVisibleEntryBottom - lesserOffsetVisibleEntryTop;

            if (visibleSize <= expandingDimension && isEveryItemAssignedVisibleEntry)
            {
                var delta =
                    ((expandingDimension - visibleSize) * this.MainDirectionScreenCoordinatesGravity) -
                    lesserOffsetVisibleEntryTop;
                foreach (ref var v in this.VisibleEntries)
                    v.Offset += delta;

                this.wasBeginningVisible = this.wasEndVisible = true;
                isEverythingFullyVisible = true;
            }
        }

        // Deal with WrapContent non-limiting dimension.
        if (nonExpandingDimension >= float.PositiveInfinity)
        {
            nonExpandingDimension = 0;
            foreach (ref var v in this.VisibleEntries)
                nonExpandingDimension = Math.Max(nonExpandingDimension, this.GetNonExpandingDimensionLerped(v));

            foreach (ref var v in this.VisibleEntries)
                this.MeasureVisibleEntry(ref v, nonExpandingDimension, expandingDimension);
        }

        // Round the offsets so that things do not look blurry due to being not pixel perfect.
        foreach (ref var item in this.VisibleEntries)
        {
            item.Offset = MathF.Round(item.Offset * this.Parent.EffectiveRenderScale) /
                          this.Parent.EffectiveRenderScale;
        }

        // Figure out the anchor again.
        if (true)
        {
            var anchorOffset = expandingDimension * this.direction.ConvertGravity(this.anchorOffsetRatio);
            var anchoredItemDist = float.PositiveInfinity;
            this.AnchoredItem = -1;
            this.AnchoredItemScrollOffset = 0f;
            this.AnchoredItemScrollOffsetRatio = 0f;

            var nonLimDimScrollRangeMax = 0f;
            foreach (ref var vi in this.VisibleEntries)
            {
                var limDimSize = this.GetExpandingDimensionLerped(vi);
                if (limDimSize >= float.PositiveInfinity)
                    continue;

                var nonLimDimSize = this.GetNonExpandingDimensionLerped(vi);
                nonLimDimScrollRangeMax = Math.Max(nonLimDimScrollRangeMax, nonLimDimSize);

                var dist = 0f;
                if (anchorOffset < vi.Offset)
                    dist = vi.Offset - anchorOffset;
                else if (anchorOffset >= vi.Offset + limDimSize)
                    dist = 1 + anchorOffset - (vi.Offset + limDimSize);
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

            nonLimDimScrollRangeMax = Math.Max(0, nonLimDimScrollRangeMax - nonExpandingDimension);
            this.nonLimDimScroll = Math.Clamp(this.nonLimDimScroll, 0, nonLimDimScrollRangeMax);
            if (this.direction.IsVertical())
                this.ScrollRange = this.ScrollRange with { X = nonLimDimScrollRangeMax };
            else
                this.ScrollRange = this.ScrollRange with { Y = nonLimDimScrollRangeMax };
        }

        if (prevAnchor != (this.AnchoredItem, this.AnchoredItemScrollOffsetRatio, this.nonLimDimScroll))
            this.needDispatchScrollEvent = true;

        // Count the "exact" number of items visible, counting invisible sections out.
        var numVisible = (float)this.visibleEntries.Count;
        if (numVisible > 0)
        {
            var o0 = this.VisibleEntries[0].Offset;
            var h0 = GetExpandingDimensionLerped(this.VisibleEntries[0]);
            var o1 = this.VisibleEntries[^1].Offset;
            var h1 = GetExpandingDimensionLerped(this.VisibleEntries[^1]);
            if (o0 < 0)
                numVisible += o0 / h0;
            if (o1 < 0)
                numVisible += o1 / h1;
            if (o0 + h0 >= expandingDimension)
                numVisible -= ((o0 + h0) - expandingDimension) / h0;
            if (o1 + h1 >= expandingDimension)
                numVisible -= ((o1 + h1) - expandingDimension) / h1;
        }

        if (this.direction.IsDirectionConsistentWithIndex())
        {
            this.FirstVisibleItemScrollOffset = -this.visibleEntries[0].Offset;
            this.FirstVisibleItemScrollOffsetRatio =
                this.FirstVisibleItemScrollOffset / this.GetExpandingDimensionLerped(this.visibleEntries[0]);

            this.LastVisibleItemScrollOffset = expandingDimension - this.visibleEntries[^1].Offset;
            this.LastVisibleItemScrollOffsetRatio =
                this.LastVisibleItemScrollOffset / this.GetExpandingDimensionLerped(this.visibleEntries[^1]);
        }
        else
        {
            this.FirstVisibleItemScrollOffset = expandingDimension - this.visibleEntries[0].Offset;
            this.FirstVisibleItemScrollOffsetRatio =
                this.FirstVisibleItemScrollOffset / this.GetExpandingDimensionLerped(this.visibleEntries[0]);

            this.LastVisibleItemScrollOffset = -this.visibleEntries[^1].Offset;
            this.LastVisibleItemScrollOffsetRatio =
                this.LastVisibleItemScrollOffset / this.GetExpandingDimensionLerped(this.visibleEntries[^1]);
        }

        if (isEverythingFullyVisible)
        {
            if (this.direction.IsVertical())
            {
                this.ScrollRange = this.ScrollRange with { Y = 0 };
                this.ScrollPosition = this.ScrollPosition with { Y = 0 };
            }
            else
            {
                this.ScrollRange = this.ScrollRange with { X = 0 };
                this.ScrollPosition = this.ScrollPosition with { X = 0 };
            }
        }
        else
        {
            // Estimate the scroll range from what's currently visible.
            if (this.direction.IsDirectionConsistentWithIndex())
            {
                this.ScrollPosition =
                    this.direction.CreateMainOff(
                        this.FirstVisibleItem + this.FirstVisibleItemScrollOffsetRatio,
                        this.nonLimDimScroll);
                this.ScrollRange = this.ScrollRange.UpdateMainDirection(
                    this.direction,
                    itemCount - (numVisible * ((float)(this.LastVisibleItem + 1) / itemCount)));
            }
            else
            {
                this.ScrollPosition =
                    this.direction.CreateMainOff(
                        (this.FirstVisibleItem + 1) - this.FirstVisibleItemScrollOffsetRatio,
                        this.nonLimDimScroll);
                this.ScrollRange = this.ScrollRange.UpdateMainDirection(
                    this.direction,
                    itemCount - (numVisible * ((float)(this.LastVisibleItem + 1) / itemCount)));
            }
        }

        if (this.direction.IsVertical())
        {
            this.Parent.VerticalScrollBar.Direction = this.direction;
            this.Parent.HorizontalScrollBar.Direction = LinearDirection.LeftToRight;
            if (numVisible > 0)
                this.Parent.VerticalScrollBar.PageSizeProportion = numVisible / (itemCount + 1);
            if (this.ScrollRange.X > 0)
            {
                this.Parent.HorizontalScrollBar.PageSizeProportion =
                    nonExpandingDimension / (nonExpandingDimension + this.ScrollRange.X);
            }
        }
        else
        {
            this.Parent.HorizontalScrollBar.Direction = this.direction;
            this.Parent.VerticalScrollBar.Direction = LinearDirection.TopToBottom;
            if (numVisible > 0)
                this.Parent.HorizontalScrollBar.PageSizeProportion = numVisible / (itemCount + 1);
            if (this.ScrollRange.Y > 0)
            {
                this.Parent.VerticalScrollBar.PageSizeProportion =
                    nonExpandingDimension / (nonExpandingDimension + this.ScrollRange.Y);
            }
        }

        this.useResetAnimationOnVisibleEntryPopulationOnce = false;

        return new(Vector2.Zero, this.direction.CreateMainOff(expandingDimension, nonExpandingDimension));
    }

    /// <inheritdoc/>
    protected override void PlaceChildren()
    {
        if (this.Parent is null)
            return;

        foreach (ref var vi in this.VisibleEntries)
        {
            if (vi.Decoration is not null)
            {
                Vector2 translation =
                    this.direction.IsVertical()
                        ? new(0, vi.Offset)
                        : new(vi.Offset, 0);

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);

                vi.Decoration.RenderPassPlace(
                    Matrix4x4.CreateTranslation(new(translation, 0)),
                    this.Parent.FullTransformation);
            }

            if (vi.Spannable is not null)
            {
                var mtx = Matrix4x4.Identity;
                if (vi.Animation?.IsRunning is true)
                    mtx = vi.Animation.AnimatedTransformation;

                Vector2 translation =
                    this.direction.IsVertical()
                        ? new(
                            ((this.Parent.MeasuredContentBox.Width - vi.Spannable.Boundary.Right)
                             * this.OffDirectionScreenCoordinatesGravity)
                            - this.nonLimDimScroll,
                            vi.Offset)
                        : new(
                            vi.Offset,
                            ((this.Parent.MeasuredContentBox.Height - vi.Spannable.Boundary.Bottom)
                             * this.OffDirectionScreenCoordinatesGravity)
                            - this.nonLimDimScroll);

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);
                mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateTranslation(new(translation, 0)));
                vi.Spannable.RenderPassPlace(mtx, this.Parent.FullTransformation);
            }

            if (vi.Previous is not null)
            {
                var mtx = Matrix4x4.Identity;
                if (vi.PreviousAnimation?.IsRunning is true)
                    mtx = vi.PreviousAnimation.AnimatedTransformation;

                Vector2 translation =
                    this.direction.IsVertical()
                        ? new(
                            ((this.Parent.MeasuredContentBox.Width - vi.Previous.Boundary.Right)
                             * this.OffDirectionScreenCoordinatesGravity)
                            - this.nonLimDimScroll,
                            vi.Offset)
                        : new(
                            vi.Offset,
                            ((this.Parent.MeasuredContentBox.Height - vi.Previous.Boundary.Bottom)
                             * this.OffDirectionScreenCoordinatesGravity)
                            - this.nonLimDimScroll);

                translation += this.Parent.MeasuredContentBox.LeftTop;
                translation = translation.Round(1f / this.Parent.EffectiveRenderScale);
                mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateTranslation(new(translation, 0)));
                vi.Previous.RenderPassPlace(mtx, this.Parent.FullTransformation);
            }
        }
    }

    /// <inheritdoc/>
    protected override void DrawChildren(SpannableDrawEventArgs args)
    {
        foreach (var vi in this.visibleEntries)
        {
            var pao = vi.PreviousAnimation?.AnimatedOpacity ?? 1f;
            var aao = vi.Animation?.AnimatedOpacity ?? 1f;

            if (vi.Decoration is not null)
            {
                // If both PreviousSpannable and Spannable are set, then a replace animation is being played.
                // Cell decoration is always rendered in full opacity in that case.
                // Otherwise, prefer the opacity from the animation for current spannable..
                var cdao =
                    vi.Previous is not null && vi.Spannable is not null
                        ? 1f
                        : vi.Spannable is not null
                            ? aao
                            : pao;
                if (cdao < 1f)
                {
                    using var s = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, Vector2.One, cdao);
                    vi.Decoration.RenderPassDraw(args.DrawListPtr);
                }
                else
                {
                    vi.Decoration.RenderPassDraw(args.DrawListPtr);
                }
            }

            if (vi.Previous is not null)
            {
                if (pao < 1f)
                {
                    using var s = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, Vector2.One, pao);
                    vi.Previous.RenderPassDraw(args.DrawListPtr);
                }
                else
                {
                    vi.Previous.RenderPassDraw(args.DrawListPtr);
                }
            }

            if (vi.Spannable is not null)
            {
                if (aao < 1f)
                {
                    using var s = new ScopedTransformer(args.DrawListPtr, Matrix4x4.Identity, Vector2.One, aao);
                    vi.Spannable.RenderPassDraw(args.DrawListPtr);
                }
                else
                {
                    vi.Spannable.RenderPassDraw(args.DrawListPtr);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionReset()
    {
        if (this.Collection is not { } col)
        {
            this.ClearVisibleEntries();
            return;
        }

        foreach (ref var v in this.VisibleEntries)
        {
            this.CurrentToPreviousVisibleEntry(ref v);

            this.ResolveChangeAnimations(NotifyCollectionChangedAction.Reset, true, false, out var pa, out _);
            v.PreviousAnimation = pa;
            v.Removed = true;
        }

        if (col.Count == 0)
        {
            this.AnchoredItem = -1;
            return;
        }

        if (this.AnchoredItem >= col.Count)
            this.AnchoredItem = col.Count - 1;

        this.useResetAnimationOnVisibleEntryPopulationOnce = true;
    }

    /// <inheritdoc/>
    protected override void OnCollectionInsert(int startIndex, int count)
    {
        if (this.visibleEntries.Count == 0)
        {
            this.ClearVisibleEntries();

            if (this.Collection is null)
                return;
        }

        foreach (ref var v in this.VisibleEntries)
        {
            if (v.Index >= startIndex && !v.Removed)
                v.Index++;
        }

        this.visibleEntries.EnsureCapacity(this.visibleEntries.Count + count);
        for (var i = 0; i < count; i++)
        {
            var index = startIndex + i;
            if (Math.Abs(this.AnchoredItem - index) > 100)
                continue;

            this.ResolveChangeAnimations(NotifyCollectionChangedAction.Add, false, true, out _, out var na);
            this.visibleEntries.Add(new(index) { Animation = na });
        }

        this.VisibleEntries.Sort();

        if (this.AnchoredItem >= startIndex)
            this.AnchoredItem += count;
    }

    /// <inheritdoc/>
    protected override void OnCollectionRemove(int startIndex, int count)
    {
        var oldEndIndex = startIndex + count;
        foreach (ref var v in this.VisibleEntries)
        {
            if (v.Index < startIndex || v.Removed)
                continue;

            if (v.Index < oldEndIndex)
            {
                this.CurrentToPreviousVisibleEntry(ref v);
                this.ResolveChangeAnimations(NotifyCollectionChangedAction.Remove, true, false, out var pa, out _);
                v.PreviousAnimation = pa;
                v.Removed = true;
            }
            else
            {
                v.Index -= count;
            }
        }

        this.VisibleEntries.Sort();

        if (this.AnchoredItem >= startIndex)
            this.AnchoredItem -= count;
    }

    /// <inheritdoc/>
    protected override void OnCollectionReplace(int startIndex, int count)
    {
        var replacementEndIndex = startIndex + count;
        foreach (ref var v in this.VisibleEntries)
        {
            if (v.Index < startIndex || v.Index >= replacementEndIndex || v.Removed)
                continue;

            this.CurrentToPreviousVisibleEntry(ref v);
            this.ReturnDecorativeVisibleEntry(ref v);
            this.ResolveChangeAnimations(NotifyCollectionChangedAction.Replace, true, true, out var pa, out var na);
            v.PreviousAnimation = pa;
            v.Animation = na;
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionMove(int oldStartIndex, int newStartIndex, int count)
    {
        var delta = newStartIndex - oldStartIndex;
        foreach (ref var v in this.VisibleEntries)
        {
            if (v.Index < oldStartIndex || v.Index >= oldStartIndex + count || v.Removed)
                continue;
            v.Index += delta;

            this.ResolveChangeAnimations(NotifyCollectionChangedAction.Move, false, true, out _, out var na);
            v.Animation = na;

            // TODO: animate moving itself
        }

        if (oldStartIndex <= this.AnchoredItem && this.AnchoredItem < oldStartIndex + count)
            this.AnchoredItem += newStartIndex - oldStartIndex;
        this.VisibleEntries.Sort();
    }

    /// <inheritdoc/>
    protected override void OnSetupChangeAnimation(SetupChangeAnimationEventArg args)
    {
        base.OnSetupChangeAnimation(args);

        if (!args.UseDefault)
            return;

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add
                when args is { WantAnimation: true, Animation: null }:
                args.Animation = new()
                {
                    BeforeOpacity = 0.5f,
                    AfterOpacity = 1f,
                    OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                };
                break;

            case NotifyCollectionChangedAction.Remove
                when args is { WantPreviousAnimation: true, PreviousAnimation: null }:
                args.PreviousAnimation = new()
                {
                    BeforeOpacity = 1f,
                    AfterOpacity = 0f,
                    OpacityEasing = new InCubic(TimeSpan.FromMilliseconds(200)),
                };
                break;

            case NotifyCollectionChangedAction.Replace:
                if (args.WantAnimation && args.Animation is null)
                {
                    args.Animation = new()
                    {
                        BeforeOpacity = 0f,
                        AfterOpacity = 1f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    };
                }

                if (args.WantPreviousAnimation && args.PreviousAnimation is null)
                {
                    args.PreviousAnimation = new()
                    {
                        BeforeOpacity = 1f,
                        AfterOpacity = 0f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    };
                }

                break;

            case NotifyCollectionChangedAction.Move
                when args is { WantAnimation: true, Animation: null }:
                // No default animation for now
                break;

            case NotifyCollectionChangedAction.Reset:
                if (args.WantAnimation && args.Animation is null)
                {
                    args.Animation = new()
                    {
                        BeforeOpacity = 0f,
                        AfterOpacity = 1f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    };
                }

                if (args.WantPreviousAnimation && args.PreviousAnimation is null)
                {
                    args.PreviousAnimation = new()
                    {
                        BeforeOpacity = 1f,
                        AfterOpacity = 0f,
                        OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(200)),
                    };
                }

                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnSetupItemResizeAnimation(SetupItemResizeAnimationEventArg args)
    {
        base.OnSetupItemResizeAnimation(args);
        if (args.UseDefault)
            args.Easing ??= new InOutCubic(TimeSpan.FromMilliseconds(200));
    }

    /// <summary>Raises the <see cref="DirectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDirectionChange(PropertyChangeEventArgs<LinearDirection> args) =>
        this.DirectionChange?.Invoke(args);

    /// <summary>Raises the <see cref="GravityChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnGravityChange(PropertyChangeEventArgs<Vector2> args) =>
        this.GravityChange?.Invoke(args);

    /// <summary>Raises the <see cref="AnchorOffsetRatioChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnAnchorOffsetRatioChange(PropertyChangeEventArgs<float> args) =>
        this.AnchorOffsetRatioChange?.Invoke(args);

    /// <summary>Raises the <see cref="StickToTerminalChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnStickToTerminalChange(PropertyChangeEventArgs<bool> args) =>
        this.StickToTerminalChange?.Invoke(args);

    /// <summary>Raises the <see cref="UseOffDirectionScrollChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnUseOffDirectionScrollChange(PropertyChangeEventArgs<bool> args) =>
        this.UseOffDirectionScrollChange?.Invoke(args);

    private void VerticalScrollBarOnScroll(ScrollBarControl.ScrollEventArgs args)
    {
        if (this.Collection?.Count is not > 0 || this.Parent is null)
            return;

        var mult = this.direction.IsDirectionConsistentWithIndex() ? 1f : -1f;
        switch (args.Action)
        {
            case ScrollBarControl.ScrollAction.LineDecrement:
                this.SmoothScrollByLines(new(0, mult * -1f));
                break;
            case ScrollBarControl.ScrollAction.LineIncrement:
                this.SmoothScrollByLines(new(0, mult * +1f));
                break;
            case ScrollBarControl.ScrollAction.MiddleButtonAutoScroll:
                this.ScrollByLines(new(0, (mult * args.RepeatCount) / 16f));
                break;

            case ScrollBarControl.ScrollAction.PageDecrement:
                this.SmoothScrollBy(new(0, mult * -this.Parent.MeasuredContentBox.Height));
                break;
            case ScrollBarControl.ScrollAction.PageIncrement:
                this.SmoothScrollBy(new(0, mult * +this.Parent.MeasuredContentBox.Height));
                break;

            case ScrollBarControl.ScrollAction.ThumbTrack when this.direction.IsVertical():
                this.scrollRequestedPosition = args.NewValue / ((ScrollBarControl)args.Sender).MaxValue;
                this.accumulatedScrollDelta.Y = 0;
                this.smoothScrollAmount.Y = 0;
                this.ScrollEasing.Reset();
                this.Parent.RequestMeasure();
                break;

            case ScrollBarControl.ScrollAction.ThumbTrack when this.direction.IsHorizontal():
                this.accumulatedScrollDelta.Y = 0;
                this.smoothScrollAmount.Y = 0;
                this.nonLimDimScroll = args.NewValue;
                this.ScrollEasing.Reset();
                this.Parent.RequestMeasure();
                break;
        }
    }

    private void HorizontalScrollBarOnScroll(ScrollBarControl.ScrollEventArgs args)
    {
        if (this.Collection?.Count is not > 0 || this.Parent is null)
            return;

        var mult = this.direction.IsDirectionConsistentWithIndex() ? 1f : -1f;
        switch (args.Action)
        {
            case ScrollBarControl.ScrollAction.LineDecrement:
                this.SmoothScrollByLines(new(mult * -1f, 0));
                break;
            case ScrollBarControl.ScrollAction.LineIncrement:
                this.SmoothScrollByLines(new(mult * +1f, 0));
                break;
            case ScrollBarControl.ScrollAction.MiddleButtonAutoScroll:
                this.ScrollByLines(new((mult * args.RepeatCount) / 16f, 0f));
                break;

            case ScrollBarControl.ScrollAction.PageDecrement:
                this.SmoothScrollBy(new(mult * -this.Parent.MeasuredContentBox.Width, 0));
                break;
            case ScrollBarControl.ScrollAction.PageIncrement:
                this.SmoothScrollBy(new(mult * +this.Parent.MeasuredContentBox.Width, 0));
                break;

            case ScrollBarControl.ScrollAction.ThumbTrack when this.direction.IsHorizontal():
                this.scrollRequestedPosition = args.NewValue / ((ScrollBarControl)args.Sender).MaxValue;
                this.accumulatedScrollDelta.X = 0;
                this.smoothScrollAmount.X = 0;
                this.ScrollEasing.Reset();
                this.Parent.RequestMeasure();
                break;

            case ScrollBarControl.ScrollAction.ThumbTrack when this.direction.IsVertical():
                this.accumulatedScrollDelta.X = 0;
                this.smoothScrollAmount.X = 0;
                this.nonLimDimScroll = args.NewValue;
                this.ScrollEasing.Reset();
                this.Parent.RequestMeasure();
                break;
        }
    }

    private void MeasureAroundAnchor(ref int veAnchorIndex, int itemCount, float limDim, float nonLimDim)
    {
        var ilibs = this.IsLowerIndexBeginningSide;

        // Resolve the item before the anchored item.
        var offset = this.VisibleEntries[veAnchorIndex].Offset;
        if (!ilibs)
            offset += this.GetExpandingDimensionLerped(this.VisibleEntries[veAnchorIndex]);

        var veMinItemIndex = veAnchorIndex - 1;
        for (this.FirstVisibleItem = this.AnchoredItem - 1;
             this.FirstVisibleItem >= 0;
             this.FirstVisibleItem--, veMinItemIndex--)
        {
            if (ilibs && offset < 0 && limDim < float.PositiveInfinity)
                break;
            if (!ilibs && offset > limDim)
                break;

            if (veMinItemIndex < 0 || this.visibleEntries[veMinItemIndex].Index != this.FirstVisibleItem)
            {
                veAnchorIndex++;
                this.visibleEntries.Insert(++veMinItemIndex, new(this.FirstVisibleItem));
                if (this.useResetAnimationOnVisibleEntryPopulationOnce)
                {
                    this.ResolveChangeAnimations(NotifyCollectionChangedAction.Reset, false, true, out _, out var na);
                    this.VisibleEntries[veMinItemIndex].Animation = na;
                }
            }

            ref var veItem = ref this.VisibleEntries[veMinItemIndex];
            this.MeasureVisibleEntry(ref veItem, nonLimDim, limDim);
            var veSize = this.GetExpandingDimensionLerped(veItem);

            if (ilibs)
                offset -= veSize;
            veItem.Offset = offset;
            veItem.OutsideViewport = false;
            if (!ilibs)
                offset += veSize;

            if (veItem.Removed)
                this.FirstVisibleItem++;
        }

        // Resolve the item after the anchored item.
        offset = this.VisibleEntries[veAnchorIndex].Offset;
        if (ilibs)
            offset += this.GetExpandingDimensionLerped(this.VisibleEntries[veAnchorIndex]);

        var veMaxItemIndex = veAnchorIndex + 1;
        for (this.LastVisibleItem = this.AnchoredItem + 1;
             this.LastVisibleItem < itemCount;
             this.LastVisibleItem++, veMaxItemIndex++)
        {
            if (!ilibs && offset < 0 && limDim < float.PositiveInfinity)
                break;
            if (ilibs && offset > limDim)
                break;

            if (veMaxItemIndex >= this.visibleEntries.Count ||
                this.visibleEntries[veMaxItemIndex].Index != this.LastVisibleItem)
            {
                this.visibleEntries.Insert(veMaxItemIndex, new(this.LastVisibleItem));
                if (this.useResetAnimationOnVisibleEntryPopulationOnce)
                {
                    this.ResolveChangeAnimations(NotifyCollectionChangedAction.Reset, false, true, out _, out var na);
                    this.VisibleEntries[veMaxItemIndex].Animation = na;
                }
            }

            ref var veItem = ref this.VisibleEntries[veMaxItemIndex];
            this.MeasureVisibleEntry(ref veItem, nonLimDim, limDim);
            var veSize = this.GetExpandingDimensionLerped(veItem);

            if (!ilibs)
                offset -= veSize;
            veItem.Offset = offset;
            veItem.OutsideViewport = false;
            if (ilibs)
                offset += veSize;

            if (veItem.Removed)
                this.LastVisibleItem--;
        }

        this.FirstVisibleItem++;
        this.LastVisibleItem--;

        veMinItemIndex++;
        veMaxItemIndex--;
        veMinItemIndex = Math.Max(veMinItemIndex, 0);
        veMaxItemIndex = Math.Min(veMaxItemIndex, this.visibleEntries.Count - 1);

        // Should not happen, but just in case it gets desynchronized.
        // This probably means change notification is not being given or handled properly.
        while (veMaxItemIndex >= 0 && this.visibleEntries[veMaxItemIndex].Index >= itemCount)
            veMaxItemIndex--;

        // Mark entries that are outside the visible range, to be deleted later.
        for (var i = this.visibleEntries.Count - 1; i > veMaxItemIndex; i--)
            this.VisibleEntries[i].OutsideViewport = true;

        for (var i = veMinItemIndex - 1; i >= 0; i--)
            this.VisibleEntries[i].OutsideViewport = true;
    }

    private void MeasureVisibleEntry(ref VisibleEntry vi, float nonLimDim, float limDim)
    {
        if (this.Parent is null || vi.Measured)
            return;
        vi.Measured = true;

        if (vi.SpannableType == RecyclerViewControl.InvalidSpannableType)
        {
            if (vi.Removed)
            {
                if (vi.UpdateAnimation())
                    this.Parent.RequestMeasure();
                return;
            }

            this.ResolveSpannableType(vi.Index, out var st, out var dt);
            vi.SpannableType = st;
            vi.DecorationType = dt;
            vi.Spannable = this.TakePlaceholder(vi.SpannableType, out vi.SpannableSlot, out vi.SpannableInnerId);
            if (vi.Spannable is not null)
                this.PopulateSpannable(vi.Index, vi.SpannableType, vi.Spannable);
            if (dt != RecyclerViewControl.InvalidSpannableType)
            {
                vi.Decoration = this.TakePlaceholder(
                    vi.DecorationType,
                    out vi.DecorationSlot,
                    out vi.DecorationInnerId);
                if (vi.Decoration is not null)
                {
                    this.PopulateSpannable(
                        vi.Index,
                        vi.DecorationType,
                        vi.Decoration);
                }
            }
        }

        if (vi.Spannable is { } m)
        {
            if (this.direction.IsVertical())
            {
                m.Options.PreferredSize = new(nonLimDim, float.PositiveInfinity);
                m.Options.VisibleSize = new(nonLimDim, limDim);
            }
            else
            {
                m.Options.PreferredSize = new(float.PositiveInfinity, nonLimDim);
                m.Options.VisibleSize = new(limDim, nonLimDim);
            }

            m.Options.RenderScale = this.Parent.Options.RenderScale;
            m.Renderer = this.Parent.Renderer;
            m.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.SpannableInnerId);
            m.RenderPassMeasure();
        }
        else
        {
            m = null;
        }

        if (vi.Previous is { } pm)
        {
            if (this.direction.IsVertical())
            {
                pm.Options.PreferredSize = new(nonLimDim, float.PositiveInfinity);
                pm.Options.VisibleSize = new(nonLimDim, limDim);
            }
            else
            {
                pm.Options.PreferredSize = new(float.PositiveInfinity, nonLimDim);
                pm.Options.VisibleSize = new(limDim, nonLimDim);
            }

            pm.Options.RenderScale = this.Parent.Options.RenderScale;
            pm.Renderer = this.Parent.Renderer;
            pm.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.SpannableInnerId);
            pm.RenderPassMeasure();
        }
        else
        {
            pm = null;
        }

        if ((m ?? pm) is not { } any)
            return;

        if (vi.UpdateAnimation())
            this.Parent.RequestMeasure();

        if (any.Boundary.IsValid
            && Math.Abs(
                this.GetExpandingDimension(vi.PreviousSize.RightBottom) -
                this.GetExpandingDimension(any.Boundary.RightBottom)) > 0.000001f)
        {
            if (vi.PreviousSize.IsValid)
            {
                if (vi.SizeEasing is null)
                {
                    this.ResolveSizingEasing(out var easing);
                    vi.SizeEasing = easing;
                    vi.SizeEasingFrom = vi.PreviousSize;
                }
                else
                {
                    vi.SizeEasingFrom = RectVector4.Lerp(vi.SizeEasingFrom, any.Boundary, (float)vi.SizeEasing.Value);
                    vi.SizeEasing.Restart();
                }

                vi.SizeEasing?.Update();
            }

            vi.PreviousSize = any.Boundary;
        }

        if (vi.Decoration is { } cdm)
        {
            var lerpedBoundary =
                vi.SizeEasing?.IsRunning is true
                    ? RectVector4.Lerp(vi.SizeEasingFrom, any.Boundary, (float)vi.SizeEasing.Value)
                    : any.Boundary;
            if (this.direction.IsVertical())
            {
                cdm.Options.PreferredSize = new(nonLimDim, lerpedBoundary.Height);
                cdm.Options.VisibleSize = new(nonLimDim, lerpedBoundary.Height);
            }
            else
            {
                cdm.Options.PreferredSize = new(lerpedBoundary.Width, nonLimDim);
                cdm.Options.VisibleSize = new(lerpedBoundary.Width, nonLimDim);
            }

            cdm.Options.RenderScale = this.Parent.Options.RenderScale;
            cdm.Renderer = this.Parent.Renderer;
            cdm.ImGuiGlobalId = this.Parent.GetGlobalIdFromInnerId(vi.DecorationInnerId);
            cdm.RenderPassMeasure();
        }
    }

    private void ClearVisibleEntries()
    {
        foreach (ref var v in this.VisibleEntries)
            this.ReturnVisibleEntry(ref v);

        this.visibleEntries.Clear();
        this.FirstVisibleItem = this.LastVisibleItem = this.AnchoredItem = -1;
        this.VisibleItemCount = 0;
        this.nonLimDimScroll = 0;
        this.accumulatedScrollDelta = this.smoothScrollAmount = Vector2.Zero;
        this.wasBeginningVisible = this.wasEndVisible = true;
    }

    private void ReturnVisibleEntry(ref VisibleEntry v)
    {
        this.ReturnCurrentVisibleEntry(ref v);
        this.ReturnPreviousVisibleEntry(ref v);
        this.ReturnDecorativeVisibleEntry(ref v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetExpandingDimension(Vector2 size2D) => this.direction.IsVertical() ? size2D.Y : size2D.X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetNonExpandingDimension(Vector2 size2D) => this.direction.IsVertical() ? size2D.X : size2D.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetDimensions(Vector2 size2D, out float expandingDimension, out float nonExpandingDimension)
    {
        if (this.direction.IsVertical())
        {
            expandingDimension = size2D.Y;
            nonExpandingDimension = size2D.X;
        }
        else
        {
            expandingDimension = size2D.X;
            nonExpandingDimension = size2D.Y;
        }
    }

    private float GetExpandingDimensionLerped(in VisibleEntry v)
    {
        var dim = this.GetExpandingDimension(
            v.Spannable?.Boundary.RightBottom ?? v.Previous?.Boundary.RightBottom ?? Vector2.Zero);
        if (v.SizeEasing?.IsDone is not false)
            return dim;

        var prevDim = this.GetExpandingDimension(v.SizeEasingFrom.RightBottom);
        return float.Lerp(prevDim, dim, (float)v.SizeEasing.Value);
    }

    private float GetNonExpandingDimensionLerped(in VisibleEntry v)
    {
        var dim = this.GetNonExpandingDimension(
            v.Spannable?.Boundary.RightBottom ?? v.Previous?.Boundary.RightBottom ?? Vector2.Zero);
        if (v.SizeEasing?.IsDone is not false)
            return dim;

        var prevDim = this.GetExpandingDimension(v.SizeEasingFrom.RightBottom);
        return float.Lerp(prevDim, dim, (float)v.SizeEasing.Value);
    }

    private void ReturnDecorativeVisibleEntry(ref VisibleEntry v)
    {
        if (v.Decoration is not null)
            this.ClearSpannable(v.DecorationType, v.Decoration);
        this.ReturnPlaceholder(v.DecorationType, v.Decoration, v.DecorationSlot, v.DecorationInnerId);
        v.Decoration = null;
        v.DecorationType = -1;
        v.DecorationSlot = v.DecorationInnerId = -1;
    }

    private void ReturnCurrentVisibleEntry(ref VisibleEntry v)
    {
        if (v.Spannable is not null)
            this.ClearSpannable(v.SpannableType, v.Spannable);
        this.ReturnPlaceholder(v.SpannableType, v.Spannable, v.SpannableSlot, v.SpannableInnerId);
        v.Spannable = null;
        v.Animation = null;
        v.SpannableSlot = v.SpannableInnerId = -1;
    }

    private void ReturnPreviousVisibleEntry(ref VisibleEntry v)
    {
        if (v.Previous is not null)
            this.ClearSpannable(v.PreviousType, v.Previous);
        this.ReturnPlaceholder(v.PreviousType, v.Previous, v.PreviousSlot, v.PreviousInnerId);
        v.Previous = null;
        v.PreviousAnimation = null;
        v.PreviousSlot = v.PreviousInnerId = -1;
    }

    private void CurrentToPreviousVisibleEntry(ref VisibleEntry v)
    {
        this.ReturnPreviousVisibleEntry(ref v);

        v.Previous = v.Spannable;
        v.PreviousType = v.SpannableType;
        v.PreviousAnimation = null;
        v.PreviousSlot = v.SpannableSlot;
        v.PreviousInnerId = v.SpannableInnerId;

        v.Spannable = null;
        v.SpannableType = RecyclerViewControl.InvalidSpannableType;
        v.Animation = null;
        v.SpannableSlot = v.SpannableInnerId = -1;
    }

    [DebuggerDisplay("#{Index} ({Spannable}): {Offset} / {Spannable}")]
    private struct VisibleEntry : IComparable<VisibleEntry>
    {
        public int Index;
        public float Offset;
        public bool Removed;
        public bool OutsideViewport;
        public bool Measured;

        public Easing? SizeEasing;
        public RectVector4 SizeEasingFrom;
        public RectVector4 PreviousSize;

        public SpannableAnimator? PreviousAnimation;
        public int PreviousType;
        public Spannable? Previous;
        public int PreviousSlot;
        public int PreviousInnerId;

        public SpannableAnimator? Animation;
        public int SpannableType;
        public Spannable? Spannable;
        public int SpannableSlot;
        public int SpannableInnerId;

        public int DecorationType;
        public Spannable? Decoration;
        public int DecorationSlot;
        public int DecorationInnerId;

        public VisibleEntry(int index)
        {
            this.Index = index;
            this.PreviousType = this.SpannableType = RecyclerViewControl.InvalidSpannableType;
            this.PreviousSize = RectVector4.InvertedExtrema;
        }

        public bool UpdateAnimation()
        {
            if (this.SizeEasing is not null)
            {
                this.SizeEasing.Update();
                if (this.SizeEasing.IsDone)
                    this.SizeEasing = null;
            }

            if (this.PreviousAnimation is not null)
            {
                if (this.Previous is not null)
                {
                    this.PreviousAnimation.Update(this.Previous);
                    if (this.PreviousAnimation.IsDone)
                        this.PreviousAnimation = null;
                }
            }

            if (this.Animation is not null)
            {
                if (this.Spannable is not null)
                {
                    this.Animation.Update(this.Spannable);
                    if (this.Animation.IsDone)
                        this.Animation = null;
                }
            }

            return this.SizeEasing is not null || this.PreviousAnimation is not null || this.Animation is not null;
        }

        public readonly int CompareTo(VisibleEntry other)
        {
            var r = this.Index.CompareTo(other.Index);
            if (r != 0)
                return r;
            return (this.Removed ? 0 : 1).CompareTo(this.Removed ? 0 : 1);
        }
    }
}

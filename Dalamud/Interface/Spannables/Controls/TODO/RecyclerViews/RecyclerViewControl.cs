using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.TODO.RecyclerViews;

#pragma warning disable SA1010

/// <summary>A recycler view control, which is a base for list views and grid views.</summary>
// TODO: should this control manage header area?
public abstract partial class RecyclerViewControl : ControlSpannable
{
    /// <summary>A sentinel value that indicates that the spannable type is invalid (could not be fetched.)</summary>
    public const int InvalidSpannableType = -1;

    private readonly Dictionary<int, List<ISpannable>> placeholders = new();
    private readonly List<int> availablePlaceholderSlotIndices = new();
    private readonly List<int> availablePlaceholderInnerIdIndices = new();

    private BaseLayoutManager? layoutManager;

    /// <summary>Initializes a new instance of the <see cref="RecyclerViewControl"/> class.</summary>
    protected RecyclerViewControl()
    {
    }

    /// <summary>Delegate for <see cref="NeedDecideSpannableType"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void NeedDecideSpannableTypeEventDelegate(NeedDecideSpannableTypeEventArg args);

    /// <summary>Delegate for <see cref="NeedMoreSpannables"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void NeedMoreSpannableEventDelegate(NeedMoreSpannableEventArg args);

    /// <summary>Delegate for <see cref="NeedPopulateSpannable"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void NeedPopulateSpannableEventDelegate(NeedPopulateSpannableEventArg args);

    /// <summary>Occurs when the type of spannable at a given index needs to be decided.</summary>
    public event NeedDecideSpannableTypeEventDelegate? NeedDecideSpannableType;

    /// <summary>Occurs when more spannables of the given the type are in need.</summary>
    /// <remarks>Call <see cref="AddPlaceholder"/> during this event.</remarks>
    public event NeedMoreSpannableEventDelegate? NeedMoreSpannables;

    /// <summary>Occurs when the spannable needs to be populated from the data.</summary>
    public event NeedPopulateSpannableEventDelegate? NeedPopulateSpannable;

    /// <summary>Occurs when the property <see cref="LayoutManager"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, BaseLayoutManager?>? LayoutManagerChange;

    /// <summary>Gets or sets the layout manager.</summary>
    public BaseLayoutManager? LayoutManager
    {
        get => this.layoutManager;
        set => this.HandlePropertyChange(
            nameof(this.LayoutManager),
            ref this.layoutManager,
            value,
            this.OnLayoutManagerChange);
    }

    /// <inheritdoc/>
    public override bool IsAnyAnimationRunning =>
        base.IsAnyAnimationRunning
        || this.layoutManager?.IsAnyAnimationRunning is true;

    /// <summary>Adds a placeholder for use.</summary>
    /// <param name="spannableType">Spannable type of the placeholder.</param>
    /// <param name="spannable">Placeholder spannable.</param>
    public void AddPlaceholder(int spannableType, ISpannable spannable)
    {
        if (spannable is null)
            throw new NullReferenceException();

        if (!this.placeholders.TryGetValue(spannableType, out var plist))
            this.placeholders.Add(spannableType, plist = []);
        plist.Add(spannable);
        spannable.SpannableChange += this.SpannableOnSpannableChange;

        this.availablePlaceholderSlotIndices.Add(this.AllSpannablesAvailableSlot++);
        this.availablePlaceholderInnerIdIndices.Add(this.InnerIdAvailableSlot++);
        this.AllSpannables.Add(null);
    }

    /// <summary>Notifies that the underlying collection has been changes.</summary>
    /// <param name="e">How the collection has changed.</param>
    public void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        this.layoutManager?.CollectionChanged(this.GetCollection(), e);

    /// <summary>Gets the underlying list.</summary>
    /// <returns>The list, or <c>null</c> if no list is bound.</returns>
    protected abstract ICollection? GetCollection();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (this.layoutManager as IProtectedLayoutManager)?.SetRecyclerView(null);
            foreach (var v in this.placeholders.Values)
            {
                foreach (var v2 in v)
                    v2.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(SpannableControlEventArgs args)
    {
        this.layoutManager?.HandleInteraction();
        base.OnHandleInteraction(args);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize) =>
        this.layoutManager?.MeasureContentBox(suggestedSize) ?? base.MeasureContentBox(suggestedSize);

    /// <inheritdoc/>
    protected override void OnUpdateTransformation(SpannableControlEventArgs args)
    {
        this.layoutManager?.UpdateTransformation();
        base.OnUpdateTransformation(args);
    }

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawEventArgs args)
    {
        this.layoutManager?.Draw(args);
        base.OnDraw(args);
    }

    /// <summary>Raises the <see cref="NeedDecideSpannableType"/> event.</summary>
    /// <param name="args">A <see cref="NeedDecideSpannableTypeEventArg"/> that contains the event data.</param>
    protected virtual void OnNeedDecideSpannableType(NeedDecideSpannableTypeEventArg args) =>
        this.NeedDecideSpannableType?.Invoke(args);

    /// <summary>Raises the <see cref="NeedMoreSpannables"/> event.</summary>
    /// <param name="args">A <see cref="NeedMoreSpannableEventArg"/> that contains the event data.</param>
    protected virtual void OnNeedMoreSpannables(NeedMoreSpannableEventArg args) =>
        this.NeedMoreSpannables?.Invoke(args);

    /// <summary>Raises the <see cref="NeedPopulateSpannable"/> event.</summary>
    /// <param name="args">A <see cref="NeedPopulateSpannableEventArg"/> that contains the event data.</param>
    protected virtual void OnNeedPopulateSpannable(NeedPopulateSpannableEventArg args) =>
        this.NeedPopulateSpannable?.Invoke(args);

    /// <summary>Raises the <see cref="LayoutManagerChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnLayoutManagerChange(PropertyChangeEventArgs<ControlSpannable, BaseLayoutManager?> args) =>
        this.LayoutManagerChange?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnMouseWheel(ControlMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (this.layoutManager?.CanScroll is not true || args.Handled || !this.IsMouseHoveredIncludingChildren)
            return;

        args.Handled = true;
        this.layoutManager.SmoothScrollBy(args.WheelDelta);
    }

    private void SpannableOnSpannableChange(ISpannable obj) => this.OnSpannableChange(this);

    public record NeedDecideSpannableTypeEventArg : SpannableControlEventArgs
    {
        /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
        public int Index { get; set; }

        /// <summary>Gets or sets the decided spannable type.</summary>
        /// <remarks>Assign to this property to assign a spannable type.</remarks>
        public int SpannableType { get; set; }
    }

    public record NeedMoreSpannableEventArg : SpannableControlEventArgs
    {
        /// <summary>Gets or sets the type of the spannable that needs to be populated.</summary>
        public int SpannableType { get; set; }
    }

    public record NeedPopulateSpannableEventArg : SpannableControlEventArgs
    {
        /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
        public int Index { get; set; }

        /// <summary>Gets or sets the decided spannable type from <see cref="NeedDecideSpannableType"/>.</summary>
        public int SpannableType { get; set; }

        /// <summary>Gets or sets the associated spannable.</summary>
        public ISpannable Spannable { get; set; } = null!;

        /// <summary>Gets or sets the associated spannable measurement.</summary>
        public ISpannableMeasurement Measurement { get; set; } = null!;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.RecyclerViews;

#pragma warning disable SA1010

/// <summary>A recycler view control, which is a base for list views and grid views.</summary>
// TODO: should this control manage header area?
public abstract partial class RecyclerViewControl : ControlSpannable
{
    /// <summary>A sentinel value that indicates that the spannable type is invalid (could not be fetched.)</summary>
    public const int InvalidSpannableType = -1;

    private readonly Dictionary<int, List<ISpannable>> placeholders = new();
    private readonly List<int> availablePlaceholderSlotIndices = [];
    private readonly List<int> availablePlaceholderInnerIdIndices = [];

    private BaseLayoutManager? layoutManager;
    private Vector2 autoScrollPerSecond;

    /// <summary>Initializes a new instance of the <see cref="RecyclerViewControl"/> class.</summary>
    protected RecyclerViewControl()
    {
        this.ClipChildren = true;
    }

    /// <summary>Delegate for <see cref="DecideSpannableType"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void DecideSpannableTypeEventDelegate(DecideSpannableTypeEventArg args);

    /// <summary>Delegate for <see cref="AddMoreSpannables"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void AddMoreSpannablesEventDelegate(AddMoreSpannablesEventArg args);

    /// <summary>Delegate for <see cref="PopulateSpannable"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void PopulateSpannableEventDelegate(PopulateSpannableEventArg args);

    /// <summary>Delegate for <see cref="PopulateSpannable"/>.</summary>
    /// <param name="args">The arguments.</param>
    public delegate void ClearSpannableEventDelegate(ClearSpannableEventArg args);

    /// <summary>Occurs when the type of spannable at a given index needs to be decided.</summary>
    public event DecideSpannableTypeEventDelegate? DecideSpannableType;

    /// <summary>Occurs when more spannables of the given the type are in need.</summary>
    /// <remarks>Call <see cref="AddPlaceholder"/> during this event.</remarks>
    public event AddMoreSpannablesEventDelegate? AddMoreSpannables;

    /// <summary>Occurs when the spannable should be populated from the data.</summary>
    public event PopulateSpannableEventDelegate? PopulateSpannable;

    /// <summary>Occurs when the spannable should be cleared.</summary>
    public event ClearSpannableEventDelegate? ClearSpannable;

    /// <summary>Occurs when the list has been scrolled.</summary>
    public event SpannableControlEventHandler? Scroll;

    /// <summary>Occurs when the property <see cref="LayoutManager"/> has been changed.</summary>
    public event PropertyChangeEventHandler<BaseLayoutManager?>? LayoutManagerChange;

    /// <summary>Occurs when the property <see cref="AutoScrollPerSecond"/> has been changed.</summary>
    public event PropertyChangeEventHandler<Vector2>? AutoScrollPerSecondChange;

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

    /// <summary>Gets or sets the auto scroll speed, in terms of amount of lines scrolled from a single detent in mouse
    /// wheels per every second.</summary>
    public Vector2 AutoScrollPerSecond
    {
        get => this.autoScrollPerSecond;
        set => this.HandlePropertyChange(
            nameof(this.AutoScrollPerSecond),
            ref this.autoScrollPerSecond,
            value,
            this.OnAutoScrollPerSecondChange);
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

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
    {
        if (this.layoutManager?.FindChildMeasurementAt(screenOffset) is { } r)
            return r;
        return base.FindChildMeasurementAt(screenOffset);
    }

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
    protected override void OnHandleInteraction(SpannableEventArgs args)
    {
        this.layoutManager?.HandleInteraction();
        base.OnHandleInteraction(args);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize) =>
        this.layoutManager?.MeasureContentBox(suggestedSize) ?? base.MeasureContentBox(suggestedSize);

    /// <inheritdoc/>
    protected override void OnUpdateTransformation(SpannableEventArgs args)
    {
        this.layoutManager?.UpdateTransformation();
        base.OnUpdateTransformation(args);
    }

    /// <inheritdoc/>
    protected override void OnDraw(SpannableDrawEventArgs args)
    {
        this.layoutManager?.Draw(args);
        base.OnDraw(args);
    }

    /// <summary>Raises the <see cref="DecideSpannableType"/> event.</summary>
    /// <param name="args">A <see cref="DecideSpannableTypeEventArg"/> that contains the event data.</param>
    protected virtual void OnDecideSpannableType(DecideSpannableTypeEventArg args) =>
        this.DecideSpannableType?.Invoke(args);

    /// <summary>Raises the <see cref="AddMoreSpannables"/> event.</summary>
    /// <param name="args">A <see cref="AddMoreSpannablesEventArg"/> that contains the event data.</param>
    protected virtual void OnAddMoreSpannables(AddMoreSpannablesEventArg args) =>
        this.AddMoreSpannables?.Invoke(args);

    /// <summary>Raises the <see cref="PopulateSpannable"/> event.</summary>
    /// <param name="args">A <see cref="PopulateSpannableEventArg"/> that contains the event data.</param>
    protected virtual void OnPopulateSpannable(PopulateSpannableEventArg args) =>
        this.PopulateSpannable?.Invoke(args);

    /// <summary>Raises the <see cref="ClearSpannable"/> event.</summary>
    /// <param name="args">A <see cref="ClearSpannableEventArg"/> that contains the event data.</param>
    protected virtual void OnClearSpannable(ClearSpannableEventArg args) => this.ClearSpannable?.Invoke(args);

    /// <summary>Raises the <see cref="Scroll"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnScroll(SpannableEventArgs args) => this.Scroll?.Invoke(args);

    /// <summary>Raises the <see cref="LayoutManagerChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnLayoutManagerChange(PropertyChangeEventArgs<BaseLayoutManager?> args) =>
        this.LayoutManagerChange?.Invoke(args);

    /// <summary>Raises the <see cref="AutoScrollPerSecondChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnAutoScrollPerSecondChange(PropertyChangeEventArgs<Vector2> args) =>
        this.AutoScrollPerSecondChange?.Invoke(args);

    /// <inheritdoc/>
    protected override void OnMouseWheel(SpannableMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (this.layoutManager?.CanScroll is not true || args.Handled || !this.IsMouseHoveredIncludingChildren)
            return;

        args.Handled = true;
        this.layoutManager.SmoothScrollBy(-args.WheelDelta);
    }

    private void SpannableOnSpannableChange(ISpannable obj) => this.OnSpannableChange(this);

    public record DecideSpannableTypeEventArg : SpannableEventArgs
    {
        /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
        public int Index { get; set; }

        /// <summary>Gets or sets the decided spannable type.</summary>
        /// <remarks>Assign to this property to assign a spannable type.</remarks>
        public int SpannableType { get; set; }

        /// <summary>Gets or sets the decided spannable type for decoration.</summary>
        /// <remarks>This is used as the background which does not get scrolled in non-main direction.
        /// Leave it as <see cref="RecyclerViewControl.InvalidSpannableType"/> to disable.</remarks>
        public int DecorationType { get; set; }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.Index = this.SpannableType = this.DecorationType = 0;
            return base.TryReset();
        }
    }

    public record AddMoreSpannablesEventArg : SpannableEventArgs
    {
        /// <summary>Gets or sets the type of the spannable that needs to be populated.</summary>
        public int SpannableType { get; set; }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.SpannableType = 0;
            return base.TryReset();
        }
    }

    public record PopulateSpannableEventArg : SpannableEventArgs
    {
        /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
        public int Index { get; set; }

        /// <summary>Gets or sets the decided spannable type from <see cref="DecideSpannableType"/>.</summary>
        public int SpannableType { get; set; }

        /// <summary>Gets or sets the associated spannable.</summary>
        public ISpannable Spannable { get; set; } = null!;

        /// <summary>Gets or sets the associated spannable measurement.</summary>
        public ISpannableMeasurement Measurement { get; set; } = null!;

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.Index = this.SpannableType = 0;
            this.Spannable = null!;
            this.Measurement = null!;
            return base.TryReset();
        }
    }

    public record ClearSpannableEventArg : SpannableEventArgs
    {
        /// <summary>Gets or sets the decided spannable type from <see cref="DecideSpannableType"/>.</summary>
        public int SpannableType { get; set; }

        /// <summary>Gets or sets the associated spannable.</summary>
        public ISpannable Spannable { get; set; } = null!;

        /// <summary>Gets or sets the associated spannable measurement.</summary>
        public ISpannableMeasurement Measurement { get; set; } = null!;

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.SpannableType = 0;
            this.Spannable = null!;
            this.Measurement = null!;
            return base.TryReset();
        }
    }
}

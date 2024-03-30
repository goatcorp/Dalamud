using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.TODO;

/// <summary>A scrollbar that may either be scrolled vertically or horizontally.</summary>
// TODO: can this also do Slider?
public class ScrollBarControl : ControlSpannable
{
    /// <summary>The default thickness of a scroll bar.</summary>
    public const float DefaultThickness = 16f;

    /// <summary>The default length of a scroll bar.</summary>
    public const float DefaultLength = 64f;

    /// <summary>The default length of a button in the scroll bar.</summary>
    public const float DefaultButtonLength = DefaultThickness;

    private const int ChildSpannableCount = 3;

    private const int MeasurementIndexDecreaseButton = 0;
    private const int MeasurementIndexIncreaseButton = 1;
    private const int MeasurementIndexThumb = 2;

    private readonly int slotIndexDecreaseButton;
    private readonly int slotIndexIncreaseButton;
    private readonly int slotIndexThumb;

    private readonly ISpannableMeasurement?[] childMeasurements = new ISpannableMeasurement?[ChildSpannableCount];
    private readonly int[] childInnerIds = new int[ChildSpannableCount];

    private bool autoValueUpdate;
    private float value;
    private float minValue;
    private float maxValue = 1f;
    private float lineSize = 1 / 32f;
    private float pageSize = 1 / 8f;
    private LinearDirection direction;

    private float barOffset;
    private float barSize;
    private float thumbOffset;
    private float thumbSize;

    private ScrollAction currentMouseDownScrollAction;
    private Vector2 currentMouseDownRange;
    private DateTime repeatNext;

    /// <summary>Initializes a new instance of the <see cref="ScrollBarControl"/> class.</summary>
    public ScrollBarControl()
    {
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);

        this.slotIndexDecreaseButton = this.AllSpannablesAvailableSlot++;
        this.slotIndexIncreaseButton = this.AllSpannablesAvailableSlot++;
        this.slotIndexThumb = this.AllSpannablesAvailableSlot++;

        for (var i = 0; i < ChildSpannableCount; i++)
            this.childInnerIds[i] = this.InnerIdAvailableSlot++;

        this.CaptureMouseOnMouseDown = true;

        this.NormalBackground = new ShapePattern
        {
            Color = 0xFF2C2C2C,
            Type = ShapePattern.Shape.RectFilled,
        };

        this.DecreaseButton = new ButtonControl
        {
            Text = FontAwesomeIcon.CaretUp.ToString(),
            TextStyle = new()
            {
                Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
            },
            Padding = BorderVector4.Zero,
            Alignment = new(0.5f), 
            Focusable = false,
        };

        this.IncreaseButton = new ButtonControl
        {
            Text = FontAwesomeIcon.CaretDown.ToString(),
            TextStyle = new()
            {
                Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
            },
            Focusable = false,
        };

        this.Thumb = new ShapePattern
        {
            Color = 0xFF9F9F9F,
            Type = ShapePattern.Shape.RectFilled,
            Rounding = (DefaultThickness / 2f) - 1f,
            Margin = new(2f),
        };

        this.NormalBackground.SpannableChange += this.ChildOnSpannableChange;
        this.DecreaseButton.SpannableChange += this.ChildOnSpannableChange;
        this.IncreaseButton.SpannableChange += this.ChildOnSpannableChange;
        this.Thumb.SpannableChange += this.ChildOnSpannableChange;
    }

    /// <summary>Delegate for <see cref="Scroll"/>.</summary>
    /// <param name="args">The event arguments.</param>
    public delegate void ScrollEventHandler(ScrollEventArgs args);

    /// <summary>Occurs when the property <see cref="AutoValueUpdate"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? AutoValueUpdateChange;

    /// <summary>Occurs when the property <see cref="Value"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ValueChange;

    /// <summary>Occurs when the property <see cref="MinValue"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? MinValueChange;

    /// <summary>Occurs when the property <see cref="MaxValue"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? MaxValueChange;

    /// <summary>Occurs when the property <see cref="LineSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? LineSizeChange;

    /// <summary>Occurs when the property <see cref="PageSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? PageSizeChange;

    /// <summary>Occurs when the property <see cref="Direction"/> is changing.</summary>
    public event PropertyChangeEventHandler<LinearDirection>? DirectionChange;

    /// <summary>Occurs when the property <see cref="DecreaseButton"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? DecreaseButtonChange;

    /// <summary>Occurs when the property <see cref="IncreaseButton"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? IncreaseButtonChange;

    /// <summary>Occurs when the property <see cref="Thumb"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? ThumbChange;

    /// <summary>Occurs when the user scrolls on the bar.</summary>
    public event ScrollEventHandler? Scroll;

    /// <summary>Possible actions that could have resulted in a scroll.</summary>
    public enum ScrollAction
    {
        /// <summary>Nothing happened.</summary>
        None,

        /// <summary>The user clicked on the decrease button.</summary>
        LineDecrement,

        /// <summary>The user clicked on the increase button.</summary>
        LineIncrement,

        /// <summary>The user clicked on the bar before the thumb.</summary>
        PageDecrement,

        /// <summary>The user clicked on the bar after the thumb.</summary>
        PageIncrement,

        /// <summary>The user dragged the thumb.</summary>
        ThumbTrack,
    }

    /// <summary>Gets or sets a value indicating whether to update <see cref="Value"/> automatically according to user
    /// interaction.</summary>
    public bool AutoValueUpdate
    {
        get => this.autoValueUpdate;
        set => this.HandlePropertyChange(
            nameof(this.AutoValueUpdate),
            ref this.autoValueUpdate,
            value,
            this.OnAutoValueUpdateChange);
    }

    /// <summary>Gets or sets the value.</summary>
    public float Value
    {
        get => this.value;
        set => this.HandlePropertyChange(nameof(this.Value), ref this.value, value, this.OnValueChange);
    }

    /// <summary>Gets or sets the minimum value.</summary>
    public float MinValue
    {
        get => this.minValue;
        set => this.HandlePropertyChange(nameof(this.MinValue), ref this.minValue, value, this.OnMinValueChange);
    }

    /// <summary>Gets or sets the maximum value.</summary>
    public float MaxValue
    {
        get => this.maxValue;
        set => this.HandlePropertyChange(nameof(this.MaxValue), ref this.maxValue, value, this.OnMaxValueChange);
    }

    /// <summary>Gets or sets the line size, proportional to the available range.</summary>
    /// <value>A value ranging from 0 to 1. Values outside will be clamped.</value>
    /// <remarks>This is the scroll amount for increase and decrease buttons.</remarks>
    public float LineSize
    {
        get => this.lineSize;
        set => this.HandlePropertyChange(nameof(this.LineSize), ref this.lineSize, value, this.OnLineSizeChange);
    }

    /// <summary>Gets or sets the page size, proportional to the available range.</summary>
    /// <value>A value ranging from 0 to 1. Values outside will be clamped. Note that <c>1</c> will effectively disable
    /// scrolling.</value>
    /// <remarks>This is the scroll amount for clicking on the bar that is not on the thumb, and this is the size of the
    /// thumb proportional to the bar size.</remarks>
    public float PageSize
    {
        get => this.pageSize;
        set => this.HandlePropertyChange(nameof(this.PageSize), ref this.pageSize, value, this.OnPageSizeChange);
    }

    /// <summary>Gets or sets the increasing direction of the scroll bar.</summary>
    public LinearDirection Direction
    {
        get => this.direction;
        set => this.HandlePropertyChange(nameof(this.Direction), ref this.direction, value, this.OnDirectionChanged);
    }

    /// <summary>Gets or sets the decrease button.</summary>
    public ISpannable? DecreaseButton
    {
        get => this.AllSpannables[this.slotIndexDecreaseButton];
        set => this.HandlePropertyChange(
            nameof(this.DecreaseButton),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexDecreaseButton],
            value,
            this.OnDecreaseButtonChanged);
    }

    /// <summary>Gets or sets the increase button.</summary>
    public ISpannable? IncreaseButton
    {
        get => this.AllSpannables[this.slotIndexIncreaseButton];
        set => this.HandlePropertyChange(
            nameof(this.IncreaseButton),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexIncreaseButton],
            value,
            this.OnIncreaseButtonChanged);
    }

    /// <summary>Gets or sets the thumb.</summary>
    public ISpannable? Thumb
    {
        get => this.AllSpannables[this.slotIndexThumb];
        set => this.HandlePropertyChange(
            nameof(this.Thumb),
            ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexThumb],
            value,
            this.OnThumbChanged);
    }

    private Span<ISpannable?> ChildSpannables =>
        CollectionsMarshal.AsSpan(this.AllSpannables).Slice(this.slotIndexDecreaseButton, ChildSpannableCount);

    private float EffectiveRange => Math.Max(this.maxValue - this.MinValue, 0f);

    private float NormalizedValue =>
        this.EffectiveRange > 0f
            ? Math.Clamp((this.Value - this.MinValue) / this.EffectiveRange, 0f, 1f)
            : 0f;

    /// <inheritdoc/>
    public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
    {
        foreach (var m in this.childMeasurements)
        {
            if (m is null)
                continue;
            if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                return m;
        }

        return base.FindChildMeasurementAt(screenOffset);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(SpannableEventArgs args)
    {
        base.OnHandleInteraction(args);
        foreach (var m in this.childMeasurements)
            m?.HandleInteraction();
        this.HandleScrollActionTick(false);
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize)
    {
        var children = this.ChildSpannables;
        var measurements = this.childMeasurements.AsSpan();
        for (var i = 0; i < ChildSpannableCount; i++)
        {
            if (!ReferenceEquals(measurements[i]?.Spannable, children[i]))
            {
                measurements[i]?.Spannable?.ReturnMeasurement(measurements[i]);
                measurements[i] = null;

                if (children[i] is not null)
                    measurements[i] = children[i].RentMeasurement(this.Renderer);
            }

            if (measurements[i] is not { } m)
                continue;
            m.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.childInnerIds[i]);
            m.RenderScale = this.EffectiveRenderScale;
        }

        if (this.direction.IsVertical())
        {
            if (suggestedSize.X >= float.PositiveInfinity)
                suggestedSize.X = DefaultThickness;
            if (suggestedSize.Y >= float.PositiveInfinity)
                suggestedSize.Y = DefaultLength;

            this.barSize = suggestedSize.Y;
            if (measurements[MeasurementIndexDecreaseButton] is { } decbut)
            {
                decbut.Options.VisibleSize = decbut.Options.Size = suggestedSize with { Y = DefaultButtonLength };
                decbut.Measure();
                this.barSize -= decbut.Boundary.Bottom;
            }

            if (measurements[MeasurementIndexIncreaseButton] is { } incbut)
            {
                incbut.Options.VisibleSize = incbut.Options.Size = suggestedSize with { Y = DefaultButtonLength };
                incbut.Measure();
                this.barSize -= incbut.Boundary.Bottom;
            }

            if (this.EffectiveRange > 0)
            {
                this.thumbSize = this.barSize * Math.Clamp(this.pageSize, 0, 1);
                this.thumbOffset = (this.barSize - this.thumbSize) * this.NormalizedValue;
                this.thumbOffset =
                    this.direction == LinearDirection.TopToBottom
                        ? this.NormalizedValue
                        : 1 - this.NormalizedValue;
                this.thumbOffset *= this.barSize - this.thumbSize;
            }
            else
            {
                this.thumbOffset = this.thumbSize = 0f;
            }

            if (measurements[MeasurementIndexThumb] is { } thumb && this.EffectiveRange > 0)
            {
                thumb.Options.VisibleSize = thumb.Options.Size = suggestedSize with { Y = this.thumbSize };
                thumb.Measure();
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        return new(Vector2.Zero, suggestedSize);
    }

    /// <inheritdoc/>
    protected override void OnUpdateTransformation(SpannableEventArgs args)
    {
        var measurements = this.childMeasurements.AsSpan();

        if (this.direction.IsVertical())
        {
            this.barOffset = 0f;
            if (measurements[MeasurementIndexDecreaseButton] is { } decbut)
            {
                decbut.UpdateTransformation(
                    Matrix4x4.CreateTranslation(
                        this.MeasuredContentBox.Left,
                        this.direction == LinearDirection.TopToBottom
                            ? this.MeasuredContentBox.Top
                            : this.MeasuredContentBox.Bottom - decbut.Boundary.Bottom,
                        0),
                    this.FullTransformation);
                if (this.direction == LinearDirection.TopToBottom)
                    this.barOffset = decbut.Boundary.Bottom;
            }

            if (measurements[MeasurementIndexIncreaseButton] is { } incbut)
            {
                incbut.UpdateTransformation(
                    Matrix4x4.CreateTranslation(
                        this.MeasuredContentBox.Left,
                        this.direction == LinearDirection.TopToBottom
                            ? this.MeasuredContentBox.Bottom - incbut.Boundary.Bottom
                            : this.MeasuredContentBox.Top,
                        0),
                    this.FullTransformation);
                if (this.direction == LinearDirection.BottomToTop)
                    this.barOffset = incbut.Boundary.Bottom;
            }

            if (measurements[MeasurementIndexThumb] is { } thumb && this.EffectiveRange > 0)
            {
                thumb.UpdateTransformation(
                    Matrix4x4.CreateTranslation(
                        this.MeasuredContentBox.Left,
                        this.MeasuredContentBox.Top + this.barOffset + this.thumbOffset,
                        0),
                    this.FullTransformation);
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        base.OnUpdateTransformation(args);
    }

    /// <inheritdoc/>
    protected override void OnDraw(SpannableDrawEventArgs args)
    {
        base.OnDraw(args);
        var measurements = this.childMeasurements.AsSpan();
        measurements[MeasurementIndexIncreaseButton]?.Draw(args.DrawListPtr);
        measurements[MeasurementIndexDecreaseButton]?.Draw(args.DrawListPtr);
        if (this.EffectiveRange > 0f)
            measurements[MeasurementIndexThumb]?.Draw(args.DrawListPtr);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(SpannableMouseEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.Handled || this.EffectiveRange <= 0f)
            return;

        var sp = this.FindChildMeasurementAt(ImGui.GetMousePos())?.Spannable;
        if (ReferenceEquals(this.DecreaseButton, sp))
        {
            this.currentMouseDownScrollAction = ScrollAction.LineDecrement;
            this.HandleScrollActionTick(true);
            args.Handled = true;
        }
        else if (ReferenceEquals(this.IncreaseButton, sp))
        {
            this.currentMouseDownScrollAction = ScrollAction.LineIncrement;
            this.HandleScrollActionTick(true);
            args.Handled = true;
        }
        else if (ReferenceEquals(this.Thumb, sp))
        {
            this.currentMouseDownScrollAction = ScrollAction.ThumbTrack;
            if (this.direction.IsHorizontal())
            {
                this.currentMouseDownRange =
                    new(
                        this.barOffset + (args.LocalLocation.X - this.thumbOffset),
                        (this.barOffset + this.barSize + (args.LocalLocation.X - this.thumbOffset)) - this.thumbSize);
            }
            else
            {
                var r = this.barSize - this.thumbSize;
                var nv = this.NormalizedValue;
                if (!this.direction.IsDirectionConsistentWithIndex())
                    nv = 1 - nv;
                var start = args.LocalLocation.Y - (r * nv);
                var end = args.LocalLocation.Y + (r * (1 - nv));
                this.currentMouseDownRange = new(start, end);
            }

            this.HandleScrollActionTick(true);
            args.Handled = true;
        }
        else if (this.IsMouseHoveredIncludingChildren)
        {
            var downOffsetV2 = (args.LocalLocation / this.Scale) - this.MeasuredContentBox.LeftTop;
            var downOffset = this.direction.IsVertical() ? downOffsetV2.Y : downOffsetV2.X;

            if (downOffset < this.thumbOffset)
            {
                this.currentMouseDownScrollAction =
                    this.direction.IsDirectionConsistentWithIndex()
                        ? ScrollAction.PageDecrement
                        : ScrollAction.PageIncrement;
                this.HandleScrollActionTick(true);
                args.Handled = true;
            }
            else if (downOffset >= this.thumbOffset + this.thumbSize)
            {
                this.currentMouseDownScrollAction =
                    this.direction.IsDirectionConsistentWithIndex()
                        ? ScrollAction.PageIncrement
                        : ScrollAction.PageDecrement;
                this.HandleScrollActionTick(true);
                args.Handled = true;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(SpannableMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (args.Handled || !this.IsMouseHoveredIncludingChildren)
            return;
        args.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(SpannableMouseEventArgs args)
    {
        base.OnMouseUp(args);
        if (this.currentMouseDownScrollAction == ScrollAction.None)
            return;
        args.Handled = true;
        this.currentMouseDownScrollAction = ScrollAction.None;
    }

    /// <summary>Raises the <see cref="AutoValueUpdateChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnAutoValueUpdateChange(PropertyChangeEventArgs<bool> args) =>
        this.AutoValueUpdateChange?.Invoke(args);

    /// <summary>Raises the <see cref="ValueChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnValueChange(PropertyChangeEventArgs<float> args) =>
        this.ValueChange?.Invoke(args);

    /// <summary>Raises the <see cref="MinValueChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMinValueChange(PropertyChangeEventArgs<float> args) =>
        this.MinValueChange?.Invoke(args);

    /// <summary>Raises the <see cref="MaxValueChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMaxValueChange(PropertyChangeEventArgs<float> args) =>
        this.MaxValueChange?.Invoke(args);

    /// <summary>Raises the <see cref="LineSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnLineSizeChange(PropertyChangeEventArgs<float> args) =>
        this.LineSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="PageSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnPageSizeChange(PropertyChangeEventArgs<float> args) =>
        this.PageSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="DirectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDirectionChanged(PropertyChangeEventArgs<LinearDirection> args) =>
        this.DirectionChange?.Invoke(args);

    /// <summary>Raises the <see cref="DecreaseButtonChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDecreaseButtonChanged(PropertyChangeEventArgs<ISpannable?> args)
    {
        this.DecreaseButtonChange?.Invoke(args);
        if (args.State == PropertyChangeState.After)
        {
            if (args.PreviousValue is ControlSpannable pcs)
            {
                pcs.MousePressLong -= this.IncDecMousePressLongAndRepeat;
                pcs.MousePressRepeat -= this.IncDecMousePressLongAndRepeat;
                pcs.MouseDown -= this.IncDecMousePressLongAndRepeat;
            }

            if (args.NewValue is ControlSpannable ncs)
            {
                ncs.MousePressLong += this.IncDecMousePressLongAndRepeat;
                ncs.MousePressRepeat += this.IncDecMousePressLongAndRepeat;
                ncs.MouseDown += this.IncDecMousePressLongAndRepeat;
            }
        }
    }

    /// <summary>Raises the <see cref="IncreaseButtonChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIncreaseButtonChanged(PropertyChangeEventArgs<ISpannable?> args)
    {
        this.IncreaseButtonChange?.Invoke(args);
        if (args.State == PropertyChangeState.After)
        {
            if (args.PreviousValue is ControlSpannable pcs)
            {
                pcs.MousePressLong -= this.IncDecMousePressLongAndRepeat;
                pcs.MousePressRepeat -= this.IncDecMousePressLongAndRepeat;
                pcs.MouseDown -= this.IncDecMousePressLongAndRepeat;
            }

            if (args.NewValue is ControlSpannable ncs)
            {
                ncs.MousePressLong += this.IncDecMousePressLongAndRepeat;
                ncs.MousePressRepeat += this.IncDecMousePressLongAndRepeat;
                ncs.MouseDown += this.IncDecMousePressLongAndRepeat;
            }
        }
    }

    /// <summary>Raises the <see cref="ThumbChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnThumbChanged(PropertyChangeEventArgs<ISpannable?> args) =>
        this.ThumbChange?.Invoke(args);

    /// <summary>Raises the <see cref="Scroll"/> event.</summary>
    /// <param name="args">A <see cref="ScrollEventArgs"/> that contains the event data.</param>
    protected virtual void OnScroll(ScrollEventArgs args) => this.Scroll?.Invoke(args);

    private void IncDecMousePressLongAndRepeat(SpannableMouseEventArgs args)
    {
        if (args.Button != ImGuiMouseButton.Left)
            return;

        if (ReferenceEquals(args.Sender, this.DecreaseButton))
        {
            var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
            e.Action = ScrollAction.LineDecrement;
            e.RepeatCount = args.Clicks;
            e.OldValue = this.value;
            e.UnboundDelta = -this.lineSize * this.EffectiveRange;
            e.NewValue = Math.Clamp(this.value + (e.RepeatCount * e.UnboundDelta), this.minValue, this.maxValue);
            this.OnScroll(e);
            if (this.autoValueUpdate)
                this.Value = e.NewValue;
            SpannableEventArgsPool.Return(e);
        }
        else if (ReferenceEquals(args.Sender, this.IncreaseButton))
        {
            var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
            e.Action = ScrollAction.LineIncrement;
            e.RepeatCount = args.Clicks;
            e.OldValue = this.value;
            e.UnboundDelta = +this.lineSize * this.EffectiveRange;
            e.NewValue = Math.Clamp(this.value + (e.RepeatCount * e.UnboundDelta), this.minValue, this.maxValue);
            this.OnScroll(e);
            if (this.autoValueUpdate)
                this.Value = e.NewValue;
            SpannableEventArgsPool.Return(e);
        }
    }

    private void HandleScrollActionTick(bool first)
    {
        if (this.currentMouseDownScrollAction == ScrollAction.None)
            return;

        var repeatCount = 1;
        if (first)
        {
            this.repeatNext = DateTime.Now + WindowsUiConfigHelper.GetKeyboardRepeatInitialDelay();
        }
        else if (this.currentMouseDownScrollAction != ScrollAction.ThumbTrack)
        {
            var d = DateTime.Now - this.repeatNext;
            if (d < TimeSpan.Zero)
                return;

            var repeatInterval = WindowsUiConfigHelper.GetKeyboardRepeatInterval();
            repeatCount = 1 + (int)MathF.Floor((float)(d / repeatInterval));
            this.repeatNext += repeatInterval * repeatCount;
        }

        float unboundDelta;
        switch (this.currentMouseDownScrollAction)
        {
            // Following two cases will be called if IncrementButton/DecrementButtons are not ControlSpannables.
            case ScrollAction.LineDecrement:
                unboundDelta = -this.lineSize * this.EffectiveRange;
                break;
            case ScrollAction.LineIncrement:
                unboundDelta = +this.lineSize * this.EffectiveRange;
                break;

            // Following two cases will be called when the bar is being held down.
            case ScrollAction.PageDecrement:
                unboundDelta = -this.pageSize * this.EffectiveRange;
                break;
            case ScrollAction.PageIncrement:
                unboundDelta = +this.pageSize * this.EffectiveRange;
                break;

            // This case is the one about dragging.
            case ScrollAction.ThumbTrack:
                if (this.direction.IsVertical())
                {
                    var t = this.PointToClient(ImGui.GetMousePos()).Y;
                    t = (t - this.currentMouseDownRange.X) /
                        (this.currentMouseDownRange.Y - this.currentMouseDownRange.X);
                    if (!this.direction.IsDirectionConsistentWithIndex())
                        t = 1 - t;
                    t *= this.maxValue - this.minValue;
                    t += this.minValue;
                    unboundDelta = t - this.value;
                    if (MathF.Abs(unboundDelta) < 0.000001f)
                        return;
                }
                else
                {
                    throw new NotImplementedException();
                }

                break;
            case ScrollAction.None:
            default:
                return;
        }

        var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
        e.Sender = this;
        e.Action = this.currentMouseDownScrollAction;
        e.RepeatCount = repeatCount;
        e.OldValue = this.value;
        e.UnboundDelta = unboundDelta;
        e.NewValue = Math.Clamp(this.value + (unboundDelta * repeatCount), this.minValue, this.maxValue);
        this.OnScroll(e);
        if (this.autoValueUpdate)
            this.Value = e.NewValue;
        SpannableEventArgsPool.Return(e);
        // this.currentMouseDownScrollAction = ScrollAction.None;
    }

    private void ChildOnSpannableChange(ISpannable obj) => this.OnSpannableChange(this);

    /// <summary>Event arguments for <see cref="Scroll"/> event.</summary>
    public record ScrollEventArgs : SpannableEventArgs
    {
        /// <summary>Gets or sets the action that resulted in a scrolling.</summary>
        public ScrollAction Action { get; set; }

        /// <summary>Gets or sets the old value.</summary>
        public float OldValue { get; set; }

        /// <summary>Gets or sets the new value.</summary>
        /// <remarks>This value will not be applied to <see cref="ScrollBarControl.Value"/> if
        /// <see cref="ScrollBarControl.AutoValueUpdate"/> is not set.</remarks>
        public float NewValue { get; set; }

        /// <summary>Gets or sets the delta of <see cref="ScrollBarControl.Value"/> if no boundaries are to be checked.
        /// </summary>
        public float UnboundDelta { get; set; }

        /// <summary>Gets or sets the number of repeats that must be multiplied to <see cref="UnboundDelta"/>.</summary>
        public int RepeatCount { get; set; }
    }
}

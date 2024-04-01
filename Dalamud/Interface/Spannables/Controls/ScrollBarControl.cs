using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls;

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

    private readonly int[] childInnerIds = new int[ChildSpannableCount];

    private bool autoValueUpdate;
    private float value;
    private float minValue;
    private float maxValue = 1f;
    private float lineSizeProportion = 1 / 32f;
    private float pageSizeProportion = 1 / 8f;
    private float minThumbSize = DefaultButtonLength;
    private LinearDirection direction;

    private float barOffset;
    private float barSize;
    private float thumbOffset;
    private float thumbSize;

    private ScrollAction currentMouseDownScrollAction;
    private Vector2 currentMouseDownRange;
    private Vector2 currentMouseThumbLocation;
    private long repeatNext;

    private Vector2 mmbScrollOrigin = new(float.NaN);
    private float mmbAutoScrollPerSecond;

    /// <summary>Initializes a new instance of the <see cref="ScrollBarControl"/> class.</summary>
    public ScrollBarControl()
    {
        this.slotIndexDecreaseButton = this.AllSpannablesAvailableSlot++;
        this.slotIndexIncreaseButton = this.AllSpannablesAvailableSlot++;
        this.slotIndexThumb = this.AllSpannablesAvailableSlot++;

        for (var i = 0; i < ChildSpannableCount; i++)
        {
            this.AllSpannables.Add(null);
            this.childInnerIds[i] = this.InnerIdAvailableSlot++;
        }

        this.CaptureMouseOnMouseDown = true;

        this.NormalBackground = new ShapePattern.Template(
            new()
            {
                Color = 0xFF2C2C2C,
                Shape = ShapePattern.Shape.RectFilled,
            });

        this.DecreaseButton = new ButtonControl
        {
            Text = FontAwesomeIcon.CaretUp.ToIconString(),
            TextStyle = new()
            {
                Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
                FontSize = 12f,
                ForeColor = 0xFF9F9F9F,
            },
            NormalBackground = null,
            Size = new(MatchParent, MatchParent),
            Padding = BorderVector4.Zero,
            Alignment = new(0.6f, 0.5f),
            Focusable = false,
        };

        this.IncreaseButton = new ButtonControl
        {
            Text = FontAwesomeIcon.CaretDown.ToIconString(),
            TextStyle = new()
            {
                Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
                FontSize = 12f,
                ForeColor = 0xFF9F9F9F,
            },
            NormalBackground = null,
            Size = new(MatchParent, MatchParent),
            Padding = BorderVector4.Zero,
            Alignment = new(0.6f, 0.5f),
            Focusable = false,
        };

        this.Thumb = new ShapePattern(
            new()
            {
                Color = 0xFF9F9F9F,
                Shape = ShapePattern.Shape.RectFilled,
                Rounding = (DefaultThickness / 2f) - 1f,
                Margin = new(2f),
            });

        this.DecreaseButton.PropertyChange += this.ChildOnPropertyChange;
        this.IncreaseButton.PropertyChange += this.ChildOnPropertyChange;
        this.Thumb.PropertyChange += this.ChildOnPropertyChange;
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

    /// <summary>Occurs when the property <see cref="LineSizeProportion"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? LineSizeProportionChange;

    /// <summary>Occurs when the property <see cref="PageSizeProportion"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? PageSizeProportionChange;

    /// <summary>Occurs when the property <see cref="MinThumbSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? MinThumbSizeChange;

    /// <summary>Occurs when the property <see cref="Direction"/> is changing.</summary>
    public event PropertyChangeEventHandler<LinearDirection>? DirectionChange;

    /// <summary>Occurs when the property <see cref="DecreaseButton"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? DecreaseButtonChange;

    /// <summary>Occurs when the property <see cref="IncreaseButton"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? IncreaseButtonChange;

    /// <summary>Occurs when the property <see cref="Thumb"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? ThumbChange;

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
        
        /// <summary>The user dragged after holding the mouse middle button down.</summary>
        MiddleButtonAutoScroll,
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
            this.autoValueUpdate == value,
            this.OnAutoValueUpdateChange);
    }

    /// <summary>Gets or sets the value.</summary>
    public float Value
    {
        get => this.value;
        set => this.HandlePropertyChange(
            nameof(this.Value),
            ref this.value,
            value,
            this.value - value == 0f,
            this.OnValueChange);
    }

    /// <summary>Gets or sets the minimum value.</summary>
    public float MinValue
    {
        get => this.minValue;
        set => this.HandlePropertyChange(
            nameof(this.MinValue),
            ref this.minValue,
            value,
            this.minValue - value == 0f,
            this.OnMinValueChange);
    }

    /// <summary>Gets or sets the maximum value.</summary>
    public float MaxValue
    {
        get => this.maxValue;
        set => this.HandlePropertyChange(
            nameof(this.MaxValue),
            ref this.maxValue,
            value,
            this.maxValue - value == 0f,
            this.OnMaxValueChange);
    }

    /// <summary>Gets or sets the line size, proportional to the available range.</summary>
    /// <value>A value ranging from 0 to 1. Values outside will be clamped.</value>
    /// <remarks>This is the scroll amount for increase and decrease buttons.</remarks>
    public float LineSizeProportion
    {
        get => this.lineSizeProportion;
        set => this.HandlePropertyChange(
            nameof(this.LineSizeProportion),
            ref this.lineSizeProportion,
            value,
            this.lineSizeProportion - value == 0f,
            this.OnLineSizeProportionChange);
    }

    /// <summary>Gets or sets the page size, proportional to the available range.</summary>
    /// <value>A value ranging from 0 to 1. Values outside will be clamped. Note that <c>1</c> will effectively disable
    /// scrolling.</value>
    /// <remarks>This is the scroll amount for clicking on the bar that is not on the thumb, and this is the size of the
    /// thumb proportional to the bar size.</remarks>
    public float PageSizeProportion
    {
        get => this.pageSizeProportion;
        set => this.HandlePropertyChange(
            nameof(this.PageSizeProportion),
            ref this.pageSizeProportion,
            value,
            this.pageSizeProportion - value == 0f,
            this.OnPageSizeProportionChange);
    }

    /// <summary>Gets or sets the minimum size of the thumb, in pixels (pre-scaled).</summary>
    public float MinThumbSize
    {
        get => this.minThumbSize;
        set => this.HandlePropertyChange(
            nameof(this.MinThumbSize),
            ref this.minThumbSize,
            value,
            this.minThumbSize - value == 0f,
            this.OnMinThumbSizeChange);
    }

    /// <summary>Gets or sets the increasing direction of the scroll bar.</summary>
    public LinearDirection Direction
    {
        get => this.direction;
        set => this.HandlePropertyChange(
            nameof(this.Direction),
            ref this.direction,
            value,
            this.direction == value,
            this.OnDirectionChanged);
    }

    /// <summary>Gets or sets the decrease button.</summary>
    public Spannable? DecreaseButton
    {
        get => this.AllSpannables[this.slotIndexDecreaseButton];
        set
        {
            ref var storage = ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexDecreaseButton];
            this.HandlePropertyChange(
                nameof(this.DecreaseButton),
                ref storage,
                value,
                ReferenceEquals(storage, value),
                this.OnDecreaseButtonChanged);
        }
    }

    /// <summary>Gets or sets the increase button.</summary>
    public Spannable? IncreaseButton
    {
        get => this.AllSpannables[this.slotIndexIncreaseButton];
        set
        {
            ref var storage = ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexIncreaseButton];
            this.HandlePropertyChange(
                nameof(this.IncreaseButton),
                ref storage,
                value,
                ReferenceEquals(storage, value),
                this.OnIncreaseButtonChanged);
        }
    }

    /// <summary>Gets or sets the thumb.</summary>
    public Spannable? Thumb
    {
        get => this.AllSpannables[this.slotIndexThumb];
        set
        {
            ref var storage = ref CollectionsMarshal.AsSpan(this.AllSpannables)[this.slotIndexThumb];
            this.HandlePropertyChange(
                nameof(this.Thumb),
                ref storage,
                value,
                ReferenceEquals(storage, value),
                this.OnThumbChanged);
        }
    }

    private Span<Spannable?> ChildSpannables =>
        CollectionsMarshal.AsSpan(this.AllSpannables).Slice(this.slotIndexDecreaseButton, ChildSpannableCount);

    private float EffectiveRange => Math.Max(this.maxValue - this.MinValue, 0f);

    private float NormalizedValue =>
        this.EffectiveRange > 0f
            ? Math.Clamp((this.Value - this.MinValue) / this.EffectiveRange, 0f, 1f)
            : 0f;

    /// <inheritdoc/>
    protected override void OnPreDispatchEvents(SpannableEventArgs args)
    {
        base.OnPreDispatchEvents(args);
        this.HandleScrollActionTick(false);

        if (this.mmbAutoScrollPerSecond != 0f)
        {
            var unboundDelta = this.lineSizeProportion * this.EffectiveRange;

            var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializeScrollEvent(
                ScrollAction.MiddleButtonAutoScroll,
                this.value,
                Math.Clamp(this.value + (unboundDelta * this.mmbAutoScrollPerSecond), this.minValue, this.maxValue),
                unboundDelta,
                this.mmbAutoScrollPerSecond);
            this.OnScroll(e);
            if (this.autoValueUpdate && !e.SuppressHandling)
                this.Value = e.NewValue;
            SpannableEventArgsPool.Return(e);
        }
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize)
    {
        var children = this.ChildSpannables;
        for (var i = 0; i < ChildSpannableCount; i++)
        {
            if (children[i] is not { } m)
                continue;
            m.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.childInnerIds[i]);
            m.Renderer = this.Renderer;
            m.Options.RenderScale = this.EffectiveRenderScale;
        }

        if (suggestedSize.GetOffDirection(this.direction) >= float.PositiveInfinity)
            suggestedSize = suggestedSize.UpdateOffDirection(this.direction, DefaultThickness);
        if (suggestedSize.GetMainDirection(this.direction) >= float.PositiveInfinity)
            suggestedSize.UpdateMainDirection(this.direction, DefaultLength);

        this.barSize = suggestedSize.GetMainDirection(this.direction);

        if (children[MeasurementIndexDecreaseButton] is { } decbut)
        {
            decbut.Options.PreferredSize = suggestedSize.UpdateMainDirection(
                this.direction,
                suggestedSize.GetOffDirection(this.direction));
            decbut.Options.VisibleSize = decbut.Options.PreferredSize;
            decbut.RenderPassMeasure();
            this.barSize -= decbut.Boundary.Bottom;
        }

        if (children[MeasurementIndexIncreaseButton] is { } incbut)
        {
            incbut.Options.PreferredSize = suggestedSize.UpdateMainDirection(
                this.direction,
                suggestedSize.GetOffDirection(this.direction));
            incbut.Options.VisibleSize = incbut.Options.PreferredSize;
            incbut.RenderPassMeasure();
            this.barSize -= incbut.Boundary.Bottom;
        }

        if (this.EffectiveRange > 0)
        {
            this.thumbSize = this.barSize * Math.Clamp(this.pageSizeProportion, 0, 1);
            this.thumbSize = Math.Max(
                this.barSize * Math.Clamp(this.pageSizeProportion, 0, 1),
                Math.Min(this.minThumbSize, this.barSize / 2f));
            this.thumbOffset = (this.barSize - this.thumbSize) * this.NormalizedValue;
            this.thumbOffset = this.direction.ConvertGravity(this.NormalizedValue);
            this.thumbOffset *= this.barSize - this.thumbSize;
        }
        else
        {
            this.thumbOffset = this.thumbSize = 0f;
        }

        if (children[MeasurementIndexThumb] is { } thumb && this.EffectiveRange > 0)
        {
            thumb.Options.PreferredSize = suggestedSize.UpdateMainDirection(this.direction, this.thumbSize);
            thumb.Options.VisibleSize = thumb.Options.PreferredSize;
            thumb.RenderPassMeasure();
        }

        return new(Vector2.Zero, suggestedSize);
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        var children = this.ChildSpannables;

        this.barOffset = 0f;
        if (children[MeasurementIndexDecreaseButton] is { } decbut)
        {
            var translation = this.direction switch
            {
                LinearDirection.LeftToRight => this.MeasuredContentBox.LeftTop,
                LinearDirection.RightToLeft => new(
                    this.MeasuredContentBox.Right - decbut.Boundary.Right,
                    this.MeasuredContentBox.Top),
                LinearDirection.TopToBottom => this.MeasuredContentBox.LeftTop,
                LinearDirection.BottomToTop => new(
                    this.MeasuredContentBox.Left,
                    this.MeasuredContentBox.Bottom - decbut.Boundary.Bottom),
                _ => Vector2.Zero,
            };
            decbut.RenderPassPlace(Matrix4x4.CreateTranslation(new(translation, 0)), this.FullTransformation);
            if (this.direction == LinearDirection.TopToBottom)
                this.barOffset = decbut.Boundary.Bottom;
            else if (this.direction == LinearDirection.LeftToRight)
                this.barOffset = decbut.Boundary.Right;
        }

        if (children[MeasurementIndexIncreaseButton] is { } incbut)
        {
            var translation = this.direction switch
            {
                LinearDirection.LeftToRight => new(
                    this.MeasuredContentBox.Right - incbut.Boundary.Right,
                    this.MeasuredContentBox.Top),
                LinearDirection.RightToLeft => this.MeasuredContentBox.LeftTop,
                LinearDirection.TopToBottom => new(
                    this.MeasuredContentBox.Left,
                    this.MeasuredContentBox.Bottom - incbut.Boundary.Bottom),
                LinearDirection.BottomToTop => this.MeasuredContentBox.LeftTop,
                _ => Vector2.Zero,
            };
            incbut.RenderPassPlace(Matrix4x4.CreateTranslation(new(translation, 0)), this.FullTransformation);
            if (this.direction == LinearDirection.BottomToTop)
                this.barOffset = incbut.Boundary.Bottom;
            else if (this.direction == LinearDirection.RightToLeft)
                this.barOffset = incbut.Boundary.Right;
        }

        if (children[MeasurementIndexThumb] is { } thumb && this.EffectiveRange > 0)
        {
            var translation = this.MeasuredContentBox.LeftTop;
            if (this.direction.IsVertical())
                translation.Y += this.barOffset + this.thumbOffset;
            else
                translation.X += this.barOffset + this.thumbOffset;
            thumb.RenderPassPlace(Matrix4x4.CreateTranslation(new(translation, 0)), this.FullTransformation);
        }

        base.OnPlace(args);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);
        var children = this.ChildSpannables;
        children[MeasurementIndexIncreaseButton]?.RenderPassDraw(args.DrawListPtr);
        children[MeasurementIndexDecreaseButton]?.RenderPassDraw(args.DrawListPtr);
        if (this.EffectiveRange > 0f)
            children[MeasurementIndexThumb]?.RenderPassDraw(args.DrawListPtr);
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(SpannableMouseEventArgs args)
    {
        this.currentMouseThumbLocation = args.LocalLocation;

        base.OnMouseDown(args);
        if (args.SuppressHandling
            || this.EffectiveRange <= 0f
            || args.Step == SpannableEventStep.BeforeChildren)
            return;

        switch (args.Button)
        {
            case ImGuiMouseButton.Left:
            {
                var sp = this.FindChildAtPos(ImGui.GetMousePos());
                if (ReferenceEquals(this.DecreaseButton, sp))
                {
                    this.currentMouseDownScrollAction = ScrollAction.LineDecrement;
                    this.HandleScrollActionTick(true);
                    args.SuppressHandling = true;
                }
                else if (ReferenceEquals(this.IncreaseButton, sp))
                {
                    this.currentMouseDownScrollAction = ScrollAction.LineIncrement;
                    this.HandleScrollActionTick(true);
                    args.SuppressHandling = true;
                }
                else if (ReferenceEquals(this.Thumb, sp))
                {
                    // Calculate the range of thumb movement, based on the current value.
                    var r = this.barSize - this.thumbSize;
                    var nv = this.direction.ConvertGravity(this.NormalizedValue);
                    var start = args.LocalLocation.GetMainDirection(this.direction) - (r * nv);
                    var end = start + r;
                    this.currentMouseDownRange = new(start, end);

                    this.currentMouseDownScrollAction = ScrollAction.ThumbTrack;
                    args.SuppressHandling = true;
                }
                else if (this.IsMouseHoveredIncludingChildren)
                {
                    var downOffsetV2 = (args.LocalLocation / this.Scale) - this.MeasuredContentBox.LeftTop;
                    var downOffset = downOffsetV2.GetMainDirection(this.direction);

                    // Calculate the range of thumb movement, based on the value that the pointer is on.
                    var start = this.barOffset;
                    var end = this.barOffset + this.barSize;
                    this.currentMouseDownRange = new(start, end);

                    if (downOffset < this.thumbOffset)
                    {
                        this.currentMouseDownScrollAction =
                            this.direction.IsDirectionConsistentWithIndex()
                                ? ScrollAction.PageDecrement
                                : ScrollAction.PageIncrement;
                        this.HandleScrollActionTick(true);
                        args.SuppressHandling = true;
                    }
                    else if (downOffset >= this.thumbOffset + this.thumbSize)
                    {
                        this.currentMouseDownScrollAction =
                            this.direction.IsDirectionConsistentWithIndex()
                                ? ScrollAction.PageIncrement
                                : ScrollAction.PageDecrement;
                        this.HandleScrollActionTick(true);
                        args.SuppressHandling = true;
                    }
                }

                break;
            }

            case ImGuiMouseButton.Middle:
            {
                this.MouseCursor = this.direction.IsVertical() ? ImGuiMouseCursor.ResizeNS : ImGuiMouseCursor.ResizeEW;
                this.mmbScrollOrigin = args.LocalLocation;
                args.SuppressHandling = true;
                break;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(SpannableMouseEventArgs args)
    {
        this.currentMouseThumbLocation = args.LocalLocation;
        base.OnMouseMove(args);

        if (this.mmbScrollOrigin.X is not float.NaN)
        {
            this.mmbAutoScrollPerSecond =
                (args.LocalLocation - this.mmbScrollOrigin).GetMainDirection(this.direction) / 4f;
            return;
        }

        if (this.currentMouseDownScrollAction == ScrollAction.ThumbTrack)
        {
            var unboundDelta = this.PointerInRangeToValue() - this.value;
            if (MathF.Abs(unboundDelta) < 0.000001f)
                return;

            var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializeScrollEvent(
                ScrollAction.ThumbTrack,
                this.value,
                Math.Clamp(this.value + unboundDelta, this.minValue, this.maxValue),
                unboundDelta,
                1);
            this.OnScroll(e);
            if (this.autoValueUpdate && !e.SuppressHandling)
                this.Value = e.NewValue;
            SpannableEventArgsPool.Return(e);
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(SpannableMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        
        if (args.SuppressHandling
            || !this.IsMouseHoveredIncludingChildren
            || this.EffectiveRange <= 0f
            || this.lineSizeProportion <= 0f
            || args.Step == SpannableEventStep.BeforeChildren)
            return;
        
        args.SuppressHandling = true;

        var repeats = -(args.WheelDelta.X + args.WheelDelta.Y);
        var scrollAction = repeats switch
        {
            > 0 => ScrollAction.LineIncrement,
            < 0 => ScrollAction.LineDecrement,
            _ => ScrollAction.None,
        };
        if (scrollAction == ScrollAction.None)
            return;
        
        var unboundDelta = this.lineSizeProportion * this.EffectiveRange;

        var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializeScrollEvent(
            scrollAction,
            this.value,
            Math.Clamp(this.value + (unboundDelta * repeats), this.minValue, this.maxValue),
            unboundDelta,
            MathF.Abs(repeats));
        this.OnScroll(e);
        if (this.autoValueUpdate && !e.SuppressHandling)
            this.Value = e.NewValue;
        SpannableEventArgsPool.Return(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(SpannableMouseEventArgs args)
    {
        base.OnMouseUp(args);
        switch (args.Button)
        {
            case ImGuiMouseButton.Left:
                this.currentMouseDownScrollAction = ScrollAction.None;
                break;
            case ImGuiMouseButton.Middle:
                this.mmbScrollOrigin = new(float.NaN);
                this.MouseCursor = ImGuiMouseCursor.Arrow;
                this.mmbAutoScrollPerSecond = 0;
                break;
        }
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

    /// <summary>Raises the <see cref="LineSizeProportionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnLineSizeProportionChange(PropertyChangeEventArgs<float> args) =>
        this.LineSizeProportionChange?.Invoke(args);

    /// <summary>Raises the <see cref="PageSizeProportionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnPageSizeProportionChange(PropertyChangeEventArgs<float> args) =>
        this.PageSizeProportionChange?.Invoke(args);

    /// <summary>Raises the <see cref="MinThumbSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMinThumbSizeChange(PropertyChangeEventArgs<float> args) =>
        this.MinThumbSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="DirectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDirectionChanged(PropertyChangeEventArgs<LinearDirection> args) =>
        this.DirectionChange?.Invoke(args);

    /// <summary>Raises the <see cref="DecreaseButtonChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDecreaseButtonChanged(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            if (args.PreviousValue is ControlSpannable pcs)
            {
                pcs.MousePress -= this.IncDecMousePressLongAndRepeat;
                pcs.MouseDown -= this.IncDecMousePressLongAndRepeat;
            }

            if (args.NewValue is ControlSpannable ncs)
            {
                ncs.MousePress += this.IncDecMousePressLongAndRepeat;
                ncs.MouseDown += this.IncDecMousePressLongAndRepeat;
            }
        }

        this.DecreaseButtonChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="IncreaseButtonChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIncreaseButtonChanged(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            if (args.PreviousValue is ControlSpannable pcs)
            {
                pcs.MousePress -= this.IncDecMousePressLongAndRepeat;
                pcs.MouseDown -= this.IncDecMousePressLongAndRepeat;
            }

            if (args.NewValue is ControlSpannable ncs)
            {
                ncs.MousePress += this.IncDecMousePressLongAndRepeat;
                ncs.MouseDown += this.IncDecMousePressLongAndRepeat;
            }
        }

        this.IncreaseButtonChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ThumbChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnThumbChanged(PropertyChangeEventArgs<Spannable?> args) =>
        this.ThumbChange?.Invoke(args);

    /// <summary>Raises the <see cref="Scroll"/> event.</summary>
    /// <param name="args">A <see cref="ScrollEventArgs"/> that contains the event data.</param>
    protected virtual void OnScroll(ScrollEventArgs args) => this.Scroll?.Invoke(args);

    private float PointerInRangeToValue()
    {
        var t = this.currentMouseThumbLocation.GetMainDirection(this.direction);
        t = (t - this.currentMouseDownRange.X) /
            (this.currentMouseDownRange.Y - this.currentMouseDownRange.X);
        t = Math.Clamp(t, 0, 1);
        if (!this.direction.IsDirectionConsistentWithIndex())
            t = 1 - t;
        t *= this.maxValue - this.minValue;
        t += this.minValue;
        return t;
    }

    private void IncDecMousePressLongAndRepeat(SpannableMouseEventArgs args)
    {
        if (args.Button != ImGuiMouseButton.Left || this.EffectiveRange <= 0f)
            return;

        var sa = ScrollAction.None;
        var ubd = 0f;

        if (ReferenceEquals(args.Sender, this.DecreaseButton))
        {
            ubd = -this.lineSizeProportion * this.EffectiveRange;
            sa = ScrollAction.LineDecrement;
        }
        else if (ReferenceEquals(args.Sender, this.IncreaseButton))
        {
            ubd = +this.lineSizeProportion * this.EffectiveRange;
            sa = ScrollAction.LineIncrement;
        }

        if (sa != ScrollAction.None)
        {
            var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializeScrollEvent(
                sa,
                this.value,
                Math.Clamp(this.value + (args.ImmediateRepeats * ubd), this.minValue, this.maxValue),
                ubd,
                args.ImmediateRepeats);
            this.OnScroll(e);
            if (this.autoValueUpdate && !e.SuppressHandling)
                this.Value = e.NewValue;
            SpannableEventArgsPool.Return(e);
        }
    }

    private void HandleScrollActionTick(bool first)
    {
        if (this.currentMouseDownScrollAction is not ScrollAction.LineDecrement and not ScrollAction.LineIncrement
            and not ScrollAction.PageDecrement and not ScrollAction.PageIncrement)
            return;

        var repeatCount = 1;
        if (first)
        {
            this.repeatNext = Environment.TickCount64 + WindowsUiConfigHelper.GetKeyboardRepeatInitialDelay();
        }
        else if (this.currentMouseDownScrollAction != ScrollAction.ThumbTrack)
        {
            var d = Environment.TickCount64 - this.repeatNext;
            if (d < 0)
                return;

            var repeatInterval = WindowsUiConfigHelper.GetKeyboardRepeatInterval();
            repeatCount = 1 + (int)MathF.Floor((float)d / repeatInterval);
            this.repeatNext += repeatInterval * repeatCount;
        }

        float unboundDelta;
        switch (this.currentMouseDownScrollAction)
        {
            // Following two cases will be called if IncrementButton/DecrementButtons are not ControlSpannables.
            case ScrollAction.LineDecrement:
                unboundDelta = -this.lineSizeProportion * this.EffectiveRange;
                break;
            case ScrollAction.LineIncrement:
                unboundDelta = +this.lineSizeProportion * this.EffectiveRange;
                break;

            // Stop when scrolling by page goes past the thumb.
            case ScrollAction.PageDecrement:
                unboundDelta = -this.pageSizeProportion * this.EffectiveRange;

                // Prevent going past the mouse.
                while (repeatCount > 0)
                {
                    if (this.value + (unboundDelta * repeatCount) <= this.PointerInRangeToValue())
                        repeatCount--;
                    else
                        break;
                }

                break;
            case ScrollAction.PageIncrement:
                unboundDelta = +this.pageSizeProportion * this.EffectiveRange;

                // Stop when scrolling by page goes past the thumb.
                while (repeatCount > 0)
                {
                    if (this.value + (unboundDelta * repeatCount) >= this.PointerInRangeToValue())
                        repeatCount--;
                    else
                        break;
                }

                break;

            case ScrollAction.ThumbTrack:
            case ScrollAction.None:
            default:
                return;
        }

        if (repeatCount == 0)
            return;

        var e = SpannableEventArgsPool.Rent<ScrollEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializeScrollEvent(
            this.currentMouseDownScrollAction,
            this.value,
            Math.Clamp(this.value + (unboundDelta * repeatCount), this.minValue, this.maxValue),
            unboundDelta,
            repeatCount);
        this.OnScroll(e);
        if (this.autoValueUpdate && !e.SuppressHandling)
            this.Value = e.NewValue;
        SpannableEventArgsPool.Return(e);
    }

    private void ChildOnPropertyChange(PropertyChangeEventArgs args) => this.RequestMeasure();

    /// <summary>Event arguments for <see cref="Scroll"/> event.</summary>
    public record ScrollEventArgs : SpannableEventArgs
    {
        /// <summary>Gets the action that resulted in a scrolling.</summary>
        public ScrollAction Action { get; private set; }

        /// <summary>Gets the old value.</summary>
        public float OldValue { get; private set; }

        /// <summary>Gets the new value.</summary>
        /// <remarks>This value will not be applied to <see cref="ScrollBarControl.Value"/> if
        /// <see cref="ScrollBarControl.AutoValueUpdate"/> is not set.</remarks>
        public float NewValue { get; private set; }

        /// <summary>Gets the delta of <see cref="ScrollBarControl.Value"/> if no boundaries are to be checked.
        /// </summary>
        public float UnboundDelta { get; private set; }

        /// <summary>Gets the number of repeats that must be multiplied to <see cref="UnboundDelta"/>.</summary>
        public float RepeatCount { get; private set; }

        /// <summary>Initializes the scroll related properties.</summary>
        /// <param name="action">Action.</param>
        /// <param name="oldValue">Old value.</param>
        /// <param name="newValue">New value.</param>
        /// <param name="unboundDelta">Unbound delta.</param>
        /// <param name="repeatCount">Repeat count.</param>
        public void InitializeScrollEvent(
            ScrollAction action, float oldValue, float newValue, float unboundDelta, float repeatCount)
        {
            this.Action = action;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.UnboundDelta = unboundDelta;
            this.RepeatCount = repeatCount;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public class SpannableControl : ISpannable, ISpannableState
{
    /// <summary>Uses the dimensions provided from the parent.</summary>
    public const float MatchParent = -1f;

    /// <summary>Uses the dimensions that will wrap the content.</summary>
    public const float WrapContent = -2f;

    private static readonly ImGuiMouseButton[] MouseButtonsWeCare =
        [ImGuiMouseButton.Left, ImGuiMouseButton.Right, ImGuiMouseButton.Middle];

    private bool initializeFromArgumentsCalled;

    private TextState activeTextState;

    private RectVector4 boundary;
    private Trss transformation;

    private RectVector4 measuredExtrudedBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private Vector2 lastMouseLocation;
    private int heldMouseButtons;
    private long[] lastMouseClickTick = new long[MouseButtonsWeCare.Length];
    private int[] lastMouseClickCount = new int[MouseButtonsWeCare.Length];

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event SpannableControlMouseEventHandler? MouseClick;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is pressed.</summary>
    public event SpannableControlMouseEventHandler? MouseDown;

    /// <summary>Occurs when the mouse pointer enters the control.</summary>
    public event SpannableControlMouseEventHandler? MouseEnter;

    /// <summary>Occurs when the mouse pointer leaves the control.</summary>
    public event SpannableControlMouseEventHandler? MouseLeave;

    /// <summary>Occurs when the mouse pointer is moved over the control.</summary>
    public event SpannableControlMouseEventHandler? MouseMove;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is released.</summary>
    public event SpannableControlMouseEventHandler? MouseUp;

    /// <summary>Occurs when the mouse wheel moves while the control is hovered.</summary>
    public event SpannableControlMouseEventHandler? MouseWheel;

    /// <summary>Gets or sets a value indicating whether this control is enabled and accepting inputs.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 Size { get; set; } = new(WrapContent);

    /// <summary>Gets or sets the minimum size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MinSize { get; set; } = Vector2.Zero;

    /// <summary>Gets or sets the maximum size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MaxSize { get; set; } = new(float.MaxValue);

    /// <summary>Gets or sets the extrusion.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Extrude { get; set; } = RectVector4.Zero;

    /// <summary>Gets or sets the margin.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Margin { get; set; } = RectVector4.Zero;

    /// <summary>Gets or sets the padding.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Padding { get; set; } = RectVector4.Zero;

    /// <inheritdoc/>
    public ref TextState TextState => ref this.activeTextState;

    /// <inheritdoc/>
    public uint ImGuiGlobalId { get; private set; }

    /// <inheritdoc/>
    /// <remarks>This excludes <see cref="Extrude"/>.</remarks>
    public ref readonly RectVector4 Boundary => ref this.boundary;

    /// <inheritdoc/>
    public Vector2 ScreenOffset { get; private set; }

    /// <inheritdoc/>
    public Vector2 TransformationOrigin { get; private set; }

    /// <inheritdoc/>
    public ref readonly Trss Transformation => ref this.transformation;

    /// <inheritdoc/>
    public ISpannableRenderer Renderer { get; private set; } = null!;

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll up event should be intercepted.</summary>
    public bool InterceptMouseWheelUp { get; set; }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll down event should be intercepted.</summary>
    public bool InterceptMouseWheelDown { get; set; }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll left event should be intercepted.</summary>
    public bool InterceptMouseWheelLeft { get; set; }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll right event should be intercepted.</summary>
    public bool InterceptMouseWheelRight { get; set; }

    /// <summary>Gets a value indicating whether the mouse button is hovering.</summary>
    public bool IsMouseHovered { get; private set; }

    /// <summary>Gets a value indicating whether the left mouse button is down.</summary>
    public bool IsLeftMouseButtonDown => (this.heldMouseButtons & 1) != 0;

    /// <summary>Gets a value indicating whether the right mouse button is down.</summary>
    public bool IsRightMouseButtonDown => (this.heldMouseButtons & 2) != 0;

    /// <summary>Gets a value indicating whether the middle mouse button is down.</summary>
    public bool IsMiddleMouseButtonDown => (this.heldMouseButtons & 4) != 0;

    /// <summary>Gets a value indicating whether the width is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsWidthWrapContent => this.Size.X == WrapContent;

    /// <summary>Gets a value indicating whether the height is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsHeightWrapContent => this.Size.Y == WrapContent;

    /// <summary>Gets the last measured extruded box size.</summary>
    /// <remarks>Useful for drawing decoration outside the standard box, such as glow or shadow effects.</remarks>
    protected ref readonly RectVector4 MeasuredExtrudedBox => ref this.measuredExtrudedBox;

    /// <summary>Gets the last measured interactive box size.</summary>
    /// <remarks>This excludes <see cref="Extrude"/> and <see cref="Margin"/>.</remarks>
    protected ref readonly RectVector4 MeasuredInteractiveBox => ref this.measuredInteractiveBox;

    /// <summary>Gets the last measured content box size.</summary>
    /// <remarks>This excludes <see cref="Extrude"/>, <see cref="Margin"/>, and <see cref="Padding"/>.</remarks>
    protected ref readonly RectVector4 MeasuredContentBox => ref this.measuredContentBox;

    /// <summary>Gets the scale.</summary>
    protected float Scale { get; private set; }

    /// <inheritdoc/>
    public ISpannableState RentState(
        ISpannableRenderer renderer,
        uint imGuiGlobalId,
        float scale,
        string? args,
        in TextState textState)
    {
        this.Renderer = renderer;
        if (!this.initializeFromArgumentsCalled)
        {
            this.InitializeFromArguments(args);
            this.initializeFromArgumentsCalled = true;
        }

        this.ImGuiGlobalId = imGuiGlobalId;
        this.Scale = scale;
        this.activeTextState = textState;

        // Spannable itself is a state. Return self.
        return this;
    }

    /// <inheritdoc/>
    public virtual void ReturnState(ISpannableState? state)
    {
    }

    /// <inheritdoc/>
    public void Measure(SpannableMeasureArgs args)
    {
        var rv = new RectVector4(
            Vector2.Zero,
            Vector2.Clamp(
                ResolveSize(args, this.Size),
                ResolveSize(args, this.MinSize),
                ResolveSize(args, this.MaxSize)));

        rv = RectVector4.Extrude(rv, -(this.Margin + this.Padding) * this.Scale);
        rv = RectVector4.Normalize(rv);

        if (rv.Right >= 1000000f)
            rv.Right = float.PositiveInfinity;
        if (rv.Bottom >= 1000000f)
            rv.Bottom = float.PositiveInfinity;
        this.measuredContentBox = this.MeasureContentBox(args, rv);
        this.measuredContentBox = RectVector4.Normalize(this.measuredContentBox);

        this.measuredInteractiveBox = RectVector4.Extrude(this.measuredContentBox, this.Padding * this.Scale);
        this.boundary = RectVector4.Extrude(this.measuredInteractiveBox, this.Margin * this.Scale);
        this.measuredExtrudedBox = RectVector4.Extrude(this.boundary, this.Extrude * this.Scale);
        return;

        static Vector2 ResolveSize(SpannableMeasureArgs args, in Vector2 dim)
        {
            return new(
                dim.X switch
                {
                    MatchParent or WrapContent => args.MaxSize.X,
                    _ => dim.X,
                },
                dim.Y switch
                {
                    MatchParent or WrapContent => args.MaxSize.Y,
                    _ => dim.Y,
                });
        }
    }

    /// <inheritdoc/>
    public virtual void CommitMeasurement(SpannableCommitTransformationArgs args)
    {
        this.ScreenOffset = args.ScreenOffset;
        this.TransformationOrigin = args.TransformationOrigin;
        this.transformation = args.Transformation;
    }

    /// <inheritdoc/>
    public virtual void HandleInteraction(SpannableHandleInteractionArgs args, out SpannableLinkInteracted link)
    {
        const int selfInnerId = 0x1234;

        link = default;

        if (!this.Enabled)
        {
            this.IsMouseHovered = false;
            this.heldMouseButtons = 0;
            return;
        }

        var margs = new SpannableControlMouseEventArgs { LocalLocation = args.MouseLocalLocation };
        var interceptWheel = false;
        interceptWheel |= args.WheelDelta.X > 0 && this.InterceptMouseWheelRight;
        interceptWheel |= args.WheelDelta.X < 0 && this.InterceptMouseWheelLeft;
        interceptWheel |= args.WheelDelta.Y > 0 && this.InterceptMouseWheelDown;
        interceptWheel |= args.WheelDelta.Y < 0 && this.InterceptMouseWheelUp;

        if (this.heldMouseButtons != 0)
            args.SetActive(selfInnerId, interceptWheel);

        var hovered = this.measuredInteractiveBox.Contains(margs.LocalLocation) && args.IsItemHoverable(selfInnerId);
        if (hovered != this.IsMouseHovered)
        {
            if (hovered)
                this.OnMouseEnter(margs);
            else
                this.OnMouseLeave(margs);
            this.IsMouseHovered = hovered;
        }

        if (args.WheelDelta != Vector2.Zero)
            this.OnMouseWheel(margs with { Delta = args.WheelDelta });
        
        if (this.lastMouseLocation != margs.LocalLocation)
        {
            this.OnMouseMove(margs);
            this.lastMouseLocation = margs.LocalLocation;
        }

        var lastHeldMouseButtons = this.heldMouseButtons;
        if (lastHeldMouseButtons != 0 || hovered)
        {
            for (var i = 0; i < MouseButtonsWeCare.Length; i++)
            {
                var held = args.IsMouseButtonDown(MouseButtonsWeCare[i]);
                if (held == ((this.heldMouseButtons & (1 << i)) != 0))
                    continue;

                if (held)
                {
                    this.OnMouseDown(margs with { Button = MouseButtonsWeCare[i] });
                    this.heldMouseButtons |= 1 << i;
                }
                else
                {
                    this.OnMouseUp(margs with { Button = MouseButtonsWeCare[i] });
                    this.heldMouseButtons &= ~(1 << i);

                    if (hovered)
                    {
                        if (this.lastMouseClickTick[i] < Environment.TickCount64)
                            this.lastMouseClickCount[i] = 1;
                        else
                            this.lastMouseClickCount[i] += 1;
                        this.lastMouseClickTick[i] = Environment.TickCount64 + GetDoubleClickTime();
                        this.OnMouseClick(
                            margs with
                            {
                                Button = MouseButtonsWeCare[i],
                                Clicks = this.lastMouseClickCount[i],
                            });
                    }
                    else
                    {
                        this.lastMouseClickTick[i] = 0;
                    }
                }
            }
        }

        if ((this.heldMouseButtons != 0) != (lastHeldMouseButtons != 0))
        {
            ImGui.SetNextFrameWantCaptureMouse(this.heldMouseButtons != 0);
            if (this.heldMouseButtons == 0)
                args.ClearActive();
        }

        if (this.IsMouseHovered)
            args.SetHovered(selfInnerId, interceptWheel);

        if (this.heldMouseButtons != 0)
            args.SetActive(selfInnerId, interceptWheel);
    }

    /// <inheritdoc/>
    public virtual void Draw(SpannableDrawArgs args)
    {
    }

    /// <summary>Clears any initialized state.</summary>
    public virtual void Reset()
    {
        this.initializeFromArgumentsCalled = false;
        this.IsMouseHovered = false;
        this.lastMouseLocation = Vector2.Zero;
        this.heldMouseButtons = 0;
    }

    /// <summary>Clamps the given value without performing sanity check.</summary>
    /// <param name="value">The vector to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <typeparam name="T">A numeric type.</typeparam>
    /// <returns>The clamped vector.</returns>
    /// <remarks>This follows the behavior of <see cref="Vector4.Clamp"/>.</remarks>
    protected static T ClampNoCheck<T>(T value, T min, T max) where T : INumber<T> => T.Min(T.Max(value, min), max);

    /// <summary>Initializes this control from the given arguments, specified from
    /// <see cref="SpannedStringBuilder.AppendSpannable(ISpannable?, string?, out int)"/>.</summary>
    /// <param name="args">The arbitrary arguments.</param>
    /// <remarks>Once returned from this function without exceptions, this function will not be called again until the
    /// next call to <see cref="Reset"/>.</remarks>
    protected virtual void InitializeFromArguments(string? args)
    {
    }

    /// <summary>Measures the content box, given the available content box excluding the margin and padding.</summary>
    /// <param name="args">Measure arguments.</param>
    /// <param name="availableContentBox">The available content box.</param>
    /// <returns>The resolved content box.</returns>
    /// <remarks>Right and bottom values can be unbound (<see cref="float.PositiveInfinity"/>), in which case, it
    /// should be treated as having no defined limits, and <see cref="WrapContent"/> behavior should be done.</remarks>
    protected virtual RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        var widthDefined = availableContentBox is { Left: >= -65536, Right: <= 65535 };
        var heightDefined = availableContentBox is { Top: >= -65536, Bottom: <= 65535 };
        var result = availableContentBox;
        if (!widthDefined)
            result.Right = result.Left /* + WrapContentWidth */;
        if (!heightDefined)
            result.Bottom = result.Top /* + WrapContentHeight */;
        return availableContentBox with
        {
            Right = availableContentBox.Right >= float.MaxValue
                        ? availableContentBox.Left
                        : availableContentBox.Right,
            Bottom = availableContentBox.Bottom >= float.MaxValue
                         ? availableContentBox.Top
                         : availableContentBox.Bottom,
        };
    }

    /// <summary>Raises the <see cref="MouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseClick(in SpannableControlMouseEventArgs args) => this.MouseClick?.Invoke(args);

    /// <summary>Raises the <see cref="MouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseDown(in SpannableControlMouseEventArgs args) => this.MouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="MouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseEnter(in SpannableControlMouseEventArgs args) => this.MouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="MouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseLeave(in SpannableControlMouseEventArgs args) => this.MouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="MouseMove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseMove(in SpannableControlMouseEventArgs args) => this.MouseMove?.Invoke(args);

    /// <summary>Raises the <see cref="MouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseUp(in SpannableControlMouseEventArgs args) => this.MouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="MouseWheel"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseWheel(in SpannableControlMouseEventArgs args) => this.MouseWheel?.Invoke(args);
}
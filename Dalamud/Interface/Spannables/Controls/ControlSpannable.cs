using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public partial class ControlSpannable : ISpannable, ISpannableRenderPass, ISpannableSerializable
{
    /// <summary>Uses the dimensions provided from the parent.</summary>
    public const float MatchParent = -1f;

    /// <summary>Uses the dimensions that will wrap the content.</summary>
    public const float WrapContent = -2f;

    private static readonly ImGuiMouseButton[] MouseButtonsWeCare =
        [ImGuiMouseButton.Left, ImGuiMouseButton.Right, ImGuiMouseButton.Middle];

    private readonly int backgroundInnerId;
    private readonly int selfInnerId;

    private readonly int normalBackgroundChildIndex;
    private readonly int hoveredBackgroundChildIndex;
    private readonly int activeBackgroundChildIndex;
    private readonly int disabledBackgroundChildIndex;

    private ISpannable? currentBackground;
    private ISpannableRenderPass? currentBackgroundPass;

    private TextState activeTextState;
    private RectVector4 boundary;
    private Matrix4x4 transformation;

    private RectVector4 measuredOutsideBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private Vector2 lastMouseLocation;
    private int heldMouseButtons;
    private long[] lastMouseClickTick = new long[MouseButtonsWeCare.Length];
    private int[] lastMouseClickCount = new int[MouseButtonsWeCare.Length];

    private bool wasVisible;

    /// <summary>Initializes a new instance of the <see cref="ControlSpannable"/> class.</summary>
    public ControlSpannable()
    {
        this.backgroundInnerId = this.InnerIdAvailableSlot++;
        this.selfInnerId = this.InnerIdAvailableSlot++;
        this.normalBackgroundChildIndex = this.AllSpannablesAvailableSlot++;
        this.hoveredBackgroundChildIndex = this.AllSpannablesAvailableSlot++;
        this.activeBackgroundChildIndex = this.AllSpannablesAvailableSlot++;
        this.disabledBackgroundChildIndex = this.AllSpannablesAvailableSlot++;
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
    }

    /// <inheritdoc />
    public int StateGeneration { get; protected set; }

    /// <inheritdoc/>
    public ref TextState ActiveTextState => ref this.activeTextState;

    /// <inheritdoc/>
    /// <remarks>This excludes <see cref="ExtendOutside"/>.</remarks>
    public ref readonly RectVector4 Boundary => ref this.boundary;

    /// <inheritdoc/>
    public Vector2 InnerOrigin { get; private set; }

    /// <inheritdoc/>
    public ref readonly Matrix4x4 Transformation => ref this.transformation;

    /// <inheritdoc/>
    public uint ImGuiGlobalId { get; private set; }

    /// <inheritdoc/>
    public ISpannableRenderer Renderer { get; private set; } = null!;

    /// <summary>Gets a value indicating whether the mouse pointer is hovering.</summary>
    public bool IsMouseHovered { get; private set; }

    /// <summary>Gets a value indicating whether the left mouse button is down.</summary>
    public bool IsLeftMouseButtonDown => (this.heldMouseButtons & 1) != 0;

    /// <summary>Gets a value indicating whether the right mouse button is down.</summary>
    public bool IsRightMouseButtonDown => (this.heldMouseButtons & 2) != 0;

    /// <summary>Gets a value indicating whether the middle mouse button is down.</summary>
    public bool IsMiddleMouseButtonDown => (this.heldMouseButtons & 4) != 0;

    /// <summary>Gets the last measured extruded box size.</summary>
    /// <remarks>Useful for drawing decoration outside the standard box, such as glow or shadow effects.</remarks>
    public ref readonly RectVector4 MeasuredExtrudedBox => ref this.measuredOutsideBox;

    /// <summary>Gets the last measured interactive box size.</summary>
    /// <remarks>This excludes <see cref="ExtendOutside"/> and <see cref="Margin"/>.</remarks>
    public ref readonly RectVector4 MeasuredInteractiveBox => ref this.measuredInteractiveBox;

    /// <summary>Gets the last measured content box size.</summary>
    /// <remarks>This excludes <see cref="ExtendOutside"/>, <see cref="Margin"/>, and <see cref="Padding"/>.</remarks>
    public ref readonly RectVector4 MeasuredContentBox => ref this.measuredContentBox;

    /// <summary>Gets a value indicating whether the width is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsWidthWrapContent => this.size.X == WrapContent;

    /// <summary>Gets a value indicating whether the height is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsHeightWrapContent => this.size.Y == WrapContent;

    /// <summary>Gets the scale.</summary>
    protected float Scale { get; private set; }

    /// <summary>Gets the list of all children contained within this control, including decorative ones.</summary>
    protected List<ISpannable?> AllSpannables { get; } = [];

    /// <summary>Gets the available slot index in <see cref="AllSpannables"/> for use by inheritors.</summary>
    protected int AllSpannablesAvailableSlot { get; init; }

    /// <summary>Gets the available slot index for inner ID, for use with
    /// <see cref="SpannableExtensions.GetGlobalIdFromInnerId"/>.</summary>
    protected int InnerIdAvailableSlot { get; init; }

    /// <summary>Gets a value indicating whether <see cref="IDisposable.Dispose"/> has been called.</summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>Gets either <see cref="showAnimation"/> or <see cref="hideAnimation"/> according to
    /// <see cref="visible"/>.</summary>
    private SpannableAnimator? VisibilityAnimation => this.visible ? this.showAnimation : this.hideAnimation;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ISpannable?> GetAllChildSpannables() => this.AllSpannables;

    /// <inheritdoc/>
    public ISpannableRenderPass RentRenderPass(ISpannableRenderer renderer)
    {
        this.Renderer = renderer;

        // Spannable itself is a state. Return self.
        return this;
    }

    /// <inheritdoc/>
    public virtual void ReturnRenderPass(ISpannableRenderPass? pass)
    {
    }

    /// <inheritdoc/>
    public void MeasureSpannable(scoped in SpannableMeasureArgs args)
    {
        var effMaxSize = Vector2.Max(this.minSize, args.MaxSize);

        var rv = new RectVector4(
            Vector2.Zero,
            Vector2.Clamp(
                ResolveSize(this.size, effMaxSize),
                ResolveSize(this.minSize, effMaxSize),
                ResolveSize(this.maxSize, effMaxSize)));

        var boundaryContentGap = (this.margin + this.padding) * this.Scale;
        rv = RectVector4.Shrink(rv, boundaryContentGap);
        rv = RectVector4.Normalize(rv);

        this.ImGuiGlobalId = args.ImGuiGlobalId;
        this.Scale = args.Scale;
        this.activeTextState = new(this.textStateOptions, args.TextState);

        if (rv.Right >= 1000000f)
            rv.Right = float.PositiveInfinity;
        if (rv.Bottom >= 1000000f)
            rv.Bottom = float.PositiveInfinity;

        var contentMeasurementArgs = args with
        {
            MinSize = Vector2.Max(args.MinSize, ResolveSize(this.minSize, effMaxSize)) - boundaryContentGap.Size,
            MaxSize = Vector2.Min(effMaxSize, ResolveSize(this.maxSize, effMaxSize)) - boundaryContentGap.Size,
        };
        contentMeasurementArgs.SuggestedSize =
            Vector2.Clamp(
                ResolveSize(this.size, effMaxSize) - boundaryContentGap.Size,
                contentMeasurementArgs.MinSize,
                contentMeasurementArgs.MaxSize);

        this.measuredContentBox = this.MeasureContentBox(contentMeasurementArgs);
        this.measuredContentBox = RectVector4.Normalize(this.measuredContentBox);
        this.measuredContentBox = RectVector4.Translate(this.measuredContentBox, boundaryContentGap.LeftTop);

        this.measuredInteractiveBox = RectVector4.Expand(this.measuredContentBox, this.padding * this.Scale);
        this.boundary = RectVector4.Expand(this.measuredInteractiveBox, this.margin * this.Scale);
        this.measuredOutsideBox = RectVector4.Expand(this.boundary, this.extendOutside * this.Scale);

        if (this.wasVisible != this.Visible)
        {
            this.VisibilityAnimation?.Restart();
            this.wasVisible = this.Visible;
        }

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
        {
            visibilityAnimation.Update(this);
            this.measuredContentBox = RectVector4.Normalize(
                RectVector4.Expand(this.measuredContentBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.measuredInteractiveBox = RectVector4.Normalize(
                RectVector4.Expand(this.measuredInteractiveBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.boundary = RectVector4.Normalize(
                RectVector4.Expand(this.boundary, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.measuredOutsideBox = RectVector4.Normalize(
                RectVector4.Expand(this.measuredOutsideBox, visibilityAnimation.AnimatedBoundaryAdjustment));
        }

        return;

        static Vector2 ResolveSize(in Vector2 dim, in Vector2 maxSize)
        {
            return new(
                dim.X switch
                {
                    MatchParent or WrapContent => maxSize.X,
                    _ => dim.X,
                },
                dim.Y switch
                {
                    MatchParent or WrapContent => maxSize.Y,
                    _ => dim.Y,
                });
        }
    }

    /// <inheritdoc/>
    public void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
    {
        this.InnerOrigin = args.InnerOrigin;

        this.transformation = Matrix4x4.Identity;

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
            this.transformation = visibilityAnimation.AnimatedTransformation;

        this.transformation = Matrix4x4.Multiply(
            this.transformation,
            Matrix4x4.CreateTranslation(new(-this.boundary.RightBottom * args.InnerOrigin, 0)));

        if (this.moveAnimation is not null)
        {
            this.moveAnimation.Update(this);
            if (this.moveAnimation.AfterMatrix != args.Transformation)
            {
                this.moveAnimation.BeforeMatrix = this.moveAnimation.AnimatedTransformation;
                this.moveAnimation.AfterMatrix = args.Transformation;
                this.moveAnimation.Restart();
                this.moveAnimation.Update(this);
            }

            this.transformation = Matrix4x4.Multiply(
                this.transformation,
                this.moveAnimation.IsRunning ? this.moveAnimation.AnimatedTransformation : args.Transformation);
        }
        else
        {
            this.transformation = Matrix4x4.Multiply(this.transformation, args.Transformation);
        }

        this.transformation = Matrix4x4.Multiply(
            this.transformation,
            Matrix4x4.CreateTranslation(new(this.boundary.RightBottom * args.InnerOrigin, 0)));

        this.OnCommitMeasurement(
            new()
            {
                Sender = this,
                SpannableArgs = args with
                {
                    Transformation = Matrix4x4.Identity,
                },
            });
    }

    /// <inheritdoc/>
    public void DrawSpannable(SpannableDrawArgs args)
    {
        if (!this.Visible && this.HideAnimation?.IsRunning is not true)
            return;

        var opacity = 1f;
        opacity *= this.VisibilityAnimation?.AnimatedOpacity ?? 1f;

        if (this.currentBackground is not null && this.currentBackgroundPass is not null)
        {
            using (ScopedTransformer.From(args, opacity))
                args.NotifyChild(this.currentBackground, this.currentBackgroundPass);
        }

        opacity *= this.enabled ? 1f : this.disabledTextOpacity;
        using (ScopedTransformer.From(args, opacity))
        {
            args.SwitchToChannel(RenderChannel.ForeChannel);
            this.OnDraw(new() { Sender = this, SpannableArgs = args });
        }

        using (ScopedTransformer.From(args, 1f))
            args.DrawListPtr.AddRect(this.Boundary.LeftTop, this.Boundary.RightBottom, 0x20FFFFFF);
    }

    /// <inheritdoc/>
    public void HandleSpannableInteraction(
        scoped in SpannableHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        this.OnHandleInteraction(new() { Sender = this, SpannableArgs = args }, out link);

        ISpannable? newBackground;
        if (!this.visible && this.hideAnimation?.IsRunning is not true)
        {
            this.IsMouseHovered = false;
            this.heldMouseButtons = 0;

            newBackground = this.normalBackground;
        }
        else if (!this.Enabled)
        {
            this.IsMouseHovered = false;
            this.heldMouseButtons = 0;

            newBackground = this.disabledBackground ?? this.normalBackground;
        }
        else
        {
            var margs = new ControlMouseEventArgs { LocalLocation = args.MouseLocalLocation };
            var interceptWheel = false;
            interceptWheel |= args.WheelDelta.X > 0 && this.interceptMouseWheelRight;
            interceptWheel |= args.WheelDelta.X < 0 && this.interceptMouseWheelLeft;
            interceptWheel |= args.WheelDelta.Y > 0 && this.interceptMouseWheelDown;
            interceptWheel |= args.WheelDelta.Y < 0 && this.interceptMouseWheelUp;

            if (this.captureMouseOnMouseDown)
            {
                if (this.heldMouseButtons != 0)
                    args.SetActive(this.selfInnerId, interceptWheel);
            }

            var hovered = this.measuredInteractiveBox.Contains(margs.LocalLocation) &&
                          args.IsItemHoverable(this.selfInnerId);
            if (hovered != this.IsMouseHovered)
            {
                this.IsMouseHovered = hovered;
                if (hovered)
                    this.OnMouseEnter(margs);
                else
                    this.OnMouseLeave(margs);
            }

            if (args.WheelDelta != Vector2.Zero)
                this.OnMouseWheel(margs with { Delta = args.WheelDelta });

            if (this.lastMouseLocation != margs.LocalLocation)
            {
                this.lastMouseLocation = margs.LocalLocation;
                this.OnMouseMove(margs);
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
                        this.heldMouseButtons |= 1 << i;
                        if (hovered)
                            this.OnMouseDown(margs with { Button = MouseButtonsWeCare[i] });
                    }
                    else
                    {
                        this.heldMouseButtons &= ~(1 << i);
                        if (hovered)
                        {
                            this.OnMouseUp(margs with { Button = MouseButtonsWeCare[i] });

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

            if (this.captureMouseOnMouseDown)
            {
                if ((this.heldMouseButtons != 0) != (lastHeldMouseButtons != 0))
                {
                    ImGui.SetNextFrameWantCaptureMouse(this.heldMouseButtons != 0);
                    if (this.heldMouseButtons == 0)
                        args.ClearActive();
                }

                if (this.IsMouseHovered)
                    args.SetHovered(this.selfInnerId, interceptWheel);

                if (this.heldMouseButtons != 0)
                    args.SetActive(this.selfInnerId, interceptWheel);
            }

            if (this.IsMouseHovered && this.IsLeftMouseButtonDown && this.ActiveBackground is not null)
                newBackground = this.ActiveBackground;
            else if (this.IsMouseHovered && this.HoveredBackground is not null)
                newBackground = this.HoveredBackground;
            else
                newBackground = this.NormalBackground;
        }

        if (!ReferenceEquals(this.currentBackground, newBackground))
        {
            this.currentBackground?.ReturnRenderPass(this.currentBackgroundPass);
            this.currentBackgroundPass = null;
            this.currentBackground = newBackground;
        }

        if (this.currentBackground is not null)
        {
            this.currentBackgroundPass ??= this.currentBackground.RentRenderPass(this.Renderer);
            this.currentBackgroundPass.MeasureSpannable(
                new(
                    this.currentBackground,
                    this.currentBackgroundPass,
                    this.MeasuredInteractiveBox.Size,
                    this.MeasuredInteractiveBox.Size,
                    this.Scale,
                    this.ActiveTextState,
                    this.GetGlobalIdFromInnerId(this.backgroundInnerId)));
            new SpannableCommitTransformationArgs(
                    args.Sender,
                    args.RenderPass,
                    this.InnerOrigin,
                    this.transformation)
                .NotifyChild(
                    this.currentBackground,
                    this.currentBackgroundPass,
                    this.MeasuredInteractiveBox.LeftTop,
                    Matrix4x4.Identity);
            if (link.IsEmpty)
                args.NotifyChild(this.currentBackground, this.currentBackgroundPass, out link);
            else
                args.NotifyChild(this.currentBackground, this.currentBackgroundPass, out _);
        }
    }

    /// <inheritdoc/>
    public int SerializeState(Span<byte> buffer) =>
        SpannableSerializationHelper.Write(ref buffer, this.GetAllChildSpannables());

    /// <inheritdoc/>
    public bool TryDeserializeState(ReadOnlySpan<byte> buffer, out int consumed)
    {
        var origLen = buffer.Length;
        consumed = 0;
        if (!SpannableSerializationHelper.TryRead(ref buffer, this.GetAllChildSpannables()))
            return false;
        consumed += origLen - buffer.Length;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.GetType().Name}({this.text})";

    /// <summary>Disposes this instance of <see cref="ControlSpannable"/>.</summary>
    /// <param name="disposing">Whether it is being called from <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (ref var s in CollectionsMarshal.AsSpan(this.AllSpannables))
            {
                s?.Dispose();
                s = null;
            }

            this.currentBackground = null;
            this.normalBackground = null;
            this.hoveredBackground = null;
            this.activeBackground = null;
            this.disabledBackground = null;
        }
    }

    /// <summary>Measures the content box, given the available content box excluding the margin and padding.</summary>
    /// <param name="args">Measure arguments.</param>
    /// <returns>The resolved content box, relative to the content box origin.</returns>
    /// <remarks>Right and bottom values can be unbound (<see cref="float.PositiveInfinity"/>).</remarks>
    protected virtual RectVector4 MeasureContentBox(SpannableMeasureArgs args) =>
        RectVector4.FromCoordAndSize(
            Vector2.Zero,
            new(
                args.SuggestedSize.X >= float.PositiveInfinity ? 0 : args.SuggestedSize.X,
                args.SuggestedSize.Y >= float.PositiveInfinity ? 0 : args.SuggestedSize.Y));
}

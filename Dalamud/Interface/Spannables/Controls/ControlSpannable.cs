using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
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
    private RectVector4 scaledBoundary;
    private Matrix4x4 transformationFromParent;
    private Matrix4x4 transformationFromAncestors;
    private Matrix4x4 transformationFromParentDirectBefore;

    private RectVector4 measuredOutsideBox;
    private RectVector4 measuredBoundaryBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private Vector2 lastMouseLocation;
    private int heldMouseButtons;
    private long[] lastMouseClickTick = new long[MouseButtonsWeCare.Length];
    private int[] lastMouseClickCount = new int[MouseButtonsWeCare.Length];

    private bool suppressNextAnimation;
    private bool wasVisible;
    private bool wasFocused;
    private bool wasCapturingMouseViaProperty;

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

    /// <inheritdoc />
    ISpannable ISpannableRenderPass.RenderPassCreator => this;

    /// <inheritdoc/>
    public ref TextState ActiveTextState => ref this.activeTextState;

    /// <inheritdoc/>
    /// <remarks>This excludes <see cref="ExtendOutside"/>, but counts <see cref="Scale"/> in.</remarks>
    public ref readonly RectVector4 Boundary => ref this.scaledBoundary;

    /// <inheritdoc/>
    public Vector2 InnerOrigin { get; private set; }

    /// <inheritdoc/>
    public ref readonly Matrix4x4 TransformationFromParent => ref this.transformationFromParent;

    /// <inheritdoc/>
    public ref readonly Matrix4x4 TransformationFromAncestors => ref this.transformationFromAncestors;

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

    /// <summary>Gets the last measured outside box size.</summary>
    /// <remarks>Useful for drawing decoration outside the standard box, such as glow or shadow effects.<br />
    /// This does not count <see cref="Scale"/> in.</remarks>
    public RectVector4 MeasuredOutsideBox
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.measuredOutsideBox;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.HandlePropertyChange(
            nameof(this.MeasuredOutsideBox),
            ref this.measuredOutsideBox,
            value,
            this.OnMeasuredOutsideBoxChange);
    }

    /// <summary>Gets the measured boundary from <see cref="MeasureSpannable"/>.</summary>
    /// <remarks>This does not count <see cref="Scale"/> in.</remarks>
    public RectVector4 MeasuredBoundaryBox
    {
        get => this.measuredBoundaryBox;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.HandlePropertyChange(
            nameof(this.MeasuredBoundaryBox),
            ref this.measuredBoundaryBox,
            value,
            this.OnMeasuredBoundaryBoxChange);
    }

    /// <summary>Gets the last measured interactive box size.</summary>
    /// <remarks>This excludes <see cref="ExtendOutside"/> and <see cref="Margin"/>,
    /// and does not count <see cref="Scale"/> in.</remarks>
    public RectVector4 MeasuredInteractiveBox
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.measuredInteractiveBox;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.HandlePropertyChange(
            nameof(this.MeasuredInteractiveBox),
            ref this.measuredInteractiveBox,
            value,
            this.OnMeasuredInteractiveBoxChange);
    }

    /// <summary>Gets the last measured content box size.</summary>
    /// <remarks>This excludes <see cref="ExtendOutside"/>, <see cref="Margin"/>, and <see cref="Padding"/>,
    /// and does not count <see cref="Scale"/> in.</remarks>
    public RectVector4 MeasuredContentBox
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.measuredContentBox;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.HandlePropertyChange(
            nameof(this.MeasuredContentBox),
            ref this.measuredContentBox,
            value,
            this.OnMeasuredContentBoxChange);
    }

    /// <summary>Gets the effective scale from the current (or last, if outside) render cycle.</summary>
    public float EffectiveScale { get; private set; } = 1f;

    /// <summary>Gets a value indicating whether the width is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsWidthWrapContent => this.size.X == WrapContent;

    /// <summary>Gets a value indicating whether the height is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsHeightWrapContent => this.size.Y == WrapContent;

    /// <summary>Gets a value indicating whether the width is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsWidthMatchParent => this.size.X == MatchParent;

    /// <summary>Gets a value indicating whether the height is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsHeightMatchParent => this.size.Y == MatchParent;

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
        this.ImGuiGlobalId = args.ImGuiGlobalId;
        this.EffectiveScale = args.Scale * this.scale;
        this.activeTextState = new(this.textStateOptions, args.TextState);

        // Note: EffectiveScale is for preparing the source resources.
        // We deal only with our own scale here, as the outer scale is dealt by the caller.
        var myMinSize = args.MinSize * this.scale;
        var myMaxSize = args.MaxSize * this.scale;
        var boundaryContentGap = this.margin + this.padding;

        RectVector4 contentBox;
        if (this.IsWidthWrapContent && this.IsHeightWrapContent)
        {
            contentBox = this.MeasureContentBox(
                args with
                {
                    MinSize = Vector2.Max(this.MinSize, myMinSize - boundaryContentGap.Size),
                    SuggestedSize = new(float.PositiveInfinity),
                    MaxSize = Vector2.Min(this.MaxSize, myMaxSize - boundaryContentGap.Size),
                    Scale = this.EffectiveScale,
                });
        }
        else if (this.IsWidthWrapContent)
        {
            var h = Math.Max(0, (this.IsHeightMatchParent ? myMaxSize.Y : this.size.Y) - boundaryContentGap.Size.Y);
            contentBox = this.MeasureContentBox(
                args with
                {
                    MinSize = new(
                        Math.Max(0, Math.Max(this.MinSize.X, myMinSize.X) - boundaryContentGap.Size.X),
                        h >= float.PositiveInfinity ? 0f : h),
                    SuggestedSize = new(float.PositiveInfinity, h),
                    MaxSize = new(Math.Max(0, Math.Min(this.MaxSize.X, myMaxSize.X) - boundaryContentGap.Size.X), h),
                    Scale = this.EffectiveScale,
                });
        }
        else if (this.IsHeightWrapContent)
        {
            var w = Math.Max(0, (this.IsWidthMatchParent ? myMaxSize.X : this.size.X) - boundaryContentGap.Size.X);
            contentBox = this.MeasureContentBox(
                args with
                {
                    MinSize = new(
                        w >= float.PositiveInfinity ? 0f : w,
                        Math.Max(0, Math.Max(this.MinSize.Y, myMinSize.Y) - boundaryContentGap.Size.Y)),
                    SuggestedSize = new(w, float.PositiveInfinity),
                    MaxSize = new(w, Math.Max(0, Math.Min(this.MaxSize.Y, myMaxSize.Y) - boundaryContentGap.Size.Y)),
                    Scale = this.EffectiveScale,
                });
        }
        else
        {
            var w = Math.Max(0, (this.IsWidthMatchParent ? myMaxSize.X : this.size.X) - boundaryContentGap.Size.X);
            var h = Math.Max(0, (this.IsHeightMatchParent ? myMaxSize.Y : this.size.Y) - boundaryContentGap.Size.Y);
            contentBox = this.MeasureContentBox(
                args with
                {
                    MinSize = new(w >= float.PositiveInfinity ? 0f : w, h >= float.PositiveInfinity ? 0f : h),
                    SuggestedSize = new(w, h),
                    MaxSize = new(w, h),
                    Scale = this.EffectiveScale,
                });
        }

        contentBox = RectVector4.Normalize(contentBox);
        contentBox = RectVector4.Translate(contentBox, boundaryContentGap.LeftTop);

        var interactiveBox = RectVector4.Expand(contentBox, this.padding);
        var boundaryBox = RectVector4.Expand(interactiveBox, this.margin);
        var outsideBox = RectVector4.Expand(boundaryBox, this.extendOutside);

        if (this.wasVisible != this.visible)
        {
            this.VisibilityAnimation?.Restart();
            this.wasVisible = this.visible;
        }

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
        {
            visibilityAnimation.Update(this);
            contentBox = RectVector4.Normalize(
                RectVector4.Expand(contentBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            interactiveBox = RectVector4.Normalize(
                RectVector4.Expand(interactiveBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            boundaryBox = RectVector4.Normalize(
                RectVector4.Expand(boundaryBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            outsideBox = RectVector4.Normalize(
                RectVector4.Expand(outsideBox, visibilityAnimation.AnimatedBoundaryAdjustment));
        }

        this.MeasuredContentBox = contentBox;
        this.MeasuredInteractiveBox = interactiveBox;
        this.MeasuredBoundaryBox = boundaryBox;
        this.MeasuredOutsideBox = outsideBox;
        this.scaledBoundary = boundaryBox * this.scale;

        if (this.currentBackground is not null)
        {
            this.currentBackgroundPass ??= this.currentBackground.RentRenderPass(this.Renderer);
            args.NotifyChild(
                this.currentBackgroundPass,
                this.backgroundInnerId,
                args with
                {
                    Scale = this.EffectiveScale,
                    MinSize = this.MeasuredInteractiveBox.Size,
                    MaxSize = this.MeasuredInteractiveBox.Size,
                    TextState = this.ActiveTextState.Fork(),
                });
        }
    }

    /// <inheritdoc/>
    public void CommitSpannableMeasurement(scoped in SpannableCommitMeasurementArgs args)
    {
        this.InnerOrigin = args.InnerOrigin;

        this.transformationFromParent = Matrix4x4.Identity;

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
            this.transformationFromParent = visibilityAnimation.AnimatedTransformation;

        this.transformationFromParent = Matrix4x4.Multiply(
            this.transformationFromParent,
            Matrix4x4.CreateTranslation(new(-this.measuredBoundaryBox.RightBottom * args.InnerOrigin, 0)));

        if (MathF.Abs(this.scale - 1f) > 0.00001f)
        {
            this.transformationFromParent = Matrix4x4.Multiply(
                this.transformationFromParent,
                Matrix4x4.CreateScale(this.scale));
        }

        this.transformationChangeAnimation?.Update(this);
        if (this.transformationChangeAnimation?.IsRunning is true)
        {
            this.transformationFromParent = Matrix4x4.Multiply(
                this.transformationFromParent,
                this.transformationChangeAnimation.AnimatedTransformation);
        }
        else if (!this.transformation.IsIdentity)
        {
            this.transformationFromParent = Matrix4x4.Multiply(
                this.transformationFromParent,
                this.transformation);
        }

        if (this.moveAnimation is not null)
        {
            this.moveAnimation.Update(this);
            if (this.moveAnimation.AfterMatrix != args.TransformationFromParent)
            {
                this.moveAnimation.AfterMatrix = args.TransformationFromParent;

                if (!this.suppressNextAnimation && this.transformationFromParentDirectBefore != default)
                {
                    this.moveAnimation.BeforeMatrix
                        = this.moveAnimation.IsRunning
                              ? this.moveAnimation.AnimatedTransformation
                              : this.transformationFromParentDirectBefore;
                    this.moveAnimation.Restart();
                }

                this.moveAnimation.Update(this);
            }

            this.transformationFromParent = Matrix4x4.Multiply(
                this.transformationFromParent,
                this.moveAnimation.IsRunning
                    ? this.moveAnimation.AnimatedTransformation
                    : args.TransformationFromParent);
        }
        else
        {
            this.transformationFromParent = Matrix4x4.Multiply(
                this.transformationFromParent,
                args.TransformationFromParent);
        }

        this.suppressNextAnimation = false;

        if (this.visible)
            this.transformationFromParentDirectBefore = args.TransformationFromParent;

        this.transformationFromParent = Matrix4x4.Multiply(
            this.transformationFromParent,
            Matrix4x4.CreateTranslation(new(this.measuredBoundaryBox.RightBottom * args.InnerOrigin, 0)));

        this.transformationFromAncestors =
            Matrix4x4.Multiply(
                Matrix4x4.Invert(args.TransformationFromParent, out var inverted)
                    ? inverted
                    : Matrix4x4.Identity,
                args.TransformationFromAncestors);
        this.transformationFromAncestors = Matrix4x4.Multiply(
            this.transformationFromParent,
            this.transformationFromAncestors);

        if (this.currentBackground is not null && this.currentBackgroundPass is not null)
        {
            args.NotifyChild(
                this.currentBackgroundPass,
                args,
                Matrix4x4.CreateTranslation(new(this.measuredInteractiveBox.LeftTop, 0)));
        }

        var e = SpannableControlEventArgsPool.Rent<ControlCommitMeasurementEventArgs>();
        e.Sender = this;
        e.SpannableArgs = args with
        {
            TransformationFromParent = Matrix4x4.Identity,
            TransformationFromAncestors = this.transformationFromAncestors,
        };
        this.OnCommitMeasurement(e);
        SpannableControlEventArgsPool.Return(e);
    }

    /// <inheritdoc/>
    public unsafe void DrawSpannable(SpannableDrawArgs args)
    {
        if (!this.Visible && this.HideAnimation?.IsRunning is not true)
            return;

        // Note: our temporary draw list uses EffectiveScale, because that's the scale that'll actualy be displayed on
        // the screen.
        // For inner transformation we use just Scale, because the scale from the parent will be dealt by the parent.

        var tmpDrawList = this.Renderer.RentDrawList(args.DrawListPtr.NativePtr);
        tmpDrawList._CmdHeader.ClipRect = this.measuredBoundaryBox.Vector4 * this.EffectiveScale;
        tmpDrawList.CmdBuffer[0].ClipRect = tmpDrawList._CmdHeader.ClipRect;
        try
        {
            if (this.currentBackground is not null && this.currentBackgroundPass is not null)
                args.NotifyChild(this.currentBackgroundPass, args with { DrawListPtr = tmpDrawList });

            using (new ScopedTransformer(
                       tmpDrawList,
                       Matrix4x4.Identity,
                       Vector2.One,
                       this.enabled ? 1f : this.disabledTextOpacity))
            {
                var e = SpannableControlEventArgsPool.Rent<ControlDrawEventArgs>();
                e.Sender = this;
                e.SpannableArgs = args with { DrawListPtr = tmpDrawList };
                this.OnDraw(e);
                SpannableControlEventArgsPool.Return(e);
            }

            var opacity = this.VisibilityAnimation?.AnimatedOpacity ?? 1f;
            if (this.clipChildren)
            {
                if (this.Renderer.RentDrawListTexture(
                        tmpDrawList,
                        this.measuredInteractiveBox,
                        Vector4.Zero,
                        new(this.EffectiveScale),
                        out var uvrc) is
                    { } dlt)
                {
                    args.DrawListPtr.AddImageQuad(
                        dlt.ImGuiHandle,
                        Vector2.Transform(this.measuredInteractiveBox.LeftTop, this.transformationFromParent),
                        Vector2.Transform(this.measuredInteractiveBox.RightTop, this.transformationFromParent),
                        Vector2.Transform(this.measuredInteractiveBox.RightBottom, this.transformationFromParent),
                        Vector2.Transform(this.measuredInteractiveBox.LeftBottom, this.transformationFromParent),
                        uvrc.LeftTop,
                        uvrc.RightTop,
                        uvrc.RightBottom,
                        uvrc.LeftBottom,
                        new Rgba32(new Vector4(1, 1, 1, opacity)));
                    this.Renderer.ReturnDrawListTexture(dlt);
                }
            }
            else
            {
                tmpDrawList.CopyDrawListDataTo(args.DrawListPtr, this.transformationFromParent, new(1, 1, 1, opacity));
            }
        }
        finally
        {
            this.Renderer.ReturnDrawList(tmpDrawList);
        }

        // TODO: testing
        using (ScopedTransformer.From(args, Vector2.One, 1f))
        {
            args.DrawListPtr.AddRect(
                this.MeasuredBoundaryBox.LeftTop,
                this.MeasuredBoundaryBox.RightBottom,
                0x20FFFFFF);
            if (this.IsMouseHovered)
            {
                args.DrawListPtr.AddCircle(this.lastMouseLocation, 3, 0x407777FF);
                ImGui.SetTooltip($"{this.Name}: {this.scale:g}x\n{this.MeasuredBoundaryBox}\n{this.Boundary}");
            }
        }

        // TODO: make better focus indicator
        if (this.wasFocused)
        {
            using (ScopedTransformer.From(args, new(this.scale), 1f))
            {
                args.DrawListPtr.AddRect(
                    this.Boundary.LeftTop,
                    this.Boundary.RightBottom,
                    0x6033BB33,
                    0,
                    ImDrawFlags.None,
                    ImGuiInternals.ImGuiContext.Instance.ActiveId == this.GetGlobalIdFromInnerId(this.selfInnerId)
                        ? 2f
                        : 1f);
            }
        }
    }

    /// <inheritdoc/>
    public unsafe void HandleSpannableInteraction(
        scoped in SpannableHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        var io = ImGui.GetIO().NativePtr;

        // This is, in fact, not a new vector
        // ReSharper disable once CollectionNeverUpdated.Local
        var inputQueueCharacters = new ImVectorWrapper<char>(&io->InputQueueCharacters);
        var inputEventsTrail = new ImVectorWrapper<ImGuiInternals.ImGuiInputEvent>(
            (ImVector*)Unsafe.AsPointer(ref ImGuiInternals.ImGuiContext.Instance.InputEventsTrail));

        var chiea = SpannableControlEventArgsPool.Rent<ControlHandleInteractionEventArgs>();
        chiea.Sender = this;
        chiea.SpannableArgs = args;
        this.OnHandleInteraction(chiea, out link);
        SpannableControlEventArgsPool.Return(chiea);

        var cmea = SpannableControlEventArgsPool.Rent<ControlMouseEventArgs>();
        cmea.Handled = false;
        cmea.Sender = this;
        cmea.LocalLocation = args.MouseLocalLocation;
        cmea.LocalLocationDelta = cmea.LocalLocation - this.lastMouseLocation;
        cmea.WheelDelta = args.WheelDelta;
        var hoveredOnRect = this.HitTest(cmea.LocalLocation);

        args.ItemAdd(
            this.selfInnerId,
            this.measuredInteractiveBox,
            this.measuredInteractiveBox,
            this.measuredOutsideBox,
            hoveredOnRect,
            !this.focusable,
            false,
            !this.enabled);

        var hovered = args.IsItemHoverable(
            this.measuredInteractiveBox * this.scale,
            this.captureMouseOnMouseDown ? this.selfInnerId : -1);
        var focused = ImGui.IsItemFocused();

        ISpannable? newBackground;
        if (!this.visible && this.hideAnimation?.IsRunning is not true)
        {
            this.IsMouseHovered = false;
            this.heldMouseButtons = 0;

            newBackground = this.normalBackground;
            focused = false;
        }
        else if (!this.Enabled)
        {
            this.IsMouseHovered = false;
            this.heldMouseButtons = 0;

            newBackground = this.disabledBackground ?? this.normalBackground;
            focused = false;
        }
        else
        {
            if (hovered != this.IsMouseHovered)
            {
                this.IsMouseHovered = hovered;

                if (hovered)
                {
                    args.SetHovered(this.selfInnerId, this.captureMouseWheel);
                    cmea.Handled = false;
                    this.OnMouseEnter(cmea);
                }
                else
                {
                    cmea.Handled = false;
                    this.OnMouseLeave(cmea);
                }
            }

            if (args.WheelDelta != Vector2.Zero)
            {
                cmea.Handled = false;
                this.OnMouseWheel(cmea);
            }

            if (this.lastMouseLocation != cmea.LocalLocation)
            {
                this.lastMouseLocation = cmea.LocalLocation;
                cmea.Handled = false;
                this.OnMouseMove(cmea);
            }

            var lastHeldMouseButtons = this.heldMouseButtons;
            if (lastHeldMouseButtons != 0 || hovered)
            {
                for (var i = 0; i < MouseButtonsWeCare.Length; i++)
                {
                    var held = args.IsMouseButtonDown(MouseButtonsWeCare[i]);
                    if (held == ((this.heldMouseButtons & (1 << i)) != 0))
                        continue;

                    cmea.Button = MouseButtonsWeCare[i];
                    if (held)
                    {
                        this.heldMouseButtons |= 1 << i;
                        if (hovered)
                        {
                            cmea.Handled = false;
                            this.OnMouseDown(cmea);
                        }

                        if (this.focusable)
                            focused = true;
                    }
                    else
                    {
                        this.heldMouseButtons &= ~(1 << i);
                        if (hovered || this.captureMouse || this.captureMouseOnMouseDown)
                        {
                            cmea.Handled = false;
                            this.OnMouseUp(cmea);

                            if (hovered)
                            {
                                if (this.lastMouseClickTick[i] < Environment.TickCount64)
                                    this.lastMouseClickCount[i] = 1;
                                else
                                    this.lastMouseClickCount[i] += 1;
                                this.lastMouseClickTick[i] = Environment.TickCount64 + GetDoubleClickTime();
                                cmea.Clicks = this.lastMouseClickCount[i];
                                cmea.Handled = false;
                                this.OnMouseClick(cmea);
                            }
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

                if (this.heldMouseButtons != 0)
                    args.SetActive(this.selfInnerId, this.captureMouseWheel);
            }

            if (this.captureMouse != this.wasCapturingMouseViaProperty)
            {
                this.wasCapturingMouseViaProperty = this.captureMouse;
                ImGui.SetNextFrameWantCaptureMouse(this.captureMouse);
                if (this.captureMouse)
                    args.SetActive(this.selfInnerId, true);
                else
                    args.ClearActive();
            }

            if (hoveredOnRect && this.captureMouseWheel)
                args.SetHovered(-1, true);

            if (this.IsMouseHovered && this.IsLeftMouseButtonDown && this.ActiveBackground is not null)
                newBackground = this.ActiveBackground;
            else if (this.IsMouseHovered && this.HoveredBackground is not null)
                newBackground = this.HoveredBackground;
            else
                newBackground = this.NormalBackground;
        }

        if (!this.focusable && focused)
            focused = false;
        if (focused)
        {
            if (this.takeKeyboardInputsOnFocus)
            {
                inputQueueCharacters.Clear();
                io->WantTextInput = 1;

                foreach (ref var trailedEvent in inputEventsTrail.DataSpan)
                {
                    switch (trailedEvent.Type)
                    {
                        case ImGuiInternals.ImGuiInputEventType.Key:
                        {
                            if (this.ProcessCmdKey(trailedEvent.Key.Key))
                                ImGuiInternals.ImGuiNavMoveRequestCancel();

                            var kpe = SpannableControlEventArgsPool.Rent<ControlKeyEventArgs>();
                            kpe.Sender = this;
                            kpe.Handled = false;
                            kpe.KeyCode = trailedEvent.Key.Key;
                            kpe.Control = io->KeyCtrl != 0;
                            kpe.Alt = io->KeyAlt != 0;
                            kpe.Shift = io->KeyShift != 0;
                            kpe.Modifiers = io->KeyMods;
                            if (trailedEvent.Key.Down != 0)
                                this.OnKeyDown(kpe);
                            else
                                this.OnKeyUp(kpe);
                            SpannableControlEventArgsPool.Return(kpe);
                            break;
                        }

                        case ImGuiInternals.ImGuiInputEventType.Text:
                        {
                            var kpe = SpannableControlEventArgsPool.Rent<ControlKeyPressEventArgs>();
                            kpe.Sender = this;
                            kpe.Handled = false;
                            kpe.Rune =
                                Rune.TryCreate(trailedEvent.Text.Char, out var rune)
                                    ? rune
                                    : Rune.ReplacementChar;
                            kpe.KeyChar = unchecked((char)rune.Value);
                            this.OnKeyPress(kpe);
                            SpannableControlEventArgsPool.Return(kpe);
                            break;
                        }
                    }
                }
            }
        }

        if (focused != this.wasFocused)
        {
            this.wasFocused = focused;

            var cea = SpannableControlEventArgsPool.Rent<SpannableControlEventArgs>();
            cea.Sender = this;
            if (focused)
            {
                args.SetFocused(this.selfInnerId);
                this.OnGotFocus(cea);
            }
            else
            {
                this.OnLostFocus(cea);
            }

            SpannableControlEventArgsPool.Return(cea);
        }

        SpannableControlEventArgsPool.Return(cmea);

        if (!ReferenceEquals(this.currentBackground, newBackground))
        {
            this.currentBackground?.ReturnRenderPass(this.currentBackgroundPass);
            this.currentBackgroundPass = null;
            this.currentBackground = newBackground;
        }

        if (this.currentBackground is not null && this.currentBackgroundPass is not null)
        {
            if (link.IsEmpty)
                args.NotifyChild(this.currentBackgroundPass, args, out link);
            else
                args.NotifyChild(this.currentBackgroundPass, args, out _);
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

    /// <summary>Tests if this control can be interacted with mouse cursor at the given local coordinates.</summary>
    /// <param name="localLocation">The local coordinates.</param>
    /// <returns><c>true</c> if it is.</returns>
    public virtual bool HitTest(Vector2 localLocation) => this.measuredInteractiveBox.Contains(localLocation);

    /// <summary>Suppresses starting the move animation for the next render cycle.</summary>
    /// <param name="recursive">Whether to suppress recursively.</param>
    public void SuppressNextAnimation(bool recursive = true)
    {
        this.suppressNextAnimation = true;
        if (!recursive)
            return;
        foreach (var f in this.EnumerateHierarchy<ControlSpannable>())
            f.SuppressNextAnimation(false);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        this.text is null
            ? $"{this.GetType().Name}#{this.Name}"
            : $"{this.GetType().Name}#{this.Name}: {this.text}";

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

    /// <summary>Processes a command key.</summary>
    /// <param name="key">One of the <see cref="ImGuiKey"/> values that represents the key to process.</param>
    /// <returns><c>true</c> if the character was processed by the control; otherwise, <c>false</c>.</returns>
    protected virtual bool ProcessCmdKey(ImGuiKey key) => false;

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

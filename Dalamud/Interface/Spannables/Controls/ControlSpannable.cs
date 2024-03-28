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
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public partial class ControlSpannable : ISpannable, ISpannableMeasurement, ISpannableSerializable
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

    private readonly SpannableMeasurementOptions optionsFromParent;

    private readonly long[] lastMouseClickTick = new long[MouseButtonsWeCare.Length];
    private readonly int[] lastMouseClickCount = new int[MouseButtonsWeCare.Length];

    private ISpannable? currentBackground;
    private ISpannableMeasurement? currentBackgroundMeasurement;

    private Matrix4x4 localTransformation;
    private Matrix4x4 fullTransformation;
    private Matrix4x4 localTransformationDirectBefore;

    private RectVector4 measuredOutsideBox;
    private RectVector4 measuredBoundaryBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private Vector2 lastMouseLocation;
    private int heldMouseButtons;

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
        this.optionsFromParent = new();
        this.optionsFromParent.PropertyChanged += _ => this.IsMeasurementValid = false;
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
        this.AllSpannables.Add(null);
    }

    /// <inheritdoc />
    ISpannable ISpannableMeasurement.Spannable => this;

    /// <inheritdoc/>
    public ISpannableRenderer Renderer { get; private set; } = null!;

    /// <inheritdoc/>
    public bool IsMeasurementValid { get; private set; }

    /// <inheritdoc/>
    /// <remarks>This excludes <see cref="ExtendOutside"/>, but counts <see cref="Scale"/> in.</remarks>
    public RectVector4 Boundary { get; private set; }

    /// <inheritdoc/>
    ISpannableMeasurementOptions ISpannableMeasurement.Options => this.optionsFromParent;

    /// <summary>Gets a read-only reference of the local transformation matrix.</summary>
    public ref readonly Matrix4x4 LocalTransformation => ref this.localTransformation;

    /// <inheritdoc/>
    public ref readonly Matrix4x4 FullTransformation => ref this.fullTransformation;

    /// <inheritdoc/>
    public uint ImGuiGlobalId { get; set; }

    /// <summary>Gets or sets the inner transform origin.</summary>
    public Vector2 InnerOrigin { get; set; } = new(0.5f);

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

    /// <summary>Gets the measured boundary.</summary>
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
    public float EffectiveRenderScale => this.scale * this.RenderScale;

    /// <summary>Gets a value indicating whether any animation is running.</summary>
    public virtual bool IsAnyAnimationRunning =>
        this.hideAnimation?.IsRunning is true
        || this.showAnimation?.IsRunning is true
        || this.showAnimation?.IsRunning is true
        || this.transformationChangeAnimation?.IsRunning is true;

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
    ISpannableMeasurement ISpannable.RentMeasurement(ISpannableRenderer renderer)
    {
        this.Renderer = renderer;

        // Spannable itself is a state. Return self.
        return this;
    }

    /// <inheritdoc/>
    void ISpannable.ReturnMeasurement(ISpannableMeasurement? pass) => this.Renderer = null!;

    /// <inheritdoc/>
    unsafe bool ISpannableMeasurement.HandleInteraction()
    {
        if (!this.Boundary.IsValid)
            return false;

        var io = ImGui.GetIO().NativePtr;

        // This is, in fact, not a new vector
        // ReSharper disable once CollectionNeverUpdated.Local
        var inputQueueCharacters = new ImVectorWrapper<char>(&io->InputQueueCharacters);
        var inputEventsTrail = new ImVectorWrapper<ImGuiInternals.ImGuiInputEvent>(
            (ImVector*)Unsafe.AsPointer(ref ImGuiInternals.ImGuiContext.Instance.InputEventsTrail));

        var chiea = SpannableControlEventArgsPool.Rent<SpannableControlEventArgs>();
        chiea.Sender = this;
        this.OnHandleInteraction(chiea);
        SpannableControlEventArgsPool.Return(chiea);

        var cmea = SpannableControlEventArgsPool.Rent<ControlMouseEventArgs>();
        cmea.Handled = false;
        cmea.Sender = this;
        cmea.LocalLocation = this.PointToClient(ImGui.GetMousePos());
        cmea.LocalLocationDelta = cmea.LocalLocation - this.lastMouseLocation;
        cmea.WheelDelta = new(io->MouseWheelH, io->MouseWheel);
        var hoveredOnRect = this.HitTest(cmea.LocalLocation);

        SpannableImGuiItem.ItemAdd(
            this,
            this.selfInnerId,
            this.measuredInteractiveBox,
            this.measuredInteractiveBox,
            this.measuredOutsideBox,
            hoveredOnRect,
            !this.focusable,
            false,
            !this.enabled);

        var hovered = SpannableImGuiItem.IsItemHoverable(
            this,
            cmea.LocalLocation,
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
                    SpannableImGuiItem.SetHovered(this, this.selfInnerId, this.captureMouseWheel);
                    cmea.Handled = false;
                    this.OnMouseEnter(cmea);
                }
                else
                {
                    cmea.Handled = false;
                    this.OnMouseLeave(cmea);
                }
            }

            if (cmea.WheelDelta != Vector2.Zero)
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
                    var held = ImGui.IsMouseDown(MouseButtonsWeCare[i]);
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
                        SpannableImGuiItem.ClearActive();
                }

                if (this.heldMouseButtons != 0)
                    SpannableImGuiItem.SetActive(this, this.selfInnerId, this.captureMouseWheel);
            }

            if (this.captureMouse != this.wasCapturingMouseViaProperty)
            {
                this.wasCapturingMouseViaProperty = this.captureMouse;
                ImGui.SetNextFrameWantCaptureMouse(this.captureMouse);
                if (this.captureMouse)
                    SpannableImGuiItem.SetActive(this, this.selfInnerId, true);
                else
                    SpannableImGuiItem.ClearActive();
            }

            if (hoveredOnRect && this.captureMouseWheel)
                SpannableImGuiItem.SetHovered(this, -1, true);

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
                SpannableImGuiItem.SetFocused(this, this.selfInnerId);
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
            this.currentBackground?.ReturnMeasurement(this.currentBackgroundMeasurement);
            this.currentBackgroundMeasurement = null;
            this.currentBackground = newBackground;
        }

        this.currentBackgroundMeasurement?.HandleInteraction();
        return true;
    }

    /// <inheritdoc/>
    bool ISpannableMeasurement.Measure()
    {
        if (!this.ShouldMeasureAgain())
            return false;

        // Note: EffectiveScale is for preparing the source resources.
        // We deal only with our own scale here, as the outer scale is dealt by the caller.
        var myMaxSize = this.optionsFromParent.Size * this.scale;
        var boundaryContentGap = this.margin + this.padding;

        var contentBox = this.MeasureContentBox(
            new(
                this.IsWidthWrapContent
                    ? float.PositiveInfinity
                    : Math.Max(0, (this.IsWidthMatchParent ? myMaxSize.X : this.size.X) - boundaryContentGap.Size.X),
                this.IsHeightWrapContent
                    ? float.PositiveInfinity
                    : Math.Max(0, (this.IsHeightMatchParent ? myMaxSize.Y : this.size.Y) - boundaryContentGap.Size.Y)));

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
        this.Boundary = boundaryBox * this.scale;

        if (this.currentBackground is not null)
        {
            this.currentBackgroundMeasurement ??= this.currentBackground.RentMeasurement(this.Renderer);
            this.currentBackgroundMeasurement.RenderScale = this.EffectiveRenderScale;
            this.currentBackgroundMeasurement.Options.Size = this.Boundary.Size;
            this.currentBackgroundMeasurement.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.backgroundInnerId);
            this.currentBackgroundMeasurement.Measure();
        }

        this.IsMeasurementValid = !this.IsAnyAnimationRunning;
        return true;
    }

    /// <inheritdoc/>
    void ISpannableMeasurement.UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
    {
        this.localTransformation = Matrix4x4.Identity;

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
            this.localTransformation = visibilityAnimation.AnimatedTransformation;

        this.localTransformation = Matrix4x4.Multiply(
            this.localTransformation,
            Matrix4x4.CreateTranslation(new(-this.measuredBoundaryBox.RightBottom * this.InnerOrigin, 0)));

        if (MathF.Abs(this.scale - 1f) > 0.00001f)
        {
            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                Matrix4x4.CreateScale(this.scale));
        }

        this.transformationChangeAnimation?.Update(this);
        if (this.transformationChangeAnimation?.IsRunning is true)
        {
            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                this.transformationChangeAnimation.AnimatedTransformation);
        }
        else if (!this.transformation.IsIdentity)
        {
            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                this.transformation);
        }

        if (this.moveAnimation is not null)
        {
            this.moveAnimation.Update(this);
            if (this.moveAnimation.AfterMatrix != local)
            {
                this.moveAnimation.AfterMatrix = local;

                if (!this.suppressNextAnimation && this.localTransformationDirectBefore != default)
                {
                    this.moveAnimation.BeforeMatrix
                        = this.moveAnimation.IsRunning
                              ? this.moveAnimation.AnimatedTransformation
                              : this.localTransformationDirectBefore;
                    this.moveAnimation.Restart();
                }

                this.moveAnimation.Update(this);
            }

            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                this.moveAnimation.IsRunning
                    ? this.moveAnimation.AnimatedTransformation
                    : local);
        }
        else
        {
            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                local);
        }

        this.suppressNextAnimation = false;

        if (this.visible)
            this.localTransformationDirectBefore = local;

        this.localTransformation = Matrix4x4.Multiply(
            this.localTransformation,
            Matrix4x4.CreateTranslation(new(this.measuredBoundaryBox.RightBottom * this.InnerOrigin, 0)));

        this.fullTransformation = Matrix4x4.Multiply(this.localTransformation, ancestral);

        this.currentBackgroundMeasurement?.UpdateTransformation(Matrix4x4.Identity, this.fullTransformation);

        var e = SpannableControlEventArgsPool.Rent<SpannableControlEventArgs>();
        e.Sender = this;
        this.OnUpdateTransformation(e);
        SpannableControlEventArgsPool.Return(e);
    }

    /// <inheritdoc/>
    unsafe void ISpannableMeasurement.Draw(ImDrawListPtr drawListPtr)
    {
        if (!this.Visible && this.HideAnimation?.IsRunning is not true)
            return;

        // Note: our temporary draw list uses EffectiveScale, because that's the scale that'll actualy be displayed on
        // the screen.
        // For inner transformation we use just Scale, because the scale from the parent will be dealt by the parent.

        var tmpDrawList = this.Renderer.RentDrawList(drawListPtr.NativePtr);
        tmpDrawList._CmdHeader.ClipRect = this.measuredBoundaryBox.Vector4;
        tmpDrawList.CmdBuffer[0].ClipRect = tmpDrawList._CmdHeader.ClipRect;
        try
        {
            this.currentBackgroundMeasurement?.Draw(tmpDrawList);

            using (new ScopedTransformer(
                       tmpDrawList,
                       Matrix4x4.Identity,
                       Vector2.One,
                       this.enabled ? 1f : this.disabledTextOpacity))
            {
                var e = SpannableControlEventArgsPool.Rent<ControlDrawEventArgs>();
                e.Sender = this;
                e.DrawListPtr = tmpDrawList;
                this.OnDraw(e);
                SpannableControlEventArgsPool.Return(e);
            }

            var opacity = this.VisibilityAnimation?.AnimatedOpacity ?? 1f;
            if (this.clipChildren)
            {
                if (this.Renderer.RentDrawListTexture(
                        tmpDrawList,
                        this.measuredBoundaryBox,
                        Vector4.Zero,
                        new(this.EffectiveRenderScale),
                        out var uvrc) is
                    { } dlt)
                {
                    drawListPtr.AddImageQuad(
                        dlt.ImGuiHandle,
                        Vector2.Transform(this.measuredBoundaryBox.LeftTop, this.localTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.RightTop, this.localTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.RightBottom, this.localTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.LeftBottom, this.localTransformation),
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
                tmpDrawList.CopyDrawListDataTo(drawListPtr, this.localTransformation, new(1, 1, 1, opacity));
            }
        }
        finally
        {
            this.Renderer.ReturnDrawList(tmpDrawList);
        }

        // TODO: testing
        using (new ScopedTransformer(drawListPtr, this.localTransformation, Vector2.One, 1f))
        {
            drawListPtr.AddRect(
                this.MeasuredBoundaryBox.LeftTop,
                this.MeasuredBoundaryBox.RightBottom,
                0x20FFFFFF);
            if (this.IsMouseHovered)
            {
                drawListPtr.AddCircle(this.lastMouseLocation, 3, 0x407777FF);
                ImGui.SetTooltip($"{this.Name}: {this.scale:g}x\n{this.MeasuredBoundaryBox}\n{this.Boundary}");
            }
        }

        // TODO: make better focus indicator
        if (this.wasFocused)
        {
            using (new ScopedTransformer(drawListPtr, this.localTransformation, Vector2.One, 1f))
            {
                drawListPtr.AddRect(
                    this.MeasuredBoundaryBox.LeftTop,
                    this.MeasuredBoundaryBox.RightBottom,
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
    void ISpannableMeasurement.ReturnMeasurementToSpannable()
    {
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

    /// <inheritdoc/>
    bool IResettable.TryReset() => false;

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

    /// <summary>Determines if <see cref="MeasureContentBox"/> should be called from
    /// <see cref="ISpannableMeasurement.Measure"/>.</summary>
    /// <returns><c>true</c> if it is.</returns>
    protected virtual bool ShouldMeasureAgain() => !this.IsMeasurementValid;

    /// <summary>Measures the content box, given the available content box excluding the margin and padding.</summary>
    /// <param name="suggestedSize">Suggested size of the content box.</param>
    /// <returns>The resolved content box, relative to the content box origin.</returns>
    /// <remarks>Right and bottom values can be unbound (<see cref="float.PositiveInfinity"/>).</remarks>
    protected virtual RectVector4 MeasureContentBox(Vector2 suggestedSize) =>
        RectVector4.FromCoordAndSize(
            Vector2.Zero,
            new(
                suggestedSize.X >= float.PositiveInfinity ? 0 : suggestedSize.X,
                suggestedSize.Y >= float.PositiveInfinity ? 0 : suggestedSize.Y));
}

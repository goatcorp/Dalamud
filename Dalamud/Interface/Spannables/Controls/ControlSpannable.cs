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
    private RectVector4 boundary;
    private Matrix4x4 transformationFromParent;
    private Matrix4x4 transformationFromAncestors;

    private RectVector4 measuredOutsideBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private Vector2 lastMouseLocation;
    private int heldMouseButtons;
    private long[] lastMouseClickTick = new long[MouseButtonsWeCare.Length];
    private int[] lastMouseClickCount = new int[MouseButtonsWeCare.Length];

    private bool suppressNextMoveAnimation;
    private bool wasVisible;
    private bool wasFocused;

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
    /// <remarks>Useful for drawing decoration outside the standard box, such as glow or shadow effects.</remarks>
    public ref readonly RectVector4 MeasuredOutsideBox => ref this.measuredOutsideBox;

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

    /// <summary>Gets a value indicating whether the width is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsWidthMatchParent => this.size.X == MatchParent;

    /// <summary>Gets a value indicating whether the height is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsHeightMatchParent => this.size.Y == MatchParent;

    /// <summary>Gets a value indicating whether the width is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsMinWidthWrapContent => this.minSize.X == WrapContent;

    /// <summary>Gets a value indicating whether the height is set to wrap content.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsMinHeightWrapContent => this.minSize.Y == WrapContent;

    /// <summary>Gets a value indicating whether the width is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsMinWidthMatchParent => this.minSize.X == MatchParent;

    /// <summary>Gets a value indicating whether the height is set to match parent.</summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Sentinel value")]
    protected bool IsMinHeightMatchParent => this.minSize.Y == MatchParent;

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
        this.ImGuiGlobalId = args.ImGuiGlobalId;
        this.Scale = args.Scale;
        this.activeTextState = new(this.textStateOptions, args.TextState);

        var cmargs = args with
        {
            MinSize = ResolveSize(this.minSize, args.MinSize, args.MaxSize, this.Scale),
            MaxSize = ResolveSize(this.maxSize, args.MaxSize, args.MaxSize, this.Scale),
            SuggestedSize = ResolveSize(this.size, args.MinSize, args.MaxSize, this.Scale),
        };
        
        var boundaryContentGap = (this.margin + this.padding) * this.Scale;
        cmargs.MinSize = Vector2.Max(Vector2.Zero, cmargs.MinSize - boundaryContentGap.Size);
        cmargs.MaxSize = Vector2.Max(Vector2.Zero, cmargs.MaxSize - boundaryContentGap.Size);
        cmargs.SuggestedSize = Vector2.Max(Vector2.Zero, cmargs.SuggestedSize - boundaryContentGap.Size);
        cmargs.SuggestedSize = Vector2.Clamp(cmargs.SuggestedSize, cmargs.MinSize, cmargs.MaxSize);

        this.measuredContentBox = this.MeasureContentBox(cmargs);

        this.measuredContentBox = RectVector4.Normalize(this.measuredContentBox);
        this.measuredContentBox = RectVector4.Translate(this.measuredContentBox, boundaryContentGap.LeftTop);

        this.measuredInteractiveBox = RectVector4.Expand(this.measuredContentBox, this.padding * this.Scale);
        this.boundary = RectVector4.Expand(this.measuredInteractiveBox, this.margin * this.Scale);
        this.measuredOutsideBox = RectVector4.Expand(this.boundary, this.extendOutside * this.Scale);

        if (this.wasVisible != this.visible)
        {
            this.VisibilityAnimation?.Restart();
            this.wasVisible = this.visible;
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

        if (this.currentBackground is not null)
        {
            this.currentBackgroundPass ??= this.currentBackground.RentRenderPass(this.Renderer);
            args.NotifyChild(
                    this.currentBackground,
                    this.currentBackgroundPass,
                    this.backgroundInnerId,
                    this.MeasuredInteractiveBox.Size,
                    this.MeasuredInteractiveBox.Size,
                    this.ActiveTextState);
        }

        return;

        static Vector2 ResolveSize(in Vector2 dim, in Vector2 minSize, in Vector2 maxSize, float scale)
        {
            return new(
                dim.X switch
                {
                    WrapContent => minSize.X,
                    MatchParent => maxSize.X,
                    float.PositiveInfinity => maxSize.X,
                    _ => dim.X * scale,
                },
                dim.Y switch
                {
                    WrapContent => minSize.Y,
                    MatchParent => maxSize.Y,
                    float.PositiveInfinity => maxSize.Y,
                    _ => dim.Y * scale,
                });
        }
    }

    /// <inheritdoc/>
    public void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
    {
        this.InnerOrigin = args.InnerOrigin;

        var transformationFromParentBefore = this.transformationFromParent;
        this.transformationFromParent = Matrix4x4.Identity;

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
            this.transformationFromParent = visibilityAnimation.AnimatedTransformation;

        this.transformationFromParent = Matrix4x4.Multiply(
            this.transformationFromParent,
            Matrix4x4.CreateTranslation(new(-this.boundary.RightBottom * args.InnerOrigin, 0)));

        if (this.moveAnimation is not null)
        {
            this.moveAnimation.Update(this);
            if (this.moveAnimation.AfterMatrix != args.TransformationFromParent)
            {
                this.moveAnimation.AfterMatrix = args.TransformationFromParent;

                if (!this.suppressNextMoveAnimation)
                {
                    this.moveAnimation.BeforeMatrix
                        = this.moveAnimation.IsRunning
                              ? this.moveAnimation.AnimatedTransformation
                              : transformationFromParentBefore;
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

        this.suppressNextMoveAnimation = false;

        this.transformationFromParent = Matrix4x4.Multiply(
            this.transformationFromParent,
            Matrix4x4.CreateTranslation(new(this.boundary.RightBottom * args.InnerOrigin, 0)));

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
                this.currentBackground,
                this.currentBackgroundPass,
                this.measuredInteractiveBox.LeftTop,
                Matrix4x4.Identity);
        }

        var e = ControlEventArgsPool.Rent<ControlCommitMeasurementEventArgs>();
        e.Sender = this;
        e.SpannableArgs = args with
        {
            TransformationFromParent = Matrix4x4.Identity,
            TransformationFromAncestors = this.transformationFromAncestors,
        };
        this.OnCommitMeasurement(e);
        ControlEventArgsPool.Return(e);
    }

    /// <inheritdoc/>
    public void DrawSpannable(SpannableDrawArgs args)
    {
        if (!this.Visible && this.HideAnimation?.IsRunning is not true)
            return;

        var tmpDrawList = this.Renderer.RentDrawList(args.DrawListPtr);
        tmpDrawList._CmdHeader.ClipRect = this.boundary.Vector4;
        tmpDrawList.CmdBuffer[0].ClipRect = this.boundary.Vector4;
        try
        {
            var tmpargs = args with { DrawListPtr = tmpDrawList };
            if (this.currentBackground is not null && this.currentBackgroundPass is not null)
                tmpargs.NotifyChild(this.currentBackground, this.currentBackgroundPass);

            using (new ScopedTransformer(
                       tmpargs.DrawListPtr,
                       Matrix4x4.Identity,
                       this.enabled ? 1f : this.disabledTextOpacity))
            {
                var e = ControlEventArgsPool.Rent<ControlDrawEventArgs>();
                e.Sender = this;
                e.SpannableArgs = tmpargs;
                this.OnDraw(e);
                ControlEventArgsPool.Return(e);
            }

            var opacity = this.VisibilityAnimation?.AnimatedOpacity ?? 1f;
            var b = this.measuredInteractiveBox;
            if (this.clipChildren)
            {
                if (this.Renderer.RentDrawListTexture(tmpDrawList, b, Vector4.Zero, Vector2.One, out var uvrc) is
                    { } dlt)
                {
                    args.DrawListPtr.AddImageQuad(
                        dlt.ImGuiHandle,
                        Vector2.Transform(b.LeftTop, this.transformationFromParent),
                        Vector2.Transform(b.RightTop, this.transformationFromParent),
                        Vector2.Transform(b.RightBottom, this.transformationFromParent),
                        Vector2.Transform(b.LeftBottom, this.transformationFromParent),
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
        using (ScopedTransformer.From(args, 1f))
        {
            args.DrawListPtr.AddRect(this.Boundary.LeftTop, this.Boundary.RightBottom, 0x20FFFFFF);
        }

        // TODO: make better focus indicator
        if (this.wasFocused)
        {
            using (ScopedTransformer.From(args, 1f))
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

        var chiea = ControlEventArgsPool.Rent<ControlHandleInteractionEventArgs>();
        chiea.Sender = this;
        chiea.SpannableArgs = args;
        this.OnHandleInteraction(chiea, out link);
        ControlEventArgsPool.Return(chiea);

        var cmea = ControlEventArgsPool.Rent<ControlMouseEventArgs>();
        cmea.Sender = this;
        cmea.LocalLocation = args.MouseLocalLocation;
        cmea.LocalLocationDelta = cmea.LocalLocation - this.lastMouseLocation;
        cmea.WheelDelta = args.WheelDelta;
        var hoveredOnRect = this.measuredInteractiveBox.Contains(cmea.LocalLocation);

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
            this.measuredInteractiveBox,
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
                    this.OnMouseEnter(cmea);
                }
                else
                {
                    this.OnMouseLeave(cmea);
                }
            }

            if (args.WheelDelta != Vector2.Zero)
                this.OnMouseWheel(cmea);

            if (this.lastMouseLocation != cmea.LocalLocation)
            {
                this.lastMouseLocation = cmea.LocalLocation;
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
                            this.OnMouseDown(cmea);

                        if (this.focusable)
                            focused = true;
                    }
                    else
                    {
                        this.heldMouseButtons &= ~(1 << i);
                        if (hovered)
                        {
                            this.OnMouseUp(cmea);

                            if (this.lastMouseClickTick[i] < Environment.TickCount64)
                                this.lastMouseClickCount[i] = 1;
                            else
                                this.lastMouseClickCount[i] += 1;
                            this.lastMouseClickTick[i] = Environment.TickCount64 + GetDoubleClickTime();
                            this.OnMouseClick(
                                cmea with
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

                if (this.heldMouseButtons != 0)
                    args.SetActive(this.selfInnerId, this.captureMouseWheel);
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

                            var kpe = ControlEventArgsPool.Rent<ControlKeyEventArgs>();
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
                            ControlEventArgsPool.Return(kpe);
                            break;
                        }

                        case ImGuiInternals.ImGuiInputEventType.Text:
                        {
                            var kpe = ControlEventArgsPool.Rent<ControlKeyPressEventArgs>();
                            kpe.Sender = this;
                            kpe.Handled = false;
                            kpe.Rune =
                                Rune.TryCreate(trailedEvent.Text.Char, out var rune)
                                    ? rune
                                    : Rune.ReplacementChar;
                            kpe.KeyChar = unchecked((char)rune.Value);
                            this.OnKeyPress(kpe);
                            ControlEventArgsPool.Return(kpe);
                            break;
                        }
                    }
                }
            }
        }

        if (focused != this.wasFocused)
        {
            this.wasFocused = focused;

            var cea = ControlEventArgsPool.Rent<SpannableControlEventArgs>();
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

            ControlEventArgsPool.Return(cea);
        }

        ControlEventArgsPool.Return(cmea);

        if (!ReferenceEquals(this.currentBackground, newBackground))
        {
            this.currentBackground?.ReturnRenderPass(this.currentBackgroundPass);
            this.currentBackgroundPass = null;
            this.currentBackground = newBackground;
        }

        if (this.currentBackground is not null && this.currentBackgroundPass is not null)
        {
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

    /// <summary>Suppresses starting the move animation for the next render cycle.</summary>
    /// <param name="recursive">Whether to suppress recursively.</param>
    public void SuppressNextMoveAnimation(bool recursive = true)
    {
        this.suppressNextMoveAnimation = true;
        if (!recursive)
            return;
        foreach (var f in this.EnumerateHierarchy<ControlSpannable>())
            f.SuppressNextMoveAnimation(false);
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

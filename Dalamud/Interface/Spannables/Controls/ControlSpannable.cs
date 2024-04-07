using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
[DebuggerDisplay("#{Name}: {text}")]
public partial class ControlSpannable : Spannable
{
    /// <summary>Uses the dimensions provided from the parent.</summary>
    public const float MatchParent = -1f;

    /// <summary>Uses the dimensions that will wrap the content.</summary>
    public const float WrapContent = -2f;

    private static readonly bool DrawDebugControlBorder = true;

    private RectVector4 measuredOutsideBox;
    private RectVector4 measuredBoundaryBox;
    private RectVector4 measuredInteractiveBox;
    private RectVector4 measuredContentBox;

    private bool suppressNextAnimation;
    private bool suppressNextMoveAnimation;

    private bool wasVisible;

    /// <summary>Initializes a new instance of the <see cref="ControlSpannable"/> class.</summary>
    public ControlSpannable() => this.EventEnabled = true;

    /// <summary>Gets or sets the inner transform origin.</summary>
    public Vector2 InnerOrigin { get; set; } = new(0.5f);

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
            this.measuredOutsideBox == value,
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
            this.measuredBoundaryBox == value,
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
            this.measuredInteractiveBox == value,
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
            this.measuredContentBox == value,
            this.OnMeasuredContentBoxChange);
    }

    /// <summary>Gets the effective scale from the current (or last, if outside) render cycle.</summary>
    public override float EffectiveRenderScale => this.scale * base.EffectiveRenderScale;

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

    /// <summary>Gets either <see cref="showAnimation"/> or <see cref="hideAnimation"/> according to
    /// <see cref="Spannable.Visible"/>.</summary>
    private SpannableAnimator? VisibilityAnimation => this.Visible ? this.showAnimation : this.hideAnimation;

    /// <summary>Tests if this control can be interacted with mouse cursor at the given local coordinates.</summary>
    /// <param name="localLocation">The local coordinates.</param>
    /// <returns><c>true</c> if it is.</returns>
    public override bool HitTest(Vector2 localLocation) => this.measuredInteractiveBox.Contains(localLocation);

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
    protected override void OnMeasure(SpannableMeasureEventArgs args)
    {
        // Note: EffectiveScale is for preparing the source resources.
        // We deal only with our own scale here, as the outer scale is dealt by the caller.
        var myMaxSize = args.PreferredSize * this.scale;
        var boundaryContentGap = this.margin + this.padding;

        this.MeasuredContentBox = RectVector4.Translate(
            RectVector4.Normalize(
                this.MeasureContentBox(
                    new(
                        this.IsWidthWrapContent
                            ? float.PositiveInfinity
                            : Math.Max(
                                0,
                                (this.IsWidthMatchParent ? myMaxSize.X : this.size.X) - boundaryContentGap.Size.X),
                        this.IsHeightWrapContent
                            ? float.PositiveInfinity
                            : Math.Max(
                                0,
                                (this.IsHeightMatchParent ? myMaxSize.Y : this.size.Y) - boundaryContentGap.Size.Y)))),
            boundaryContentGap.LeftTop);
        this.MeasuredInteractiveBox = RectVector4.Expand(this.MeasuredContentBox, this.padding);
        this.MeasuredBoundaryBox = RectVector4.Expand(this.MeasuredInteractiveBox, this.margin);
        this.MeasuredOutsideBox = RectVector4.Expand(this.MeasuredBoundaryBox, this.extendOutside);
        this.Boundary = this.MeasuredBoundaryBox * this.scale;

        if (this.wasVisible != this.Visible)
        {
            this.VisibilityAnimation?.Restart();
            this.wasVisible = this.Visible;
        }

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
        {
            visibilityAnimation.Update(this);
            this.MeasuredContentBox = RectVector4.Normalize(
                RectVector4.Expand(this.MeasuredContentBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.MeasuredInteractiveBox = RectVector4.Normalize(
                RectVector4.Expand(this.MeasuredInteractiveBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.MeasuredBoundaryBox = RectVector4.Normalize(
                RectVector4.Expand(this.MeasuredBoundaryBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.MeasuredOutsideBox = RectVector4.Normalize(
                RectVector4.Expand(this.MeasuredOutsideBox, visibilityAnimation.AnimatedBoundaryAdjustment));
            this.Boundary = this.MeasuredBoundaryBox * this.scale;
        }

        if (this.background is not null)
        {
            if (this.background is DisplayedStatePattern dsbp)
                dsbp.State = this.GetDisplayedState();
            this.background.RenderPassMeasure(this.Boundary.Size);
        }

        if (this.IsAnyAnimationRunning)
            this.RequestMeasure();

        base.OnMeasure(args);
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        this.background?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
        base.OnPlace(args);
    }

    /// <inheritdoc/>
    protected override unsafe void OnDraw(SpannableDrawEventArgs args)
    {
        base.OnDraw(args);

        if (!this.Visible && this.HideAnimation?.IsRunning is not true)
            return;

        // Note: our temporary draw list uses EffectiveScale, because that's the scale that'll actualy be displayed on
        // the screen.
        // For inner transformation we use just Scale, because the scale from the parent will be dealt by the parent.

        var tmpDrawList = this.Renderer.RentDrawList(args.DrawListPtr.NativePtr);
        tmpDrawList._CmdHeader.ClipRect = this.measuredBoundaryBox.Vector4;
        tmpDrawList.CmdBuffer[0].ClipRect = tmpDrawList._CmdHeader.ClipRect;
        try
        {
            this.background?.RenderPassDraw(tmpDrawList);

            using (new ScopedTransformer(
                       tmpDrawList,
                       Matrix4x4.Identity,
                       Vector2.One,
                       this.Enabled ? 1f : this.disabledTextOpacity))
            {
                var e = SpannableEventArgsPool.Rent<SpannableDrawEventArgs>();
                e.Initialize(this);
                e.InitializeDrawEvent(tmpDrawList);
                this.OnDrawInside(e);
                SpannableEventArgsPool.Return(e);
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
                    args.DrawListPtr.AddImageQuad(
                        dlt.ImGuiHandle,
                        Vector2.Transform(this.measuredBoundaryBox.LeftTop, this.LocalTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.RightTop, this.LocalTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.RightBottom, this.LocalTransformation),
                        Vector2.Transform(this.measuredBoundaryBox.LeftBottom, this.LocalTransformation),
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
                tmpDrawList.CopyDrawListDataTo(args.DrawListPtr, this.LocalTransformation, new(1, 1, 1, opacity));
            }
        }
        finally
        {
            this.Renderer.ReturnDrawList(tmpDrawList);
        }

        // TODO: testing
        if (DrawDebugControlBorder)
        {
            using (new ScopedTransformer(args.DrawListPtr, this.LocalTransformation, Vector2.One, 1f))
            {
                args.DrawListPtr.AddRect(
                    this.MeasuredBoundaryBox.LeftTop,
                    this.MeasuredBoundaryBox.RightBottom,
                    0x20FFFFFF);
            }
        }

        // TODO: make better focus indicator
        if (this.ImGuiIsFocused)
        {
            using (new ScopedTransformer(args.DrawListPtr, this.LocalTransformation, Vector2.One, 1f))
            {
                args.DrawListPtr.AddRect(
                    this.MeasuredBoundaryBox.LeftTop,
                    this.MeasuredBoundaryBox.RightBottom,
                    0x6033BB33,
                    0,
                    ImDrawFlags.None,
                    this.ImGuiIsActive ? 2f : 1f);
            }
        }
    }

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

    /// <inheritdoc/>
    protected override Matrix4x4 TransformLocalTransformation(scoped in Matrix4x4 local)
    {
        var lt = Matrix4x4.Identity;

        if (this.VisibilityAnimation is { IsRunning: true } visibilityAnimation)
            lt = visibilityAnimation.AnimatedTransformation;

        lt = Matrix4x4.Multiply(
            lt,
            Matrix4x4.CreateTranslation(new(-this.measuredBoundaryBox.RightBottom * this.InnerOrigin, 0)));

        if (MathF.Abs(this.scale - 1f) > 0.00001f)
        {
            lt = Matrix4x4.Multiply(
                lt,
                Matrix4x4.CreateScale(this.scale));
        }

        this.transformationChangeAnimation?.Update(this);
        if (this.transformationChangeAnimation?.IsRunning is true)
        {
            lt = Matrix4x4.Multiply(
                lt,
                this.transformationChangeAnimation.AnimatedTransformation);
        }
        else if (!this.transformation.IsIdentity)
        {
            lt = Matrix4x4.Multiply(
                lt,
                this.transformation);
        }

        if (this.moveAnimation is not null)
        {
            this.moveAnimation.Update(this);
            if (this.moveAnimation.AfterMatrix != local)
            {
                this.moveAnimation.AfterMatrix = local;

                if (!this.suppressNextAnimation && !this.suppressNextMoveAnimation)
                {
                    this.moveAnimation.BeforeMatrix
                        = this.moveAnimation.IsRunning
                              ? this.moveAnimation.AnimatedTransformation
                              : this.LocalTransformation;
                    this.moveAnimation.Restart();
                }

                this.moveAnimation.Update(this);
            }

            lt = Matrix4x4.Multiply(
                lt,
                this.moveAnimation.IsRunning
                    ? this.moveAnimation.AnimatedTransformation
                    : local);
        }
        else
        {
            lt = Matrix4x4.Multiply(
                lt,
                local);
        }

        this.suppressNextAnimation = false;

        return Matrix4x4.Multiply(
            lt,
            Matrix4x4.CreateTranslation(new(this.measuredBoundaryBox.RightBottom * this.InnerOrigin, 0)));
    }
}

using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class ControlSpannable
{
    private string name = string.Empty;
    private bool enabled = true;
    private bool focusable;
    private bool visible = true;
    private bool clipChildren = true;
    private string? text;

    private TextStyle textStyle = new()
    {
        ForeColor = 0xFFFFFFFF,
        TextDecorationColor = 0xFFFFFFFF,
        TextDecorationThickness = 1 / 16f,
        VerticalAlignment = -1,
    };
    
    private float scale = 1f;
    private float renderScale = 1f;
    private Vector2 size = new(WrapContent);
    private Vector2 minSize = Vector2.Zero;
    private Vector2 maxSize = new(float.PositiveInfinity);
    private BorderVector4 extendOutside = BorderVector4.Zero;
    private BorderVector4 margin = BorderVector4.Zero;
    private BorderVector4 padding = BorderVector4.Zero;
    private Matrix4x4 transformation = Matrix4x4.Identity;
    private ISpannable? normalBackground;
    private ISpannable? hoveredBackground;
    private ISpannable? activeBackground;
    private ISpannable? disabledBackground;
    private SpannableAnimator? showAnimation;
    private SpannableAnimator? hideAnimation;
    private SpannableAnimator? moveAnimation;
    private SpannableAnimator? transformationChangeAnimation;
    private float disabledTextOpacity = 0.5f;
    private bool captureMouseOnMouseDown;
    private bool captureMouseWheel;
    private bool captureMouse;
    private bool takeKeyboardInputsOnFocus = true;

    /// <summary>Gets or sets a name, for internal identification purpose.</summary>
    public string Name
    {
        get => this.name;
        set => this.HandlePropertyChange(
            nameof(this.Name),
            ref this.name,
            value ?? throw new NullReferenceException(),
            this.OnNameChange);
    }

    /// <summary>Gets or sets a value indicating whether this control is enabled.</summary>
    public bool Enabled
    {
        get => this.enabled;
        set => this.HandlePropertyChange(nameof(this.Enabled), ref this.enabled, value, this.OnEnabledChange);
    }

    /// <summary>Gets or sets a value indicating whether this control is focusable.</summary>
    public bool Focusable
    {
        get => this.focusable;
        set => this.HandlePropertyChange(nameof(this.Focusable), ref this.focusable, value, this.OnFocusableChange);
    }

    /// <summary>Gets or sets a value indicating whether this control is visible.</summary>
    // TODO: a property indicating whether to assume zero size when invisible, so that it can skip measure pass
    public bool Visible
    {
        get => this.visible;
        set => this.HandlePropertyChange(nameof(this.Visible), ref this.visible, value, this.OnVisibleChange);
    }

    /// <summary>Gets or sets a value indicating whether to clip the children.</summary>
    public bool ClipChildren
    {
        get => this.clipChildren;
        set => this.HandlePropertyChange(
            nameof(this.ClipChildren),
            ref this.clipChildren,
            value,
            this.OnClipChildrenChange);
    }

    /// <summary>Gets or sets a text.</summary>
    /// <remarks>Default implementation does nothing with it, else than to display it in <see cref="ToString"/>.
    /// </remarks>
    public string? Text
    {
        get => this.text;
        set => this.HandlePropertyChange(nameof(this.Text), ref this.text, value, this.OnTextChange);
    }

    /// <summary>Gets or sets the text state options.</summary>
    public TextStyle TextStyle
    {
        get => this.textStyle;
        set => this.HandlePropertyChange(
            nameof(this.TextStyle),
            ref this.textStyle,
            value,
            this.OnTextStyleChange);
    }

    /// <summary>Gets or sets the scale, applicable for this and all the descendant spannables.</summary>
    /// <remarks>Effective scale is <see cref="EffectiveRenderScale"/>, which takes this and the render scale specified from
    /// <see cref="RenderContext.RenderScale"/> into consideration.</remarks>
    public float Scale
    {
        get => this.scale;
        set => this.HandlePropertyChange(
            nameof(this.Scale),
            ref this.scale,
            value,
            this.OnScaleChange);
    }

    /// <inheritdoc/>
    public float RenderScale
    {
        get => this.renderScale;
        set => this.HandlePropertyChange(
            nameof(this.RenderScale),
            ref this.renderScale,
            value,
            this.OnRenderScaleChange);
    }

    /// <summary>Gets or sets the size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="EffectiveRenderScale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 Size
    {
        get => this.size;
        set => this.HandlePropertyChange(nameof(this.Size), ref this.size, value, this.OnSizeChange);
    }

    /// <summary>Gets or sets the minimum size.</summary>
    /// <remarks>
    /// <para>The value will be scaled by <see cref="EffectiveRenderScale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MinSize
    {
        get => this.minSize;
        set => this.HandlePropertyChange(nameof(this.MinSize), ref this.minSize, value, this.OnMinSizeChange);
    }

    /// <summary>Gets or sets the maximum size.</summary>
    /// <remarks>
    /// <para>The value will be scaled by <see cref="EffectiveRenderScale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MaxSize
    {
        get => this.maxSize;
        set => this.HandlePropertyChange(nameof(this.MaxSize), ref this.maxSize, value, this.OnMaxSizeChange);
    }

    /// <summary>Gets or sets the extrusion.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 ExtendOutside
    {
        get => this.extendOutside;
        set => this.HandlePropertyChange(
            nameof(this.ExtendOutside),
            ref this.extendOutside,
            value,
            this.OnExtendOutsideChange);
    }

    /// <summary>Gets or sets the margin.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 Margin
    {
        get => this.margin;
        set => this.HandlePropertyChange(nameof(this.Margin), ref this.margin, value, this.OnMarginChange);
    }

    /// <summary>Gets or sets the padding.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 Padding
    {
        get => this.padding;
        set => this.HandlePropertyChange(nameof(this.Padding), ref this.padding, value, this.OnPaddingChange);
    }

    /// <summary>Gets or sets the transformation.</summary>
    /// <remarks>This value does not count when calculating <see cref="EffectiveRenderScale"/>.</remarks>
    public Matrix4x4 Transformation
    {
        get => this.transformation;
        set => this.HandlePropertyChange(
            nameof(this.Transformation),
            ref this.transformation,
            value,
            this.OnTransformationChange);
    }

    /// <summary>Gets or sets the normal background spannable.</summary>
    public ISpannable? NormalBackground
    {
        get => this.normalBackground;
        set => this.HandlePropertyChange(
            nameof(this.NormalBackground),
            ref this.normalBackground,
            value,
            this.OnNormalBackgroundChange);
    }

    /// <summary>Gets or sets the hovered background spannable.</summary>
    public ISpannable? HoveredBackground
    {
        get => this.hoveredBackground;
        set => this.HandlePropertyChange(
            nameof(this.HoveredBackground),
            ref this.hoveredBackground,
            value,
            this.OnHoveredBackgroundChange);
    }

    /// <summary>Gets or sets the active background spannable.</summary>
    public ISpannable? ActiveBackground
    {
        get => this.activeBackground;
        set => this.HandlePropertyChange(
            nameof(this.ActiveBackground),
            ref this.activeBackground,
            value,
            this.OnActiveBackgroundChange);
    }

    /// <summary>Gets or sets the disabled background spannable.</summary>
    public ISpannable? DisabledBackground
    {
        get => this.disabledBackground;
        set => this.HandlePropertyChange(
            nameof(this.DisabledBackground),
            ref this.disabledBackground,
            value,
            this.OnDisabledBackgroundChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Visible"/> changes to <c>true</c>.</summary>
    public SpannableAnimator? ShowAnimation
    {
        get => this.showAnimation;
        set => this.HandlePropertyChange(
            nameof(this.ShowAnimation),
            ref this.showAnimation,
            value,
            this.OnShowAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Visible"/> changes to <c>false</c>.</summary>
    public SpannableAnimator? HideAnimation
    {
        get => this.hideAnimation;
        set => this.HandlePropertyChange(
            nameof(this.HideAnimation),
            ref this.hideAnimation,
            value,
            this.OnHideAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when the control effectively moves for any reason, with respect to
    /// its parent.</summary>
    public SpannableAnimator? MoveAnimation
    {
        get => this.moveAnimation;
        set => this.HandlePropertyChange(
            nameof(this.MoveAnimation),
            ref this.moveAnimation,
            value,
            this.OnMoveAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Transformation"/> changes.</summary>
    public SpannableAnimator? TransformationChangeAnimation
    {
        get => this.transformationChangeAnimation;
        set => this.HandlePropertyChange(
            nameof(this.TransformationChangeAnimation),
            ref this.transformationChangeAnimation,
            value,
            this.OnTransformationChangeAnimationChange);
    }

    /// <summary>Gets or sets the opacity of the body when the control is disabled.</summary>
    public float DisabledTextOpacity
    {
        get => this.disabledTextOpacity;
        set => this.HandlePropertyChange(
            nameof(this.DisabledTextOpacity),
            ref this.disabledTextOpacity,
            value,
            this.OnDisabledTextOpacityChange);
    }

    /// <summary>Gets or sets a value indicating whether to capture mouse events when a mouse button is held on
    /// the control.</summary>
    /// <remarks>Enabling this when <see cref="Enabled"/> is set will typically prevent moving the container window by
    /// dragging on what <i>looks</i> like the window background.</remarks>
    public bool CaptureMouseOnMouseDown
    {
        get => this.captureMouseOnMouseDown;
        set => this.HandlePropertyChange(
            nameof(this.CaptureMouseOnMouseDown),
            ref this.captureMouseOnMouseDown,
            value,
            this.OnCaptureMouseOnMouseDownChange);
    }

    /// <summary>Gets or sets a value indicating whether to capture mouse events when a mouse button is held on
    /// the control.</summary>
    /// <remarks>Enabling this when <see cref="Enabled"/> is set will typically prevent moving the container window by
    /// dragging on what <i>looks</i> like the window background.</remarks>
    public bool CaptureMouseWheel
    {
        get => this.captureMouseWheel;
        set => this.HandlePropertyChange(
            nameof(this.CaptureMouseWheel),
            ref this.captureMouseWheel,
            value,
            this.OnCaptureMouseWheelChange);
    }

    /// <summary>Gets or sets a value indicating whether to capture mouse events, regardless of whether the control is
    /// held.</summary>
    public bool CaptureMouse
    {
        get => this.captureMouse;
        set => this.HandlePropertyChange(
            nameof(this.CaptureMouse),
            ref this.captureMouse,
            value,
            this.OnCaptureMouseChange);
    }

    /// <summary>Gets or sets a value indicating whether to take and claim keyboard inputs when focused.</summary>
    /// <remarks>
    /// <para>If set to <c>true</c>, then the game will not receive keyboard inputs when this control is focused.</para>
    /// <para>Does nothing if <see cref="Focusable"/> is <c>false</c>.</para>
    /// </remarks>
    public bool TakeKeyboardInputsOnFocus
    {
        get => this.takeKeyboardInputsOnFocus;
        set => this.HandlePropertyChange(
            nameof(this.TakeKeyboardInputsOnFocus),
            ref this.takeKeyboardInputsOnFocus,
            value,
            this.OnTakeKeyboardInputsOnFocusChange);
    }

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="sender">The object that generated the event.</param>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    /// <returns><c>true</c> if changed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool HandlePropertyChange<TSender, T>(
        TSender sender,
        string propName,
        ref T storage,
        T newValue,
        PropertyChangeEventHandler<TSender, T> eh)
        where TSender : ControlSpannable
    {
        if (Equals(storage, newValue))
            return false;
        var old = storage;
        storage = newValue;

        var e = SpannableControlEventArgsPool.Rent<PropertyChangeEventArgs<TSender, T>>();
        e.Sender = sender;
        e.PropertyName = propName;
        e.PreviousValue = old;
        e.NewValue = newValue;
        eh(e);
        SpannableControlEventArgsPool.Return(e);
        return true;
    }

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void HandlePropertyChange<T>(
        string propName,
        ref T storage,
        T newValue,
        PropertyChangeEventHandler<ControlSpannable, T> eh)
    {
        if (HandlePropertyChange(this, propName, ref storage, newValue, eh))
            this.OnSpannableChange(this);
    }
}

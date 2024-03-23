using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class SpannableControl
{
    private bool enabled = true;
    private bool visible = true;
    private string? text;
    private Vector2 size = new(WrapContent);
    private Vector2 minSize = Vector2.Zero;
    private Vector2 maxSize = new(float.MaxValue);
    private RectVector4 extrude = RectVector4.Zero;
    private RectVector4 margin = RectVector4.Zero;
    private RectVector4 padding = RectVector4.Zero;
    private ISpannable? normalBackground;
    private ISpannable? hoveredBackground;
    private ISpannable? activeBackground;
    private ISpannable? disabledBackground;
    private SpannableControlAnimator? showAnimation;
    private SpannableControlAnimator? hideAnimation;
    private float disabledTextOpacity = 0.5f;
    private bool interceptMouseWheelUp;
    private bool interceptMouseWheelDown;
    private bool interceptMouseWheelLeft;
    private bool interceptMouseWheelRight;

    /// <summary>Gets or sets a value indicating whether this control is enabled.</summary>
    public bool Enabled
    {
        get => this.enabled;
        set => HandlePropertyChange(this, nameof(this.Enabled), ref this.enabled, value, this.OnEnabledChanged);
    }

    /// <summary>Gets or sets a value indicating whether this control is visible.</summary>
    public bool Visible
    {
        get => this.visible;
        set => HandlePropertyChange(this, nameof(this.Visible), ref this.visible, value, this.OnVisibleChanged);
    }

    /// <summary>Gets or sets a text.</summary>
    /// <remarks>Default implementation does nothing with it, else than to display it in <see cref="ToString"/>.
    /// </remarks>
    public string? Text
    {
        get => this.text;
        set => HandlePropertyChange(this, nameof(this.Text), ref this.text, value, this.OnTextChanged);
    }

    /// <summary>Gets or sets the size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 Size
    {
        get => this.size;
        set => HandlePropertyChange(this, nameof(this.Size), ref this.size, value, this.OnSizeChanged);
    }

    /// <summary>Gets or sets the minimum size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MinSize
    {
        get => this.minSize;
        set => HandlePropertyChange(this, nameof(this.MinSize), ref this.minSize, value, this.OnMinSizeChanged);
    }

    /// <summary>Gets or sets the maximum size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 MaxSize
    {
        get => this.maxSize;
        set => HandlePropertyChange(this, nameof(this.MaxSize), ref this.maxSize, value, this.OnMaxSizeChanged);
    }

    /// <summary>Gets or sets the extrusion.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Extrude
    {
        get => this.extrude;
        set => HandlePropertyChange(this, nameof(this.Extrude), ref this.extrude, value, this.OnExtrudeChanged);
    }

    /// <summary>Gets or sets the margin.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Margin
    {
        get => this.margin;
        set => HandlePropertyChange(this, nameof(this.Margin), ref this.margin, value, this.OnMarginChanged);
    }

    /// <summary>Gets or sets the padding.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="Scale"/>.</para>
    /// </remarks>
    public RectVector4 Padding
    {
        get => this.padding;
        set => HandlePropertyChange(this, nameof(this.Padding), ref this.padding, value, this.OnPaddingChanged);
    }

    /// <summary>Gets or sets the normal background spannable.</summary>
    public ISpannable? NormalBackground
    {
        get => this.normalBackground;
        set => HandlePropertyChange(
            this,
            nameof(this.NormalBackground),
            ref this.normalBackground,
            value,
            this.OnNormalBackgroundChanged);
    }

    /// <summary>Gets or sets the hovered background spannable.</summary>
    public ISpannable? HoveredBackground
    {
        get => this.hoveredBackground;
        set => HandlePropertyChange(
            this,
            nameof(this.HoveredBackground),
            ref this.hoveredBackground,
            value,
            this.OnHoveredBackgroundChanged);
    }

    /// <summary>Gets or sets the active background spannable.</summary>
    public ISpannable? ActiveBackground
    {
        get => this.activeBackground;
        set => HandlePropertyChange(
            this,
            nameof(this.ActiveBackground),
            ref this.activeBackground,
            value,
            this.OnActiveBackgroundChanged);
    }

    /// <summary>Gets or sets the disabled background spannable.</summary>
    public ISpannable? DisabledBackground
    {
        get => this.disabledBackground;
        set => HandlePropertyChange(
            this,
            nameof(this.DisabledBackground),
            ref this.disabledBackground,
            value,
            this.OnDisabledBackgroundChanged);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Visible"/> changes to <c>true</c>.</summary>
    public SpannableControlAnimator? ShowAnimation
    {
        get => this.showAnimation;
        set => HandlePropertyChange(
            this,
            nameof(this.ShowAnimation),
            ref this.showAnimation,
            value,
            this.OnShowAnimationChanged);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Visible"/> changes to <c>false</c>.</summary>
    public SpannableControlAnimator? HideAnimation
    {
        get => this.hideAnimation;
        set => HandlePropertyChange(
            this,
            nameof(this.HideAnimation),
            ref this.hideAnimation,
            value,
            this.OnHideAnimationChanged);
    }

    /// <summary>Gets or sets the opacity of the body when the control is disabled.</summary>
    public float DisabledTextOpacity
    {
        get => this.disabledTextOpacity;
        set => HandlePropertyChange(
            this,
            nameof(this.DisabledTextOpacity),
            ref this.disabledTextOpacity,
            value,
            this.OnDisabledTextOpacityChanged);
    }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll up event should be intercepted.</summary>
    public bool InterceptMouseWheelUp
    {
        get => this.interceptMouseWheelUp;
        set => HandlePropertyChange(
            this,
            nameof(this.InterceptMouseWheelUp),
            ref this.interceptMouseWheelUp,
            value,
            this.OnInterceptMouseWheelUpChanged);
    }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll down event should be intercepted.</summary>
    public bool InterceptMouseWheelDown
    {
        get => this.interceptMouseWheelDown;
        set => HandlePropertyChange(
            this,
            nameof(this.InterceptMouseWheelDown),
            ref this.interceptMouseWheelDown,
            value,
            this.OnInterceptMouseWheelDownChanged);
    }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll left event should be intercepted.</summary>
    public bool InterceptMouseWheelLeft
    {
        get => this.interceptMouseWheelLeft;
        set => HandlePropertyChange(
            this,
            nameof(this.InterceptMouseWheelLeft),
            ref this.interceptMouseWheelLeft,
            value,
            this.OnInterceptMouseWheelLeftChanged);
    }

    /// <summary>Gets or sets a value indicating whether mouse wheel scroll right event should be intercepted.</summary>
    public bool InterceptMouseWheelRight
    {
        get => this.interceptMouseWheelRight;
        set => HandlePropertyChange(
            this,
            nameof(this.InterceptMouseWheelRight),
            ref this.interceptMouseWheelRight,
            value,
            this.OnInterceptMouseWheelRightChanged);
    }

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="sender">The control.</param>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="T">The type of the property.</typeparam>
    protected static void HandlePropertyChange<T>(
        SpannableControl sender,
        string propName,
        ref T storage,
        T newValue,
        SpannableControlPropertyChangedEventHandler<T> eh)
    {
        if (Equals(storage, newValue))
            return;
        var old = storage;
        storage = newValue;
        eh(
            new()
            {
                Sender = sender,
                PropertyName = propName,
                PreviousValue = old,
                NewValue = newValue,
            });
        sender.StateGeneration++;
    }
}

using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class ControlSpannable
{
    private bool enabled = true;
    private bool focusable;
    private bool takeKeyboardInputsOnFocus = true;
    private bool visible = true;
    private string? text;
    private TextState.Options textStateOptions;
    private Vector2 size = new(WrapContent);
    private Vector2 minSize = Vector2.Zero;
    private Vector2 maxSize = new(float.PositiveInfinity);
    private BorderVector4 extendOutside = BorderVector4.Zero;
    private BorderVector4 margin = BorderVector4.Zero;
    private BorderVector4 padding = BorderVector4.Zero;
    private ISpannable? normalBackground;
    private ISpannable? hoveredBackground;
    private ISpannable? activeBackground;
    private ISpannable? disabledBackground;
    private SpannableAnimator? showAnimation;
    private SpannableAnimator? hideAnimation;
    private SpannableAnimator? moveAnimation;
    private float disabledTextOpacity = 0.5f;
    private bool captureMouseOnMouseDown;
    private bool captureMouseWheel;

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
    public bool Visible
    {
        get => this.visible;
        set => this.HandlePropertyChange(nameof(this.Visible), ref this.visible, value, this.OnVisibleChange);
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

    /// <summary>Gets or sets a text.</summary>
    /// <remarks>Default implementation does nothing with it, else than to display it in <see cref="ToString"/>.
    /// </remarks>
    public string? Text
    {
        get => this.text;
        set => this.HandlePropertyChange(nameof(this.Text), ref this.text, value, this.OnTextChange);
    }

    /// <summary>Gets or sets the text state options.</summary>
    /// <remarks>If empty, the text state from the parent will be used.</remarks>
    public TextState.Options TextStateOptions
    {
        get => this.textStateOptions;
        set => this.HandlePropertyChange(
            nameof(this.TextStateOptions),
            ref this.textStateOptions,
            value,
            this.OnTextStateOptionsChange);
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
        set => this.HandlePropertyChange(nameof(this.Size), ref this.size, value, this.OnSizeChange);
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
        set => this.HandlePropertyChange(nameof(this.MinSize), ref this.minSize, value, this.OnMinSizeChange);
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
        set => this.HandlePropertyChange(nameof(this.MaxSize), ref this.maxSize, value, this.OnMaxSizeChange);
    }

    /// <summary>Gets or sets the extrusion.</summary>
    /// <remarks>The value will be scaled by <see cref="Scale"/>.</remarks>
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
    /// <remarks>The value will be scaled by <see cref="Scale"/>.</remarks>
    public BorderVector4 Margin
    {
        get => this.margin;
        set => this.HandlePropertyChange(nameof(this.Margin), ref this.margin, value, this.OnMarginChange);
    }

    /// <summary>Gets or sets the padding.</summary>
    /// <remarks>The value will be scaled by <see cref="Scale"/>.</remarks>
    public BorderVector4 Padding
    {
        get => this.padding;
        set => this.HandlePropertyChange(nameof(this.Padding), ref this.padding, value, this.OnPaddingChange);
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

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="sender">The object that generated the event.</param>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    /// <returns><c>true</c> if changed.</returns>
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
        
        var e = ControlEventArgsPool.Rent<PropertyChangeEventArgs<TSender, T>>();
        e.Sender = sender;
        e.PropertyName = propName;
        e.PreviousValue = old;
        e.NewValue = newValue;
        eh(e);
        ControlEventArgsPool.Return(e);
        return false;
    }

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    protected void HandlePropertyChange<T>(
        string propName,
        ref T storage,
        T newValue,
        PropertyChangeEventHandler<ControlSpannable, T> eh)
    {
        if (HandlePropertyChange(this, propName, ref storage, newValue, eh))
            this.StateGeneration++;
    }
}

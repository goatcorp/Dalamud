using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tri-state control for checkboxes and radio buttons.</summary>
public class BooleanControl : LabelControl
{
    private readonly DisplayedStatePattern dsbp = new();

    private bool @checked;
    private bool indeterminate;
    private Side iconSide;
    private Spannable? backgroundSpannable;
    private Spannable? foregroundSpannable;
    private NullableBooleanStatePattern? normalIcon;
    private NullableBooleanStatePattern? hoveredIcon;
    private NullableBooleanStatePattern? activeIcon;

    /// <summary>Initializes a new instance of the <see cref="BooleanControl"/> class.</summary>
    public BooleanControl()
    {
        this.IconSize = new(24f);
        this.IconMinSize = new(16f);
        this.IconMaxSize = new(24f);
        this.dsbp.SizeChange += this.DsbpOnSizeChange;
        this.dsbp.MinSizeChange += this.DsbpOnMinSizeChange;
        this.dsbp.MaxSizeChange += this.DsbpOnMaxSizeChange;
    }

    /// <summary>Occurs when the property <see cref="Checked"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CheckedChange;

    /// <summary>Occurs when the property <see cref="Indeterminate"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? IndeterminateChange;

    /// <summary>Occurs when the property <see cref="IconSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? IconSizeChange;

    /// <summary>Occurs when the property <see cref="IconMinSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? IconMinSizeChange;

    /// <summary>Occurs when the property <see cref="IconMaxSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? IconMaxSizeChange;

    /// <summary>Occurs when the property <see cref="IconSide"/> is changing.</summary>
    public event PropertyChangeEventHandler<Side>? IconSideChange;

    /// <summary>Occurs when the property <see cref="BackgroundSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? BackgroundSpannableChange;

    /// <summary>Occurs when the property <see cref="ForegroundSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? ForegroundSpannableChange;

    /// <summary>Occurs when the property <see cref="NormalIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<NullableBooleanStatePattern?>? NormalIconChange;

    /// <summary>Occurs when the property <see cref="HoveredIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<NullableBooleanStatePattern?>? HoveredIconChange;

    /// <summary>Occurs when the property <see cref="ActiveIcon"/> is changing.</summary>
    public event PropertyChangeEventHandler<NullableBooleanStatePattern?>? ActiveIconChange;

    /// <summary>Side of an icon.</summary>
    public enum Side
    {
        /// <summary>Show the icon on the left.</summary>
        Left,

        /// <summary>Show the icon on the top.</summary>
        Top,

        /// <summary>Show the icon on the right.</summary>
        Right,

        /// <summary>Show the icon on the bottom.</summary>
        Bottom,
    }

    /// <summary>Gets or sets a value indicating whether this checkbox is checked.</summary>
    public bool Checked
    {
        get => this.@checked;
        set => this.HandlePropertyChange(
            nameof(this.Checked),
            ref this.@checked,
            value,
            this.@checked == value,
            this.OnCheckedChange);
    }

    /// <summary>Gets or sets a value indicating whether this checkbox is indeterminate.</summary>
    /// <remarks>If <c>true</c>, then automatic <see cref="Checked"/> toggling will be disabled.</remarks>
    public bool Indeterminate
    {
        get => this.indeterminate;
        set => this.HandlePropertyChange(
            nameof(this.Indeterminate),
            ref this.indeterminate,
            value,
            this.indeterminate == value,
            this.OnIndeterminateChange);
    }

    /// <summary>Gets or sets the size of icon.</summary>
    public Vector2 IconSize
    {
        get => this.dsbp.Size;
        set => this.dsbp.Size = value;
    }

    /// <summary>Gets or sets the minimum size of icon.</summary>
    public Vector2 IconMinSize
    {
        get => this.dsbp.MinSize;
        set => this.dsbp.MinSize = value;
    }

    /// <summary>Gets or sets the maximum size of icon.</summary>
    public Vector2 IconMaxSize
    {
        get => this.dsbp.MaxSize;
        set => this.dsbp.MaxSize = value;
    }

    /// <summary>Gets or sets the side to display the icon.</summary>
    public Side IconSide
    {
        get => this.iconSide;
        set => this.HandlePropertyChange(
            nameof(this.IconSide),
            ref this.iconSide,
            value,
            this.iconSide == value,
            this.OnIconSideChange);
    }

    /// <summary>Gets or sets the background.</summary>
    public Spannable? BackgroundSpannable
    {
        get => this.backgroundSpannable;
        set => this.HandlePropertyChange(
            nameof(this.BackgroundSpannable),
            ref this.backgroundSpannable,
            value,
            this.backgroundSpannable == value,
            this.OnBackgroundSpannableChange);
    }

    /// <summary>Gets or sets the foreground.</summary>
    public Spannable? ForegroundSpannable
    {
        get => this.foregroundSpannable;
        set => this.HandlePropertyChange(
            nameof(this.ForegroundSpannable),
            ref this.foregroundSpannable,
            value,
            this.foregroundSpannable == value,
            this.OnForegroundSpannableChange);
    }

    /// <summary>Gets or sets the icon to use when not checked.</summary>
    public NullableBooleanStatePattern? NormalIcon
    {
        get => this.normalIcon;
        set => this.HandlePropertyChange(
            nameof(this.NormalIcon),
            ref this.normalIcon,
            value,
            this.normalIcon == value,
            this.OnNormalIconChange);
    }

    /// <summary>Gets or sets the icon to use when checked.</summary>
    public NullableBooleanStatePattern? HoveredIcon
    {
        get => this.hoveredIcon;
        set => this.HandlePropertyChange(
            nameof(this.HoveredIcon),
            ref this.hoveredIcon,
            value,
            this.hoveredIcon == value,
            this.OnHoveredIconChange);
    }

    /// <summary>Gets or sets the icon to use when indeterminate.</summary>
    public NullableBooleanStatePattern? ActiveIcon
    {
        get => this.activeIcon;
        set => this.HandlePropertyChange(
            nameof(this.ActiveIcon),
            ref this.activeIcon,
            value,
            this.activeIcon == value,
            this.OnActiveIconChange);
    }

    /// <summary>Raises the <see cref="CheckedChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCheckedChange(PropertyChangeEventArgs<bool> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.CheckedChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="CheckedChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIndeterminateChange(PropertyChangeEventArgs<bool> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.IndeterminateChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="IconSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIconSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        if (args.State == PropertyChangeState.After)
            this.dsbp.Size = args.NewValue;
        this.IconSizeChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="IconMinSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIconMinSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        if (args.State == PropertyChangeState.After)
            this.dsbp.MinSize = args.NewValue;
        this.IconMinSizeChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="IconMaxSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIconMaxSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        if (args.State == PropertyChangeState.After)
            this.dsbp.MaxSize = args.NewValue;
        this.IconMaxSizeChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnIconSideChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnIconSideChange(PropertyChangeEventArgs<Side> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.IconSideChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="BackgroundSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnBackgroundSpannableChange(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.BackgroundSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ForegroundSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnForegroundSpannableChange(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.ForegroundSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="NormalIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalIconChange(PropertyChangeEventArgs<NullableBooleanStatePattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.NormalIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnHoveredIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredIconChange(PropertyChangeEventArgs<NullableBooleanStatePattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.HoveredIconChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="OnActiveIconChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveIconChange(PropertyChangeEventArgs<NullableBooleanStatePattern?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.UpdateIcon();
        this.ActiveIconChange?.Invoke(args);
    }

    private void DsbpOnSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<Vector2>>();
        e.Initialize(this);
        e.InitializePropertyChangeEvent(e.PropertyName, e.State, e.PreviousValue, e.NewValue);
        this.OnIconSizeChange(e);
        args.SuppressHandling = e.SuppressHandling;
        SpannableEventArgsPool.Return(e);
    }

    private void DsbpOnMaxSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<Vector2>>();
        e.Initialize(this);
        e.InitializePropertyChangeEvent(e.PropertyName, e.State, e.PreviousValue, e.NewValue);
        this.OnIconMaxSizeChange(e);
        args.SuppressHandling = e.SuppressHandling;
        SpannableEventArgsPool.Return(e);
    }

    private void DsbpOnMinSizeChange(PropertyChangeEventArgs<Vector2> args)
    {
        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<Vector2>>();
        e.Initialize(this);
        e.InitializePropertyChangeEvent(e.PropertyName, e.State, e.PreviousValue, e.NewValue);
        this.OnIconMinSizeChange(e);
        args.SuppressHandling = e.SuppressHandling;
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Updates the icon.</summary>
    private void UpdateIcon()
    {
        bool? state = this.indeterminate ? null : this.@checked;

        this.dsbp.BackgroundSpannable = this.backgroundSpannable;
        this.dsbp.ForegroundSpannable = this.foregroundSpannable;
        this.dsbp.NormalSpannable = this.normalIcon;
        this.dsbp.HoveredSpannable = this.hoveredIcon;
        this.dsbp.ActiveSpannable = this.activeIcon;
        this.dsbp.State = this.GetDisplayedState();

        if (this.normalIcon is not null) this.normalIcon.State = state;
        if (this.hoveredIcon is not null) this.hoveredIcon.State = state;
        if (this.activeIcon is not null) this.activeIcon.State = state;

        this.LeftIcon = this.iconSide == Side.Left ? this.dsbp : null;
        this.TopIcon = this.iconSide == Side.Top ? this.dsbp : null;
        this.RightIcon = this.iconSide == Side.Right ? this.dsbp : null;
        this.BottomIcon = this.iconSide == Side.Bottom ? this.dsbp : null;
        this.RequestMeasure();
    }
}

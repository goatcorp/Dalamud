using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern spannable that draws different things depending on the displayed state.</summary>
public class DisplayedStatePattern : StateTransitioningPattern
{
    /// <summary>Initializes a new instance of the <see cref="DisplayedStatePattern"/> class.</summary>
    public DisplayedStatePattern()
        : base(4)
    {
    }

    /// <summary>Occurs when the property <see cref="State"/> is changing.</summary>
    public new event PropertyChangeEventHandler<DisplayedState>? StateChange;

    /// <summary>Occurs when the property <see cref="NormalSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? NormalSpannableChange;

    /// <summary>Occurs when the property <see cref="HoveredSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? HoveredSpannableChange;

    /// <summary>Occurs when the property <see cref="ActiveSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? ActiveSpannableChange;

    /// <summary>Occurs when the property <see cref="DisabledSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? DisabledSpannableChange;

    /// <summary>Reference information when drawing a spannable, if it draws differently depending on user interactions.
    /// </summary>
    public enum DisplayedState
    {
        /// <summary>Control is in normal state.</summary>
        Normal,

        /// <summary>Control is being hovered.</summary>
        Hovered,

        /// <summary>Control is active.</summary>
        Active,

        /// <summary>Control is disabled.</summary>
        Disabled,

        /// <summary>Control is hidden.</summary>
        Hidden,
    }

    /// <summary>Gets or sets the display state.</summary>
    public new DisplayedState State
    {
        get => (DisplayedState)base.State;
        set => base.State = (int)value;
    }

    /// <summary>Gets or sets the spannable to display when <see cref="State"/> is
    /// <see cref="DisplayedState.Normal"/>.</summary>
    public Spannable? NormalSpannable
    {
        get => this.Spannables[0];
        set => this.HandlePropertyChange(
            nameof(this.NormalSpannable),
            ref this.Spannables[0],
            value,
            ReferenceEquals(this.Spannables[0], value),
            this.OnNormalSpannableChange);
    }

    /// <summary>Gets or sets the spannable to display when <see cref="State"/> is
    /// <see cref="DisplayedState.Hovered"/>.</summary>
    public Spannable? HoveredSpannable
    {
        get => this.Spannables[1];
        set => this.HandlePropertyChange(
            nameof(this.HoveredSpannable),
            ref this.Spannables[1],
            value,
            ReferenceEquals(this.Spannables[1], value),
            this.OnHoveredSpannableChange);
    }

    /// <summary>Gets or sets the spannable to display when <see cref="State"/> is
    /// <see cref="DisplayedState.Active"/>.</summary>
    public Spannable? ActiveSpannable
    {
        get => this.Spannables[2];
        set => this.HandlePropertyChange(
            nameof(this.ActiveSpannable),
            ref this.Spannables[2],
            value,
            ReferenceEquals(this.Spannables[2], value),
            this.OnActiveSpannableChange);
    }

    /// <summary>Gets or sets the spannable to display when <see cref="State"/> is
    /// <see cref="DisplayedState.Disabled"/>.</summary>
    public Spannable? DisabledSpannable
    {
        get => this.Spannables[3];
        set => this.HandlePropertyChange(
            nameof(this.DisabledSpannable),
            ref this.Spannables[3],
            value,
            ReferenceEquals(this.Spannables[3], value),
            this.OnDisabledSpannableChange);
    }

    /// <inheritdoc/>
    protected sealed override void OnStateChange(PropertyChangeEventArgs<int> args)
    {
        base.OnStateChange(args);

        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<DisplayedState>>();
        e.Initialize(this);
        e.InitializePropertyChangeEvent(
            args.PropertyName,
            args.State,
            (DisplayedState)args.PreviousValue,
            (DisplayedState)args.NewValue);
        this.OnStateChange(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Raises the <see cref="StateChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnStateChange(PropertyChangeEventArgs<DisplayedState> args) =>
        this.StateChange?.Invoke(args);

    /// <summary>Raises the <see cref="NormalSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.NormalSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="HoveredSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.HoveredSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ActiveSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.ActiveSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="DisabledSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.DisabledSpannableChange?.Invoke(args);
    }
}

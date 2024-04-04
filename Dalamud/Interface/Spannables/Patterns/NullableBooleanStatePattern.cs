using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern with three icons and a background.</summary>
public class NullableBooleanStatePattern : StateTransitioningPattern
{
    /// <summary>Initializes a new instance of the <see cref="NullableBooleanStatePattern"/> class.</summary>
    public NullableBooleanStatePattern()
        : base(3)
    {
    }

    /// <summary>Occurs when the property <see cref="State"/> is changing.</summary>
    public new event PropertyChangeEventHandler<bool?>? StateChange;

    /// <summary>Occurs when the property <see cref="FalseSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? FalseSpannableChange;

    /// <summary>Occurs when the property <see cref="TrueSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? TrueSpannableChange;

    /// <summary>Occurs when the property <see cref="NullSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable>? NullSpannableChange;

    /// <summary>Gets or sets the state.</summary>
    public new bool? State
    {
        get => base.State switch { 0 => false, 1 => true, _ => null };
        set => base.State = value switch { false => 0, true => 1, _ => 2 };
    }

    /// <summary>Gets or sets the icon to use when not checked.</summary>
    public Spannable? FalseSpannable
    {
        get => this.Spannables[0];
        set => this.HandlePropertyChange(
            nameof(this.FalseSpannable),
            ref this.Spannables[0],
            value,
            ReferenceEquals(this.Spannables[0], value),
            this.OnFalseSpannableChange);
    }

    /// <summary>Gets or sets the icon to use when checked.</summary>
    public Spannable? TrueSpannable
    {
        get => this.Spannables[1];
        set => this.HandlePropertyChange(
            nameof(this.TrueSpannable),
            ref this.Spannables[1],
            value,
            ReferenceEquals(this.Spannables[1], value),
            this.OnTrueSpannableChange);
    }

    /// <summary>Gets or sets the icon to use when indeterminate.</summary>
    public Spannable? NullSpannable
    {
        get => this.Spannables[2];
        set => this.HandlePropertyChange(
            nameof(this.NullSpannable),
            ref this.Spannables[2],
            value,
            ReferenceEquals(this.Spannables[2], value),
            this.OnNullSpannableChange);
    }

    /// <inheritdoc/>
    protected sealed override void OnStateChange(PropertyChangeEventArgs<int> args)
    {
        base.OnStateChange(args);

        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<bool?>>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializePropertyChangeEvent(
            args.PropertyName,
            args.State,
            args.PreviousValue switch { 0 => false, 1 => true, _ => null },
            args.NewValue switch { 0 => false, 1 => true, _ => null });
        this.OnStateChange(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Raises the <see cref="StateChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnStateChange(PropertyChangeEventArgs<bool?> args) => this.StateChange?.Invoke(args);

    /// <summary>Raises the <see cref="FalseSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnFalseSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.FalseSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="TrueSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTrueSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.TrueSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="NullSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNullSpannableChange(PropertyChangeEventArgs<Spannable> args)
    {
        if (args.State == PropertyChangeState.After)
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        this.NullSpannableChange?.Invoke(args);
    }
}

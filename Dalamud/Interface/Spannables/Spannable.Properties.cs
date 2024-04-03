using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables;

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable
{
    private bool enabled = true;
    private bool focusable;
    private bool eventEnabled = true;
    private bool occupySpaceWhenHidden = true;
    private bool visible = true;

    /// <summary>Occurs when the property <see cref="Enabled"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? EnabledChange;

    /// <summary>Occurs when the property <see cref="Focusable"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? FocusableChange;
    
    /// <summary>Occurs when the property <see cref="EventEnabled"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? EventEnabledChange;

    /// <summary>Occurs when the property <see cref="OccupySpaceWhenHidden"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? OccupySpaceWhenHiddenChange;

    /// <summary>Occurs when the property <see cref="Visible"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? VisibleChange;

    /// <summary>Gets or sets a value indicating whether this spannable is enabled.</summary>
    public bool Enabled
    {
        get => this.enabled;
        set => this.HandlePropertyChange(
            nameof(this.Enabled),
            ref this.enabled,
            value,
            this.enabled == value,
            this.OnEnabledChange);
    }

    /// <summary>Gets or sets a value indicating whether this spannable is focusable.</summary>
    public bool Focusable
    {
        get => this.focusable;
        set => this.HandlePropertyChange(
            nameof(this.Focusable),
            ref this.focusable,
            value,
            this.focusable == value,
            this.OnFocusableChange);
    }

    /// <summary>Gets or sets a value indicating whether this spannable takes events.</summary>
    /// <remarks>If set to <c>false</c>, child spannables will receive events only if <see cref="Enabled"/> is set to
    /// <c>true</c>.</remarks>
    public bool EventEnabled
    {
        get => this.eventEnabled;
        set => this.HandlePropertyChange(
            nameof(this.EventEnabled),
            ref this.eventEnabled,
            value,
            this.eventEnabled == value,
            this.OnEventEnabledChange);
    }

    /// <summary>Gets or sets a value indicating whether this spannable should measure and place itself even when
    /// <see cref="Visible"/> is set to <c>false</c>.</summary>
    public bool OccupySpaceWhenHidden
    {
        get => this.occupySpaceWhenHidden;
        set => this.HandlePropertyChange(
            nameof(this.OccupySpaceWhenHidden),
            ref this.occupySpaceWhenHidden,
            value,
            this.occupySpaceWhenHidden == value,
            this.OnOccupySpaceWhenHiddenChange);
    }

    /// <summary>Gets or sets a value indicating whether this spannable is visible.</summary>
    // TODO: a property indicating whether to assume zero size when invisible, so that it can skip measure pass
    public bool Visible
    {
        get => this.visible;
        set => this.HandlePropertyChange(
            nameof(this.Visible),
            ref this.visible,
            value,
            this.visible == value,
            this.OnVisibleChange);
    }

    /// <summary>Raises the <see cref="EnabledChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnEnabledChange(PropertyChangeEventArgs<bool> args) => this.EnabledChange?.Invoke(args);

    /// <summary>Raises the <see cref="FocusableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnFocusableChange(PropertyChangeEventArgs<bool> args) => this.FocusableChange?.Invoke(args);

    /// <summary>Raises the <see cref="EventEnabledChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnEventEnabledChange(PropertyChangeEventArgs<bool> args) =>
        this.EventEnabledChange?.Invoke(args);

    /// <summary>Raises the <see cref="OccupySpaceWhenHiddenChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnOccupySpaceWhenHiddenChange(PropertyChangeEventArgs<bool> args) =>
        this.OccupySpaceWhenHiddenChange?.Invoke(args);

    /// <summary>Raises the <see cref="VisibleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnVisibleChange(PropertyChangeEventArgs<bool> args) => this.VisibleChange?.Invoke(args);

    /// <summary>Raises the <see cref="PropertyChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs"/> that contains the event data.</param>
    protected virtual void OnPropertyChange(PropertyChangeEventArgs args)
    {
        if (args.State == PropertyChangeState.After)
            this.RequestMeasure();
        this.PropertyChange?.Invoke(args);
    }

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eq">Whether the values are equal.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    /// <returns><c>true</c> if changed.</returns>
    protected bool HandlePropertyChange<T>(
        string propName,
        ref T storage,
        T newValue,
        bool eq,
        PropertyChangeEventHandler<T> eh)
    {
        if (eq)
            return false;

        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<T>>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializePropertyChangeEvent(propName, PropertyChangeState.Before, storage, newValue);

        this.OnPropertyChange(e);
        eh(e);

        if (e.SuppressHandling)
        {
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializePropertyChangeEvent(propName, PropertyChangeState.Cancelled, storage, newValue);

            this.OnPropertyChange(e);
            eh(e);

            SpannableEventArgsPool.Return(e);
            return false;
        }

        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializePropertyChangeEvent(propName, PropertyChangeState.After, storage, newValue);
        storage = e.NewValue;

        this.OnPropertyChange(e);
        eh(e);

        SpannableEventArgsPool.Return(e);
        return true;
    }
}

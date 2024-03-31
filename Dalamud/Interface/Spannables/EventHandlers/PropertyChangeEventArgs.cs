using System.Runtime.CompilerServices;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Property changed event arguments.</summary>
public record PropertyChangeEventArgs : SpannableEventArgs
{
    /// <summary>Gets the name of the changed property.</summary>
    public string PropertyName { get; private set; } = string.Empty;

    /// <summary>Gets the change state.</summary>
    public PropertyChangeState State { get; private set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.PropertyName = string.Empty;
        this.State = PropertyChangeState.None;
        return base.TryReset();
    }

    /// <summary>Initializes property related properties of this instance.</summary>
    /// <param name="propertyName">Name of the changed property.</param>
    /// <param name="state">Change state.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializePropertyChangeEvent(string propertyName, PropertyChangeState state)
    {
        this.PropertyName = propertyName;
        this.State = state;
    }
}

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="T">Type of the changed value.</typeparam>
public record PropertyChangeEventArgs<T> : PropertyChangeEventArgs
{
    /// <summary>Gets the previous value.</summary>
    public T PreviousValue { get; private set; }

    /// <summary>Gets the new value.</summary>
    public T NewValue { get; private set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.PreviousValue = default!;
        this.NewValue = default!;
        return base.TryReset();
    }

    /// <summary>Initializes property related properties of this instance.</summary>
    /// <param name="propertyName">Name of the changed property.</param>
    /// <param name="state">Change state.</param>
    /// <param name="prevValue">Previous value.</param>
    /// <param name="newValue">New value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializePropertyChangeEvent(
        string propertyName,
        PropertyChangeState state,
        scoped in T prevValue,
        scoped in T newValue)
    {
        this.InitializePropertyChangeEvent(propertyName, state);
        this.PreviousValue = prevValue;
        this.NewValue = newValue;
    }
}

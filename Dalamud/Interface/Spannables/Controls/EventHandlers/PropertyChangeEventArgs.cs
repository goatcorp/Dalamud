using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Valid values for <see cref="PropertyChangeEventArgs{TSender,T}.State"/>.</summary>
public enum PropertyChangeState
{
    /// <summary>This event argument is invalid.</summary>
    None,

    /// <summary>This event is being called before the property change is reflected.</summary>
    Before,

    /// <summary>This event is being called after the property change is reflected.</summary>
    After,
    
    /// <summary>This event is being called after having property change cancelled after <see cref="Before"/>.</summary>
    Cancelled,
}

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
/// <typeparam name="T">Type of the changed value.</typeparam>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record PropertyChangeEventArgs<TSender, T> : SpannableControlEventArgs
{
    /// <summary>Gets or sets the name of the changed property.</summary>
    public string PropertyName { get; set; }

    /// <summary>Gets or sets the change state.</summary>
    public PropertyChangeState State { get; set; }

    /// <summary>Gets or sets the previous value.</summary>
    public T PreviousValue { get; set; }

    /// <summary>Gets or sets the new value.</summary>
    /// <remarks>Assign <see cref="PreviousValue"/> to this property on <see cref="PropertyChangeState.Before"/>,
    /// and set <see cref="State"/> to <see cref="PropertyChangeState.Cancelled"/> to cancel the property change
    /// operation.</remarks>
    public T NewValue { get; set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.PropertyName = null!;
        this.State = PropertyChangeState.None;
        this.PreviousValue = default!;
        this.NewValue = default!;
        return base.TryReset();
    }
}

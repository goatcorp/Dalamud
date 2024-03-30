using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="T">Type of the changed value.</typeparam>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record PropertyChangeEventArgs<T> : SpannableEventArgs
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

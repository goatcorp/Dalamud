using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
/// <typeparam name="T">Type of the changed value.</typeparam>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record PropertyChangeEventArgs<TSender, T> : SpannableControlEventArgs
{
    /// <summary>Gets or sets the name of the changed property.</summary>
    public string PropertyName { get; set; }

    /// <summary>Gets or sets the previous value.</summary>
    public T PreviousValue { get; set; }

    /// <summary>Gets or sets the new value.</summary>
    public T NewValue { get; set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.PropertyName = null!;
        this.PreviousValue = default!;
        this.NewValue = default!;
        return base.TryReset();
    }
}

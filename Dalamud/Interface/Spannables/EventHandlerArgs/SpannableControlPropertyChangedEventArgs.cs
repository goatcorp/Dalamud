using Dalamud.Interface.Spannables.Controls;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="T">The type of the changed value.</typeparam>
public struct SpannableControlPropertyChangedEventArgs<T>
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The name of the changed property.</summary>
    public string PropertyName;

    /// <summary>The previous value.</summary>
    public T PreviousValue;

    /// <summary>The new value.</summary>
    public T NewValue;
}

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Property changed event arguments.</summary>
/// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
/// <typeparam name="T">Type of the changed value.</typeparam>
public struct PropertyChangedEventArgs<TSender, T>
{
    /// <summary>The object that generated the event.</summary>
    public TSender Sender;

    /// <summary>The name of the changed property.</summary>
    public string PropertyName;

    /// <summary>The previous value.</summary>
    public T PreviousValue;

    /// <summary>The new value.</summary>
    public T NewValue;
}

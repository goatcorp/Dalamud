namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Valid values for <see cref="PropertyChangeEventArgs{T}.State"/>.</summary>
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

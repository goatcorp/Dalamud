namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Property changed event delegate.</summary>
/// <param name="args">Property changed event arguments.</param>
/// <typeparam name="T">Type of the changed value.</typeparam>
public delegate void PropertyChangeEventHandler<T>(PropertyChangeEventArgs<T> args);

/// <summary>Property changed event delegate.</summary>
/// <param name="args">Property changed event arguments.</param>
public delegate void PropertyChangeEventHandler(PropertyChangeEventArgs args);

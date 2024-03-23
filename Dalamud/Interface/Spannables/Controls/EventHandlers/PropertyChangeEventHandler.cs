namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Property changed event delegate.</summary>
/// <param name="args">Property changed event arguments.</param>
/// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
/// <typeparam name="T">Type of the changed value.</typeparam>
public delegate void PropertyChangeEventHandler<TSender, T>(PropertyChangeEventArgs<TSender, T> args);

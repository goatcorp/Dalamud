using Dalamud.Interface.Spannables.Controls.EventHandlers;

namespace Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;

/// <summary>Property changed event delegate.</summary>
/// <param name="args">Property changed event arguments.</param>
/// <typeparam name="TSender">Type of the object that generated the event.</typeparam>
/// <typeparam name="T">Type of the changed value.</typeparam>
public delegate void PropertyChangedEventHandler<TSender, T>(PropertyChangedEventArgs<TSender, T> args);

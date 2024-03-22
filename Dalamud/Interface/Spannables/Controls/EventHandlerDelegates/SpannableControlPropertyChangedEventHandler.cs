using Dalamud.Interface.Spannables.EventHandlerArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;

/// <summary>Property changed event delegate.</summary>
/// <param name="args">Property changed event arguments.</param>
/// <typeparam name="T">The type of the changed property.</typeparam>
public delegate void SpannableControlPropertyChangedEventHandler<T>(SpannableControlPropertyChangedEventArgs<T> args);

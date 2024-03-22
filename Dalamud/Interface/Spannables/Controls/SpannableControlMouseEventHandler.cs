using Dalamud.Interface.Spannables.EventHandlerArgs;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>Mouse event delegate.</summary>
/// <param name="args">Mouse event arguments.</param>
public delegate void SpannableControlMouseEventHandler(SpannableControlMouseEventArgs args);

/// <summary>Draw event delegate.</summary>
/// <param name="args">Draw event arguments.</param>
public delegate void SpannableControlDrawEventHandler(SpannableControlDrawArgs args);

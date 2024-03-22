using Dalamud.Interface.Spannables.EventHandlerArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;

/// <summary>Handle interaction event delegate.</summary>
/// <param name="args">Handle interaction event arguments.</param>
/// <param name="interactedLink">The interacted link, if any.</param>
public delegate void SpannableControlHandleInteractionEventHandler(SpannableControlHandleInteractionArgs args, out SpannableLinkInteracted interactedLink);

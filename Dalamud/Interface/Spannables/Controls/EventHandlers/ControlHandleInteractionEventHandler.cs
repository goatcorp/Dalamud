namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Handle interaction event delegate.</summary>
/// <param name="args">Handle interaction event arguments.</param>
/// <param name="interactedLink">The interacted link, if any.</param>
public delegate void ControlHandleInteractionEventHandler(
    ControlHandleInteractionEventArgs args,
    out SpannableLinkInteracted interactedLink);

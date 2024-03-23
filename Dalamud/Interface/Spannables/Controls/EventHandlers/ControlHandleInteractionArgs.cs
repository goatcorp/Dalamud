using Dalamud.Interface.Spannables.EventHandlerArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Hanndle interaction event arguments.</summary>
public struct ControlHandleInteractionArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>The handle interaction arguments from the spannable invoker.</summary>
    public SpannableHandleInteractionArgs HandleInteractionArgs;
}

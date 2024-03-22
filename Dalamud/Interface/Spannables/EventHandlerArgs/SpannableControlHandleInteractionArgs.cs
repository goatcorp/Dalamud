using Dalamud.Interface.Spannables.Controls;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Hanndle interaction event arguments.</summary>
public struct SpannableControlHandleInteractionArgs
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The handle interaction arguments from the spannable invoker.</summary>
    public SpannableHandleInteractionArgs HandleInteractionArgs;
}

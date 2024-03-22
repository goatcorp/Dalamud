using Dalamud.Interface.Spannables.Controls;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Draw event arguments.</summary>
public struct SpannableControlDrawArgs
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The draw arguments from the spannable invoker.</summary>
    public SpannableDrawArgs DrawArgs;
}
using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Draw event arguments.</summary>
public struct ControlDrawEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>The draw arguments from the spannable invoker.</summary>
    public SpannableDrawArgs SpannableArgs;
}

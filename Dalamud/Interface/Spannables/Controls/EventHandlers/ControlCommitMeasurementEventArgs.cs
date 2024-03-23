using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Commit measurement event arguments.</summary>
public struct ControlCommitMeasurementEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>The commit measure arguments from the spannable invoker.</summary>
    public SpannableCommitTransformationArgs SpannableArgs;
}

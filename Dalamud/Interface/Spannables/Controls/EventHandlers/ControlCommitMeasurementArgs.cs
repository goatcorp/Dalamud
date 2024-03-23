using Dalamud.Interface.Spannables.EventHandlerArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Commit measurement event arguments.</summary>
public struct ControlCommitMeasurementArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>The commit measure arguments from the spannable invoker.</summary>
    public SpannableCommitTransformationArgs MeasureArgs;
}

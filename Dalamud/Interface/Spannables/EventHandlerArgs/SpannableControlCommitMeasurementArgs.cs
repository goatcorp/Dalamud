using Dalamud.Interface.Spannables.Controls;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Commit measurement event arguments.</summary>
public struct SpannableControlCommitMeasurementArgs
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The commit measure arguments from the spannable invoker.</summary>
    public SpannableCommitTransformationArgs MeasureArgs;
}

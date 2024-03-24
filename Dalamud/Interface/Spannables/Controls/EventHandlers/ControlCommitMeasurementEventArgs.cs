using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Commit measurement event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlCommitMeasurementEventArgs : ControlEventArgs
{
    /// <summary>Gets or sets the commit measure arguments from the spannable invoker.</summary>
    public SpannableCommitTransformationArgs SpannableArgs { get; set; }
}

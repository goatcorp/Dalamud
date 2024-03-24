using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Base class for control events.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlEventArgs
{
    /// <summary>Gets or sets the control that generated the event.</summary>
    public ControlSpannable Sender { get; set; }
}

using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Draw event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlDrawEventArgs : ControlEventArgs
{
    /// <summary>Gets or sets the draw arguments from the spannable invoker.</summary>
    public SpannableDrawArgs SpannableArgs { get; set; }
}

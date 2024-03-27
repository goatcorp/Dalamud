using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Handle interaction event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlHandleInteractionEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets the handle interaction arguments from the spannable invoker.</summary>
    public SpannableHandleInteractionArgs SpannableArgs { get; set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.SpannableArgs = default;
        return base.TryReset();
    }
}

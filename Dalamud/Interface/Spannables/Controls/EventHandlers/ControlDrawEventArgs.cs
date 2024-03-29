using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Draw event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlDrawEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets the draw arguments from the spannable invoker.</summary>
    public ImDrawListPtr DrawListPtr { get; set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.DrawListPtr = default;
        return base.TryReset();
    }
}

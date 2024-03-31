using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Link event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableMouseLinkEventArgs : SpannableEventArgs
{
    /// <summary>Gets or sets the link being interacted with.</summary>
    public ReadOnlyMemory<byte> Link { get; set; }

    /// <summary>Gets or sets the mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button { get; set; }

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.Link = default;
        this.Button = default;
        return base.TryReset();
    }
}

using System.Runtime.CompilerServices;

using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Rendering;

/// <summary>The render results.</summary>
public ref struct RenderResult
{
    /// <summary>The boundary containing the offset of the text rendered, pre-transformed value.</summary>
    public RectVector4 Boundary;

    /// <summary>The final text state.</summary>
    public TextState FinalTextState;

    /// <summary>The interacted link, if any.</summary>
    public SpannableLinkInteracted InteractedLink;

    /// <summary>Gets the link, if it is not empty.</summary>
    /// <param name="link">The link.</param>
    /// <returns><c>true</c> if <paramref name="link"/> is provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetLink(out ReadOnlySpan<byte> link)
    {
        link = this.InteractedLink.Link;
        return !link.IsEmpty;
    }

    /// <summary>Gets the link, if it is not empty and the specified mouse button is clicked.</summary>
    /// <param name="link">The link.</param>
    /// <param name="expectedMouseButton">The expected mouse button.</param>
    /// <returns><c>true</c> if <paramref name="link"/> is provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetLinkOnClick(
        out ReadOnlySpan<byte> link,
        ImGuiMouseButton expectedMouseButton = ImGuiMouseButton.Left)
    {
        if (this.InteractedLink.ClickedMouseButton != expectedMouseButton || !this.InteractedLink.IsMouseClicked)
        {
            link = default;
            return false;
        }

        link = this.InteractedLink.Link;
        return !link.IsEmpty;
    }
}

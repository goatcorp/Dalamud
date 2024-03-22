using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

/// <summary>Describes an interacted link.</summary>
public ref struct SpannableLinkInteracted
{
    /// <summary>The interacted link.</summary>
    public ReadOnlySpan<byte> Link;

    /// <summary>The clicked mouse button.</summary>
    public ImGuiMouseButton ClickedMouseButton;

    /// <summary>Gets a value indicating whether the link has been clicked with any mouse button.</summary>
    public bool IsMouseClicked;

    /// <summary>Gets a value indicating whether no link is being interacted.</summary>
    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Link.IsEmpty;
    }
}

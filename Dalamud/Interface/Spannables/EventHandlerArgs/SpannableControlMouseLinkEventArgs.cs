using Dalamud.Interface.Spannables.Controls;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Link event arguments.</summary>
public ref struct SpannableControlMouseLinkEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The link being interacted with.</summary>
    public ReadOnlySpan<byte> Link;

    /// <summary>The mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button;
}

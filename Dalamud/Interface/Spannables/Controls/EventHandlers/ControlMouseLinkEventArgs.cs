using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Link event arguments.</summary>
public ref struct ControlMouseLinkEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>The link being interacted with.</summary>
    public ReadOnlySpan<byte> Link;

    /// <summary>The mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button;
}

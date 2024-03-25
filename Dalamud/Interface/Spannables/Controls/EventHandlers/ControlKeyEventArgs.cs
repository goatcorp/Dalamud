using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Key event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlKeyEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets a value indicating whether the event was handled.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool Handled { get; set; }

    /// <summary>Gets or sets a value indicating whether the CTRL key was pressed.</summary>
    public bool Control { get; set; }

    /// <summary>Gets or sets a value indicating whether the ALT key was pressed.</summary>
    public bool Alt { get; set; }

    /// <summary>Gets or sets a value indicating whether the SHIFT key was pressed.</summary>
    public bool Shift { get; set; }

    /// <summary>Gets or sets the modifier flags for a <see cref="ControlSpannable.KeyDown"/> or
    /// <see cref="ControlSpannable.KeyUp"/> event.
    /// The flags indicate which combination of CTRL, SHIFT, and ALT keys was pressed.</summary>
    public ImGuiModFlags Modifiers { get; set; }

    /// <summary>Gets or sets the keyboard code for a <see cref="ControlSpannable.KeyDown"/> or
    /// <see cref="ControlSpannable.KeyUp"/> event.</summary>
    public ImGuiKey KeyCode { get; set; }
}

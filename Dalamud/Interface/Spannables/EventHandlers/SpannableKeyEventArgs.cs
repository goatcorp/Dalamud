using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Key event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableKeyEventArgs : SpannableEventArgs
{
    /// <summary>Gets or sets a value indicating whether the event was handled.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool Handled { get; set; }

    /// <summary>Gets a value indicating whether the CTRL key was pressed.</summary>
    public bool Control => (this.Modifiers & ImGuiModFlags.Ctrl) != 0;

    /// <summary>Gets a value indicating whether the ALT key was pressed.</summary>
    public bool Alt => (this.Modifiers & ImGuiModFlags.Alt) != 0;

    /// <summary>Gets a value indicating whether the SHIFT key was pressed.</summary>
    public bool Shift => (this.Modifiers & ImGuiModFlags.Shift) != 0;

    /// <summary>Gets or sets the modifier flags for a <see cref="Spannable.KeyDown"/> or <see cref="Spannable.KeyUp"/>
    /// event. The flags indicate which combination of CTRL, SHIFT, and ALT keys was pressed.</summary>
    public ImGuiModFlags Modifiers { get; set; }

    /// <summary>Gets or sets the keyboard code for a <see cref="Spannable.KeyDown"/> or <see cref="Spannable.KeyUp"/>
    /// event.</summary>
    public ImGuiKey KeyCode { get; set; }

    /// <summary>Initializes mouse related properties of this instance of <see cref="SpannableKeyEventArgs"/>.
    /// </summary>
    /// <param name="modifiers">Currently held modifier keys.</param>
    /// <param name="key">Pressed key.</param>
    public void InitializeKeyEvent(ImGuiModFlags modifiers, ImGuiKey key)
    {
        this.Modifiers = modifiers;
        this.KeyCode = key;
    }
}

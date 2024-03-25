using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Key press event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlKeyPressEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets a value indicating whether the event was handled.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool Handled { get; set; }
    
    /// <summary>Gets or sets the character corresponding to the key pressed.</summary>
    public char KeyChar { get; set; }

    /// <summary>Gets or sets the character corresponding to the key pressed.</summary>
    public Rune Rune { get; set; }
}

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Key press event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableKeyPressEventArgs : SpannableEventArgs
{
    /// <summary>Gets or sets a value indicating whether the event was handled.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool Handled { get; set; }

    /// <summary>Gets or sets the character corresponding to the key pressed (BMP range).</summary>
    public char KeyChar { get; set; }

    /// <summary>Gets or sets the character corresponding to the key pressed (full unicode range).</summary>
    public Rune Rune { get; set; }

    /// <summary>Initializes mouse related properties of this instance of <see cref="SpannableKeyEventArgs"/>.
    /// </summary>
    /// <param name="rune">Character corresponding to the key pressed (full unicode range).</param>
    public void InitializeKeyEvent(Rune rune)
    {
        this.KeyChar = (char)rune.Value;
        this.Rune = rune;
    }
}

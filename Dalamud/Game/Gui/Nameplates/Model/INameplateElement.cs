using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate text element.
/// </summary>
public interface INameplateElement
{
    /// <summary>
    /// Gets or sets the text for the nameplate element.
    /// </summary>
    SeString Text { get; set; }

    /// <summary>
    /// Gets the type for this nameplate element. If the element is not common, it is <see cref="NameplateElementType.Unknown"/>.
    /// </summary>
    NameplateElementType Type { get; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Text"/> has been changed.
    /// <br/>Set this to <see cref="T:true"/> if you changes to <see cref="NameplateInfo"/> should take affect.
    /// </summary>
    bool HasChanged { get; set; }
}

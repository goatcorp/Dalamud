using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate text node.
/// </summary>
public interface INameplateNode
{
    /// <summary>
    /// Gets the pointer for the nameplate node.
    /// </summary>
    nint Pointer { get; }

    /// <summary>
    /// Gets or sets the text for the nameplate node.
    /// </summary>
    SeString Text { get; set; }

    /// <summary>
    /// Gets the name for this nameplate node. If the node is not common, it is <see cref="NameplateNodeName.Unknown"/>.
    /// </summary>
    NameplateNodeName Name { get; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Text"/> has been changed.
    /// <br/>Set this to <see cref="T:true"/> if you changes to <see cref="NameplateInfo"/> should take affect.
    /// </summary>
    bool HasChanged { get; set; }
}

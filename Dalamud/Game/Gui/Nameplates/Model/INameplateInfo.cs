namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate by providing some information about the visual look.
/// </summary>
public interface INameplateInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether the title is shown above the name.
    /// <br/>If the value is true, the title is shown above the name. Otherwise the title is shown below the name.
    bool IsTitleAboveName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title is visible.
    /// <br/>If the title value is true, the title is visible. Otherwise the title text is not visible and ignored.
    /// </summary>
    bool IsTitleVisible { get; set; }

    /// <summary>
    /// Gets or sets the icon shown on the nameplate. Mostly used for the status icon.
    /// </summary>
    NameplateStatusIcons IconID { get; set; }

    /// <summary>
    /// Searches for an element by a given common name.
    /// </summary>
    /// <param name="type">The common namplate element type to serch for.<br/>Note: <see cref="NameplateElementType.Unknown"/> is not supported.</param>
    /// <returns>Returns the found element, or null if nohting has been found.</returns>
    INameplateElement? GetElement(NameplateElementType type);

    /// <summary>
    /// Gets all available nameplate elements.
    /// </summary>
    /// <returns>Returns an array of all nameplate element.</returns>
    INameplateElement[] GetAllElement();
}

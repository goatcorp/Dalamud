namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// The known usage for an element.
/// </summary>
public enum NameplateElementName
{
    /// <summary>
    /// An unknown element, maybe a custom one.
    /// </summary>
    Unknown,

    /// <summary>
    /// Gets or sets the title text of the Nameplate.
    /// </summary>
    Title,

    /// <summary>
    /// Gets or sets the name text of the Nameplate.
    /// </summary>
    Name,

    /// <summary>
    /// Gets or sets the free company text.
    /// </summary>
    FreeCompany,

    /// <summary>
    /// Gets or sets the prefix text. Mostly used for the job name shortcuts or some status icons that can be shown within a text.
    /// </summary>
    Prefix,
}
